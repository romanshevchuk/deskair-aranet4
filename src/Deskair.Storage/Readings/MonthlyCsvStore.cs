using Deskair.Domain.Readings;
using Deskair.Services.Platform;
using Deskair.Services.Storage;

namespace Deskair.Storage.Readings;

/// <summary>
/// Stores readings in <c>devices/&lt;id&gt;/readings-YYYY-MM.csv</c> under the data folder.
/// Appends to the current month; rewrites go through a temp file and a rename.
/// </summary>
public sealed class MonthlyCsvStore : IReadingStore
{
    private readonly IKnownFolders folders;
    private readonly Dictionary<DeviceId, ReadingIndex> indexes = new();

    public MonthlyCsvStore(IKnownFolders folders)
    {
        this.folders = folders;
    }

    // TODO(STORE-01a) [M, haiku]
    // Spec: docs/engineering/data-model.md (file table, Size, Safety).
    // Input : a device.
    // Output: loads every readings-YYYY-MM.csv of the device into its ReadingIndex (once).
    // Rules : skip lines CsvReadingFormat cannot parse and keep going; folder name is "<providerId>-<address>" (questions.md Q9).
    // Edge  : a missing folder gives an empty index; a half-written last line is ignored.
    // Done  : add Storage/MonthlyCsvStoreTests.Load_* using a temp folder and make them pass.
    private Task<ReadingIndex> LoadAsync(DeviceId device, CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(STORE-01a)");

    // TODO(STORE-01b) [S, haiku]
    // Spec: docs/product/behaviour-rules.md "Semantics".
    // Input : device, start inclusive, end exclusive.
    // Output: readings in the range from the index.
    // Rules : uses LoadAsync then ReadingIndex.Range.
    // Edge  : unknown device returns an empty list.
    // Done  : add Storage/MonthlyCsvStoreTests.ReadRange_* and make them pass.
    public Task<IReadOnlyList<Reading>> ReadRangeAsync(
        DeviceId device,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(STORE-01b)");

    // TODO(STORE-01c) [S, haiku]
    // Spec: docs/engineering/sync-and-import.md "Sensor sync".
    // Input : a device.
    // Output: the newest stored timestamp, or null.
    // Rules : uses LoadAsync then ReadingIndex.Last.
    // Edge  : no data returns null.
    // Done  : add Storage/MonthlyCsvStoreTests.LastReading_* and make them pass.
    public Task<DateTimeOffset?> GetLastReadingTimeAsync(DeviceId device, CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(STORE-01c)");

    // TODO(STORE-01d) [L, sonnet]
    // Spec: docs/engineering/data-model.md "Merge rule" and "Safety"; AGENTS.md (never delete a reading).
    // Input : device, readings, source, interval.
    // Output: the number of newly filled slots.
    // Rules : use SlotMerger rules (sensor wins, imports fill empty slots only); append new rows to the month file;
    //         when a Device reading replaces an Import row, rewrite that month through a temp file and rename.
    // Edge  : an interrupted write must leave the previous file intact; readings spanning two months go to two files.
    // Done  : add Storage/MonthlyCsvStoreTests.Merge_* including an interrupted-write test, and make them pass.
    public Task<int> MergeAsync(
        DeviceId device,
        IReadOnlyList<Reading> readings,
        ReadingSource source,
        TimeSpan interval,
        CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(STORE-01d)");
}
