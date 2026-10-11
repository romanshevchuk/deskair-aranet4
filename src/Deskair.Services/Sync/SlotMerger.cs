using Deskair.Domain.Readings;
using Deskair.Services.Storage;

namespace Deskair.Services.Sync;

public static class SlotMerger
{
    // TODO(SYNC-01a) [S, haiku]
    // Spec: docs/engineering/data-model.md "Merge rule".
    // Input : a reading timestamp, the sensor interval.
    // Output: the start of the interval slot the timestamp snaps to.
    // Rules : snap to the nearest multiple of the interval counted from the Unix epoch (within half an interval).
    // Edge  : exactly half an interval rounds up; interval of zero or less throws ArgumentOutOfRangeException.
    // Done  : unskip tests Services/SlotMergerTests.Snap_* and make them pass.
    public static DateTimeOffset Snap(DateTimeOffset timestamp, TimeSpan interval)
        => throw new NotImplementedException("TODO(SYNC-01a)");

    // TODO(SYNC-01b) [M, sonnet]
    // Spec: docs/engineering/data-model.md "Merge rule"; docs/product/behaviour-rules.md "Sync and data" rule 2.
    // Input : existing readings, incoming readings with their source, interval.
    // Output: the readings to add (one per newly filled slot), sorted by time, timestamps snapped.
    // Rules : sensor values win; imports only fill empty slots; an incoming duplicate slot keeps the first; never delete.
    // Edge  : empty incoming returns empty; a Device reading for a slot already filled by an Import is not handled here (questions.md Q2).
    // Done  : unskip tests Services/SlotMergerTests.Merge_* and make them pass.
    public static IReadOnlyList<Reading> Merge(
        IReadOnlyList<Reading> existing,
        IReadOnlyList<Reading> incoming,
        ReadingSource source,
        TimeSpan interval)
        => throw new NotImplementedException("TODO(SYNC-01b)");
}
