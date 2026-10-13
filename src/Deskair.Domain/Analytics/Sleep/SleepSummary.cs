using Deskair.Domain.Metrics;

namespace Deskair.Domain.Analytics.Sleep;

/// <summary>A reading's value and when it was taken.</summary>
public readonly record struct TimedValue(double Value, DateTimeOffset Time);

/// <summary>
/// One night of one metric. Values are in the stored units (°C, hPa); the screen converts them. <see cref="Verdict"/> and <see cref="TimeElevated"/> exist for CO₂ only.
/// Nothing is made up: a night without readings has no average, peak or verdict.
/// </summary>
public sealed record SleepSummary(
    Metric Metric,
    DateTimeOffset Start,
    DateTimeOffset End,
    int ReadingCount,
    TimeSpan Recorded,
    TimeSpan Expected,
    double? Average,
    TimedValue? Lowest,
    TimedValue? Peak,
    double? DeepSleepAverage,
    TimeSpan? TimeElevated,
    SleepVerdict? Verdict)
{
    public bool HasData => ReadingCount > 0;

    /// <summary>True when the readings cover under 80 % of the night (the usual "partial" rule): the app was not connected for the rest.</summary>
    public bool IsPartial => Recorded < Expected * 0.8;
}
