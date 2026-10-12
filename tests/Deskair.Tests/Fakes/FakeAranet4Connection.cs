using System.Buffers.Binary;
using System.IO;
using Deskair.Sensors.Aranet4;
using Deskair.Services.Sensors;

namespace Deskair.Tests.Fakes;

/// <summary>
/// An Aranet4 as the sensor driver sees it through <see cref="IBleConnection"/>: it answers the characteristic reads, takes history requests, and serves pages the way the real sensor did in spike S1
/// (117 values per page, 234 for humidity, one page per read, an empty page when there is nothing more).
/// </summary>
public sealed class FakeAranet4Connection : IBleConnection
{
    private readonly Dictionary<byte, ushort[]> values;
    private readonly int intervalSeconds;
    private readonly int ageSeconds;
    private readonly byte[] current;
    private byte metric;
    private int next;

    public FakeAranet4Connection(Dictionary<byte, ushort[]> values, int intervalSeconds, int ageSeconds, byte[] current)
    {
        this.values = values;
        this.intervalSeconds = intervalSeconds;
        this.ageSeconds = ageSeconds;
        this.current = current;
    }

    public string? ConnectedTo { get; private set; }

    public bool Disposed { get; private set; }

    public Exception? ConnectError { get; set; }

    public List<byte[]> HistoryRequests { get; } = [];

    public int HistoryReads { get; private set; }

    public int Reads { get; private set; }

    /// <summary>The history read that throws, counting from 1. Null means never.</summary>
    public int? FailOnHistoryRead { get; set; }

    /// <summary>From this history read on, the sensor sends only empty pages. Null means never.</summary>
    public int? StallFromHistoryRead { get; set; }

    /// <summary>Runs before each read; a test can cancel from here.</summary>
    public Action? OnRead { get; set; }

    public Task ConnectAsync(string address, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ConnectError is not null)
        {
            throw ConnectError;
        }

        ConnectedTo = address;

        return Task.CompletedTask;
    }

    public Task<byte[]> ReadAsync(Guid service, Guid characteristic, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureService(service);
        Reads++;
        OnRead?.Invoke();
        cancellationToken.ThrowIfCancellationRequested();

        if (characteristic == Aranet4Protocol.HistoryV2Id)
        {
            return Task.FromResult(NextPage());
        }

        var bytes = characteristic switch
        {
            _ when characteristic == Aranet4Protocol.IntervalId => UInt16(intervalSeconds),
            _ when characteristic == Aranet4Protocol.TotalReadingsId => UInt16(values[Aranet4Protocol.Co2Metric].Length),
            _ when characteristic == Aranet4Protocol.SecondsSinceUpdateId => UInt16(ageSeconds),
            _ when characteristic == Aranet4Protocol.CurrentReadingsId => current,
            _ => throw new InvalidOperationException($"The fake sensor has no characteristic {characteristic}.")
        };

        return Task.FromResult(bytes);
    }

    public Task WriteAsync(Guid service, Guid characteristic, byte[] value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureService(service);

        if (characteristic != Aranet4Protocol.CommandId || value.Length != 4 || value[0] != Aranet4Protocol.HistoryRequestCode)
        {
            throw new InvalidOperationException("The fake sensor only understands history requests.");
        }

        HistoryRequests.Add(value);
        metric = value[1];
        next = BinaryPrimitives.ReadUInt16LittleEndian(value.AsSpan(2, 2));

        return Task.CompletedTask;
    }

    public IAsyncEnumerable<byte[]> SubscribeAsync(Guid service, Guid characteristic, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public ValueTask DisposeAsync()
    {
        Disposed = true;

        return ValueTask.CompletedTask;
    }

    private static void EnsureService(Guid service)
    {
        if (service != Aranet4Protocol.ServiceId)
        {
            throw new InvalidOperationException($"The fake sensor has no service {service}.");
        }
    }

    private static byte[] UInt16(int value)
    {
        var bytes = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, (ushort)value);

        return bytes;
    }

    private byte[] NextPage()
    {
        HistoryReads++;
        var total = values[Aranet4Protocol.Co2Metric].Length;

        if (HistoryReads == FailOnHistoryRead)
        {
            throw new IOException("The sensor disconnected.");
        }

        var stalled = StallFromHistoryRead is { } from && HistoryReads >= from;
        var perPage = metric == Aranet4Protocol.HumidityMetric ? 234 : 117;
        var count = stalled || next < 1 ? 0 : Math.Min(perPage, total - next + 1);

        if (count <= 0)
        {
            return Aranet4TestData.Page(metric, total, ageSeconds, start: 0);
        }

        var page = Aranet4TestData.Page(metric, total, ageSeconds, next, [.. values[metric].Skip(next - 1).Take(count)]);
        next += count;

        return page;
    }
}
