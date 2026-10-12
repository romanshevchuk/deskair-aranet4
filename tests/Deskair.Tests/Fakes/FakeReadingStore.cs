using Deskair.Domain.Readings;
using Deskair.Services.Storage;
using Deskair.Services.Sync;

namespace Deskair.Tests.Fakes;

/// <summary>An in-memory reading store. Reads return the readings inside the requested range in the order they were added; merges fill empty slots like the real store.</summary>
public sealed class FakeReadingStore : IReadingStore
{
    public List<Reading> Readings { get; } = [];

    public (DeviceId Device, DateTimeOffset Start, DateTimeOffset End)? Requested { get; private set; }

    public List<(DeviceId Device, ReadingSource Source, TimeSpan Interval, int Added)> Merges { get; } = [];

    public Task<IReadOnlyList<Reading>> ReadRangeAsync(
        DeviceId device,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        Requested = (device, start, end);

        return Task.FromResult<IReadOnlyList<Reading>>(
            Readings.Where(reading => reading.Timestamp >= start && reading.Timestamp <= end).ToList());
    }

    public Task<DateTimeOffset?> GetLastReadingTimeAsync(DeviceId device, CancellationToken cancellationToken)
        => Task.FromResult<DateTimeOffset?>(Readings.Count == 0 ? null : Readings.Max(reading => reading.Timestamp));

    public Task<int> MergeAsync(
        DeviceId device,
        IReadOnlyList<Reading> readings,
        ReadingSource source,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var added = SlotMerger.Merge(Readings, readings, interval);
        Readings.AddRange(added);
        Merges.Add((device, source, interval, added.Count));

        return Task.FromResult(added.Count);
    }
}
