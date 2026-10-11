namespace Deskair.Domain.Analytics.Aggregation;

/// <summary>Statistics for one chart bucket (an hour, day, week or month). Statistics are null when the bucket has no readings.</summary>
public sealed record Bucket(
    DateTimeOffset Start,
    DateTimeOffset End,
    double? Average,
    double? Minimum,
    double? Maximum,
    int ReadingCount,
    int ExpectedCount,
    BucketState State);
