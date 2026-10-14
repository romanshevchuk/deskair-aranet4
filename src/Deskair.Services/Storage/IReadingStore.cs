using Deskair.Domain.Readings;

namespace Deskair.Services.Storage;

public interface IReadingStore
{
    Task<IReadOnlyList<Reading>> ReadRangeAsync(
        DeviceId device,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken);

    /// <summary>The time of the oldest stored reading, or null when nothing is stored (History stops ‹ there, D-0019).</summary>
    Task<DateTimeOffset?> GetFirstReadingTimeAsync(DeviceId device, CancellationToken cancellationToken);

    Task<DateTimeOffset?> GetLastReadingTimeAsync(DeviceId device, CancellationToken cancellationToken);

    /// <summary>Merges readings by interval slot and returns how many slots were newly filled.</summary>
    Task<int> MergeAsync(
        DeviceId device,
        IReadOnlyList<Reading> readings,
        ReadingSource source,
        TimeSpan interval,
        CancellationToken cancellationToken);
}
