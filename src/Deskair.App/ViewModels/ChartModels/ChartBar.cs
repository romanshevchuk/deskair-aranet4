using Deskair.Domain.Analytics.Aggregation;

namespace Deskair.App.ViewModels.ChartModels;

/// <summary>One floating min to max bar with an average tick. The hours say how much of the bucket was recorded ("only 5 of 24 h"), for the hover of a partial bar.</summary>
public sealed record ChartBar(
    DateTimeOffset Start,
    DateTimeOffset End,
    double? Minimum,
    double? Maximum,
    double? Average,
    BucketState State,
    double RecordedHours = 0,
    double ExpectedHours = 0);
