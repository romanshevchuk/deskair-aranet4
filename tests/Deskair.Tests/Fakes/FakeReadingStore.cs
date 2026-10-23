using Deskair.Domain.Readings;
using Deskair.Services.Storage;
using Deskair.Services.Sync;

namespace Deskair.Tests.Fakes;

/// <summary>An in-memory reading store. Reads return the readings inside the requested range, oldest first like the real store; merges fill empty slots like the real store.</summary>
public sealed class FakeReadingStore : IReadingStore
{
    public List<Reading> Readings { get; } = [];

    public (DeviceId Device, DateTimeOffset Start, DateTimeOffset End)? Requested { get; private set; }

    public List<(DeviceId Device, ReadingSource Source, TimeSpan Interval, int Added)> Merges { get; } = [];

    /// <summary>When set, every read throws it (a locked or unreadable file).</summary>
    public Exception? FailReads { get; set; }

    /// <summary>When set, merging throws it instead of storing (a full or locked disk).</summary>
    public Exception? FailMerges { get; set; }

    public Task<IReadOnlyList<Reading>> ReadRangeAsync(
        DeviceId device,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        ThrowWhenReadsFail();
        Requested = (device, start, end);

        return Task.FromResult<IReadOnlyList<Reading>>(
            Readings.Where(reading => reading.Timestamp >= start && reading.Timestamp <= end).OrderBy(reading => reading.Timestamp).ToList());
    }

    public Task<DateTimeOffset?> GetFirstReadingTimeAsync(DeviceId device, CancellationToken cancellationToken)
    {
        ThrowWhenReadsFail();

        return Task.FromResult<DateTimeOffset?>(Readings.Count == 0 ? null : Readings.Min(reading => reading.Timestamp));
    }

    public Task<DateTimeOffset?> GetLastReadingTimeAsync(DeviceId device, CancellationToken cancellationToken)
    {
        ThrowWhenReadsFail();

        return Task.FromResult<DateTimeOffset?>(Readings.Count == 0 ? null : Readings.Max(reading => reading.Timestamp));
    }

    public Task<int> MergeAsync(
        DeviceId device,
        IReadOnlyList<Reading> readings,
        ReadingSource source,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (FailMerges is not null) { throw FailMerges; }
        var added = SlotMerger.Merge(Readings, readings, interval);
        Readings.AddRange(added);
        Merges.Add((device, source, interval, added.Count));

        return Task.FromResult(added.Count);
    }

    private void ThrowWhenReadsFail()
    {
        if (FailReads is not null)
        {
            throw FailReads;
        }
    }
}
