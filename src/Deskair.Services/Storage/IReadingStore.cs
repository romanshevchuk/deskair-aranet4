using Deskair.Domain.Readings;

namespace Deskair.Services.Storage;

public interface IReadingStore
{
    Task<IReadOnlyList<Reading>> ReadRangeAsync(
        DeviceId device,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken);

    Task<DateTimeOffset?> GetLastReadingTimeAsync(DeviceId device, CancellationToken cancellationToken);

    /// <summary>Merges readings by interval slot and returns how many slots were newly filled.</summary>
    Task<int> MergeAsync(
        DeviceId device,
        IReadOnlyList<Reading> readings,
        ReadingSource source,
        TimeSpan interval,
        CancellationToken cancellationToken);
}
