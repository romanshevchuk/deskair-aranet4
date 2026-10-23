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
    private readonly IBleScanner scanner;
    private readonly DeviceId id;
    private readonly TimeProvider timeProvider;

    public Aranet4Sensor(IBleConnection connection, IBleScanner scanner, DeviceId id, TimeProvider timeProvider)
    {
        this.connection = connection;
        this.scanner = scanner;
        this.id = id;
        this.timeProvider = timeProvider;
    }

    public SensorCapabilities Capabilities { get; } = new(History: true, Battery: true, SignalStrength: false, Broadcasts: true);

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
    /// The measurements in the sensor's advertisements, without a connection (needs "Smart Home integration" on in the phone app). The sensor repeats the same measurement in
    /// every advertisement for a whole interval, so one is passed on only when its measurement time is clearly later than the last one's. Its time is the receiving time minus
    /// the age the sensor reports, as for <see cref="ReadCurrentAsync"/>. Listens passively: asking the sensor for its scan response on every packet would cost its battery.
    /// The sensor repeats its advertisement every few seconds, so hearing nothing from it for <see cref="SilenceLimit"/> means the scan has gone quiet (or the sensor is out of reach):
    /// the sequence then throws <see cref="TimeoutException"/> and the caller starts again with a connection.
    /// </summary>
    public async IAsyncEnumerable<SensorBroadcast> ListenAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        DateTimeOffset? newest = null;
        var silent = false;

        using var scanning = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var watchdog = timeProvider.CreateTimer(_ =>
        {
            silent = true;
            scanning.Cancel();
        }, null, SilenceLimit, Timeout.InfiniteTimeSpan);

        await using var advertisements = scanner.ScanAsync(BleScanMode.Passive, scanning.Token).GetAsyncEnumerator(scanning.Token);

        while (true)
        {
            if (!await NextAsync(advertisements, () => silent && !cancellationToken.IsCancellationRequested))
            {
                yield break;
            }

            var advertisement = advertisements.Current;

            if (!IsFromThisSensor(advertisement) || Aranet4Parser.ParseAdvertisement(advertisement.ManufacturerData[Aranet4Protocol.ManufacturerId]) is not { } current)
            {
                continue;
            }

            // Every advertisement of this sensor counts, the repeated ones too: it is the scan that is being watched, not the measurements.
            watchdog.Change(SilenceLimit, Timeout.InfiniteTimeSpan);

            var measuredAt = timeProvider.GetUtcNow() - current.Age;

            if (newest is { } last && measuredAt - last < current.Interval / 2)
            {
                continue;
            }

            newest = measuredAt;
            var reading = new Reading(measuredAt, current.Co2Ppm, current.TemperatureC, current.HumidityPct, current.PressureHpa);

            yield return new SensorBroadcast(reading, current.Interval, current.BatteryPercent <= 100 ? current.BatteryPercent : null);
        }
    }

    /// <summary>How long the scan may hear nothing from the sensor before listening is given up and started again.</summary>
    public static readonly TimeSpan SilenceLimit = TimeSpan.FromSeconds(90);

    // The watchdog stops a quiet scan by cancelling it; that is turned into a timeout here, outside the iterator, because an iterator cannot yield inside a try with a catch.
    private static async Task<bool> NextAsync(IAsyncEnumerator<BleAdvertisement> advertisements, Func<bool> watchdogFired)
    {
        try
        {
            return await advertisements.MoveNextAsync();
        }
        catch (OperationCanceledException) when (watchdogFired())
        {
            throw new TimeoutException($"No advertisement from the sensor for {SilenceLimit.TotalSeconds:0} s; the Bluetooth scan has gone quiet.");
        }
    }

    /// <summary>
    /// Downloads CO₂, temperature, humidity and pressure from just before <paramref name="since"/> to the newest reading, then returns the readings from <paramref name="since"/> on, oldest first.
    /// Nothing is returned until all four metrics are complete: a download that fails or stalls throws, so the caller never sees a partial history.
    /// </summary>
    public IAsyncEnumerable<Reading> ReadHistoryAsync(DateTimeOffset since, CancellationToken cancellationToken)
        => ReadHistoryAsync(since, null, cancellationToken);

    /// <summary>As above, and reports after every page (packet) that arrives: how many of the roughly expected pages are done.</summary>
    public async IAsyncEnumerable<Reading> ReadHistoryAsync(DateTimeOffset since, IProgress<HistoryProgress>? progress, [EnumeratorCancellation] CancellationToken cancellationToken)
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
        var counter = new PageCounter(ExpectedPages(total - start + 1), progress);
        DateTimeOffset? syncedAt = null;

        foreach (var metric in historyMetrics)
        {
            var firstReadAt = await DownloadAsync(metric, start, total, packets, counter, cancellationToken);
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

    private bool IsFromThisSensor(BleAdvertisement advertisement)
        => string.Equals(advertisement.Address, id.Address, StringComparison.OrdinalIgnoreCase)
            && advertisement.ManufacturerData.ContainsKey(Aranet4Protocol.ManufacturerId);

    private Task<byte[]> ReadAsync(Guid characteristic, CancellationToken cancellationToken)
        => connection.ReadAsync(Aranet4Protocol.ServiceId, characteristic, cancellationToken);

    /// <summary>Requests one metric from <paramref name="start"/> and reads pages until every index up to <paramref name="total"/> has arrived. Returns when the first page was read.</summary>
    private async Task<DateTimeOffset> DownloadAsync(byte metric, int start, int total, List<byte[]> packets, PageCounter counter, CancellationToken cancellationToken)
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
                counter.PageArrived();
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

    /// <summary>How many pages a download of <paramref name="needed"/> readings takes: each metric on its own, 117 values to a page and 234 for humidity.</summary>
    private static int ExpectedPages(int needed)
        => historyMetrics.Sum(metric => (needed + PerPage(metric) - 1) / PerPage(metric));

    private static int PerPage(byte metric)
        => metric == Aranet4Protocol.HumidityMetric ? Aranet4Protocol.HumidityValuesPerPage : Aranet4Protocol.ValuesPerPage;

    /// <summary>Counts pages across the four metrics and reports each one. The expected total grows if more pages arrive than estimated.</summary>
    private sealed class PageCounter
    {
        private readonly IProgress<HistoryProgress>? progress;
        private readonly int expected;
        private int done;

        public PageCounter(int expected, IProgress<HistoryProgress>? progress)
        {
            this.expected = expected;
            this.progress = progress;
        }

        public void PageArrived()
        {
            done++;
            progress?.Report(new HistoryProgress(done, Math.Max(expected, done)));
        }
    }
}
