using System.Runtime.CompilerServices;
using Deskair.Domain.Readings;
using Deskair.Services.Sensors;

namespace Deskair.Sensors.Aranet4;

public sealed class Aranet4Sensor : ISensor
{
    private static readonly TimeSpan pollDelay = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan stallTimeout = TimeSpan.FromSeconds(20);
    private static readonly byte[] historyMetrics =
    [
        Aranet4Protocol.Co2Metric,
        Aranet4Protocol.TemperatureMetric,
        Aranet4Protocol.HumidityMetric,
        Aranet4Protocol.PressureMetric
    ];

    private readonly IBleConnection connection;
    private readonly DeviceId id;
    private readonly TimeProvider timeProvider;

    public Aranet4Sensor(IBleConnection connection, DeviceId id, TimeProvider timeProvider)
    {
        this.connection = connection;
        this.id = id;
        this.timeProvider = timeProvider;
    }

    public SensorCapabilities Capabilities { get; } = new(History: true, Battery: true, SignalStrength: false);

    /// <summary>Connects and reads the interval and the battery. The signal is null: a connected sensor does not report it (only its advertisements carry one).</summary>
    public async Task<SensorStatus> ConnectAsync(CancellationToken cancellationToken)
    {
        await connection.ConnectAsync(id.Address, cancellationToken);

        var interval = Aranet4Parser.ParseUInt16(await ReadAsync(Aranet4Protocol.IntervalId, cancellationToken));
        var battery = Aranet4Parser.ParseBattery(await ReadAsync(Aranet4Protocol.CurrentReadingsId, cancellationToken));

        return new SensorStatus(TimeSpan.FromSeconds(interval), battery, SignalDbm: null);
    }

    /// <summary>One read. The reading is stamped with the read time minus the age the sensor reports for its measurement.</summary>
    public async Task<Reading> ReadCurrentAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bytes = await ReadAsync(Aranet4Protocol.CurrentReadingsId, cancellationToken);
        var current = Aranet4Parser.ParseCurrent(bytes);

        return new Reading(timeProvider.GetUtcNow() - current.Age, current.Co2Ppm, current.TemperatureC, current.HumidityPct, current.PressureHpa);
    }

    /// <summary>
    /// Downloads CO₂, temperature, humidity and pressure from just before <paramref name="since"/> to the newest reading, then returns the readings from <paramref name="since"/> on, oldest first.
    /// Nothing is returned until all four metrics are complete: a download that fails or stalls throws, so the caller never sees a partial history.
    /// </summary>
    public async IAsyncEnumerable<Reading> ReadHistoryAsync(DateTimeOffset since, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var total = Aranet4Parser.ParseUInt16(await ReadAsync(Aranet4Protocol.TotalReadingsId, cancellationToken));
        var interval = TimeSpan.FromSeconds(Aranet4Parser.ParseUInt16(await ReadAsync(Aranet4Protocol.IntervalId, cancellationToken)));
        var age = TimeSpan.FromSeconds(Aranet4Parser.ParseUInt16(await ReadAsync(Aranet4Protocol.SecondsSinceUpdateId, cancellationToken)));

        if (total == 0 || interval <= TimeSpan.Zero)
        {
            yield break;
        }

        var newest = timeProvider.GetUtcNow() - age;
        var start = FirstIndex(since, newest, interval, total);
        var packets = new List<byte[]>();
        DateTimeOffset? syncedAt = null;

        foreach (var metric in historyMetrics)
        {
            var firstReadAt = await DownloadAsync(metric, start, total, packets, cancellationToken);
            syncedAt ??= firstReadAt;
        }

        foreach (var reading in Aranet4Parser.ParseHistory(packets, syncedAt!.Value))
        {
            if (reading.Timestamp >= since)
            {
                yield return reading;
            }
        }
    }

    public ValueTask DisposeAsync() => connection.DisposeAsync();

    /// <summary>One index before the reading at <paramref name="since"/>, so that a reading on the edge is not lost. Indexes are 1-based, 1 is the oldest.</summary>
    private static int FirstIndex(DateTimeOffset since, DateTimeOffset newest, TimeSpan interval, int total)
    {
        var back = (long)Math.Round((newest - since) / interval);
        var index = total - back;

        return (int)Math.Clamp(index - 1, 1, total);
    }

    private Task<byte[]> ReadAsync(Guid characteristic, CancellationToken cancellationToken)
        => connection.ReadAsync(Aranet4Protocol.ServiceId, characteristic, cancellationToken);

    /// <summary>Requests one metric from <paramref name="start"/> and reads pages until every index up to <paramref name="total"/> has arrived. Returns when the first page was read.</summary>
    private async Task<DateTimeOffset> DownloadAsync(byte metric, int start, int total, List<byte[]> packets, CancellationToken cancellationToken)
    {
        byte[] request = [Aranet4Protocol.HistoryRequestCode, metric, (byte)start, (byte)(start >> 8)];
        await connection.WriteAsync(Aranet4Protocol.ServiceId, Aranet4Protocol.CommandId, request, cancellationToken);

        var received = new HashSet<int>();
        var needed = total - start + 1;
        var lastProgress = timeProvider.GetUtcNow();
        DateTimeOffset? firstReadAt = null;

        while (received.Count < needed)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var packet = await ReadAsync(Aranet4Protocol.HistoryV2Id, cancellationToken);
            var readAt = timeProvider.GetUtcNow();

            if (AddsNewValues(packet, metric, start, total, received))
            {
                packets.Add(packet);
                firstReadAt ??= readAt;
                lastProgress = readAt;

                continue;
            }

            // An empty, stale or repeated page: the sensor is not ready yet. Wait a moment, but not forever.
            if (readAt - lastProgress > stallTimeout)
            {
                throw new TimeoutException($"The sensor stopped sending {MetricName(metric)} history after {received.Count} of {needed} readings.");
            }

            await Task.Delay(pollDelay, timeProvider, cancellationToken);
        }

        return firstReadAt!.Value;
    }

    private static bool AddsNewValues(byte[] packet, byte metric, int start, int total, HashSet<int> received)
    {
        Aranet4HistoryPage page;

        try
        {
            page = Aranet4Parser.ParseHistoryPage(packet);
        }
        catch (FormatException)
        {
            return false;
        }

        if (page.Metric != metric)
        {
            return false;
        }

        var added = false;

        for (var offset = 0; offset < page.Values.Count; offset++)
        {
            var index = page.StartIndex + offset;

            if (index >= start && index <= total && received.Add(index))
            {
                added = true;
            }
        }

        return added;
    }

    private static string MetricName(byte metric) => metric switch
    {
        Aranet4Protocol.Co2Metric => "CO2",
        Aranet4Protocol.TemperatureMetric => "temperature",
        Aranet4Protocol.HumidityMetric => "humidity",
        _ => "pressure"
    };
}
