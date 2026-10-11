using Deskair.Domain.Analytics.Aggregation;

namespace Deskair.App.ViewModels.ChartModels;

/// <summary>One floating min to max bar with an average tick.</summary>
public sealed record ChartBar(
    DateTimeOffset Start,
    DateTimeOffset End,
    double? Minimum,
    double? Maximum,
    double? Average,
    BucketState State);
