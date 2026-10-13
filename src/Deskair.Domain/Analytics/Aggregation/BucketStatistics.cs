namespace Deskair.Domain.Analytics.Aggregation;

/// <summary>Numbers over a list of buckets. Only buckets with readings count, and each counts as many times as it has readings, so the result is exact, not an average of averages.</summary>
public static class BucketStatistics
{
    /// <summary>The average of all readings in the buckets, or null when there are none.</summary>
    public static double? WeightedAverage(IReadOnlyList<Bucket> buckets)
    {
        ArgumentNullException.ThrowIfNull(buckets);

        var withData = buckets.Where(HasData).ToList();
        var readings = withData.Sum(bucket => bucket.ReadingCount);

        return readings == 0 ? null : withData.Sum(bucket => bucket.Average!.Value * bucket.ReadingCount) / readings;
    }

    /// <summary>The complete bucket with the lowest average (the earliest on a tie), or null. Partial buckets are left out: a few readings say little about a whole day.</summary>
    public static Bucket? Lowest(IReadOnlyList<Bucket> buckets)
    {
        ArgumentNullException.ThrowIfNull(buckets);

        return buckets.Where(IsComplete).MinBy(bucket => bucket.Average);
    }

    /// <summary>The complete bucket with the highest average (the earliest on a tie), or null.</summary>
    public static Bucket? Highest(IReadOnlyList<Bucket> buckets)
    {
        ArgumentNullException.ThrowIfNull(buckets);

        return buckets.Where(IsComplete).MaxBy(bucket => bucket.Average);
    }

    public static bool HasData(Bucket bucket) => bucket.ReadingCount > 0 && bucket.Average is not null;

    private static bool IsComplete(Bucket bucket) => bucket.State == BucketState.Complete && HasData(bucket);
}
