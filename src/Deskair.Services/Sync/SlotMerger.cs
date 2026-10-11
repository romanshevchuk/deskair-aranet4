using Deskair.Domain.Readings;

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
    // Input : existing readings, incoming readings, interval.
    // Output: the readings to add (one per newly filled slot), sorted by time, timestamps snapped.
    // Rules : history is append-only (D-0013): a slot that already has a reading is never filled again, whatever the source; of two incoming readings for one slot the first stays.
    // Edge  : empty incoming returns empty; a reading with a non-finite value does not fill its slot.
    // Done  : unskip tests Services/SlotMergerTests.Merge_* and make them pass.
    public static IReadOnlyList<Reading> Merge(
        IReadOnlyList<Reading> existing,
        IReadOnlyList<Reading> incoming,
        TimeSpan interval)
        => throw new NotImplementedException("TODO(SYNC-01b)");
}
