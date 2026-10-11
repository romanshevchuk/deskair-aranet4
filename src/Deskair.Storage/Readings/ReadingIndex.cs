using Deskair.Domain.Readings;

namespace Deskair.Storage.Readings;

/// <summary>All readings of one device held in memory, sorted by time, so aggregation never touches the disk.</summary>
public sealed class ReadingIndex
{
    // TODO(STORE-02a) [M, haiku]
    // Spec: docs/engineering/data-model.md "Size" (load everything into memory).
    // Input : readings in any order.
    // Output: the index holds them sorted by timestamp, one per timestamp.
    // Rules : keep a sorted list; adding an existing timestamp keeps the existing reading.
    // Edge  : adding an empty list changes nothing.
    // Done  : unskip tests Storage/ReadingIndexTests.Add_* and make them pass.
    public void Add(IReadOnlyList<Reading> readings)
        => throw new NotImplementedException("TODO(STORE-02a)");

    // TODO(STORE-02b) [S, haiku]
    // Spec: docs/product/behaviour-rules.md "Semantics" (half-open ranges).
    // Input : start (inclusive) and end (exclusive).
    // Output: readings in the range, oldest first.
    // Rules : binary search for the first reading at or after start.
    // Edge  : empty or reversed range returns an empty list.
    // Done  : unskip tests Storage/ReadingIndexTests.Range_* and make them pass.
    public IReadOnlyList<Reading> Range(DateTimeOffset start, DateTimeOffset end)
        => throw new NotImplementedException("TODO(STORE-02b)");

    // TODO(STORE-02c) [S, haiku]
    // Spec: docs/engineering/sync-and-import.md "Sensor sync" (read since the last stored slot).
    // Input : none.
    // Output: the newest timestamp, or null when empty.
    // Rules : O(1).
    // Edge  : empty index returns null.
    // Done  : unskip tests Storage/ReadingIndexTests.Last_* and make them pass.
    public DateTimeOffset? Last()
        => throw new NotImplementedException("TODO(STORE-02c)");
}
