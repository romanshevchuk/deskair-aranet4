using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;

namespace Deskair.Domain.Analytics.Aggregation;

public static class BucketAggregator
{
    // TODO(HIST-01a) [M, haiku]
    // Spec: docs/product/behaviour-rules.md "Aggregation" rules 1 and 2; "Semantics" (half-open buckets).
    // Input : readings (any order), metric, bucket start and end (UTC), sensor interval.
    // Output: one Bucket with average, min, max and counts; State from BucketStateClassifier.
    // Rules : only readings with start <= timestamp < end count; average is the plain mean of existing readings.
    // Edge  : no readings gives null statistics and NoData; never interpolate.
    // Done  : unskip tests in Domain/Analytics/BucketAggregatorTests.cs and make them pass.
    public static Bucket Aggregate(
        IReadOnlyList<Reading> readings,
        Metric metric,
        DateTimeOffset start,
        DateTimeOffset end,
        TimeSpan interval)
        => throw new NotImplementedException("TODO(HIST-01a)");

    // TODO(HIST-03a) [L, sonnet]
    // Spec: docs/product/behaviour-rules.md "Aggregation" rule 2; docs/design/screens-and-states.md (group by).
    // Input : readings, metric, range start and end (UTC), grouping Period, LocalCalendar, sensor interval.
    // Output: ordered list of Bucket covering the range with no holes.
    // Rules : buckets follow local calendar days, weeks ending today, or months; DST days are 23 or 25 hours.
    // Edge  : empty range returns an empty list; the caller passes "now" as end for the current month.
    // Done  : unskip tests in Domain/Analytics/BucketGroupingTests.cs and make them pass.
    public static IReadOnlyList<Bucket> Group(
        IReadOnlyList<Reading> readings,
        Metric metric,
        DateTimeOffset start,
        DateTimeOffset end,
        Period grouping,
        LocalCalendar calendar,
        TimeSpan interval)
        => throw new NotImplementedException("TODO(HIST-03a)");
}
