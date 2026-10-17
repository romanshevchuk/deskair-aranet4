using Deskair.Domain.Metrics;

namespace Deskair.Domain.Analytics.Sleep;

/// <summary>One counted night: the evening it started on, its end and its average in the stored unit.</summary>
public sealed record NightResult(DateTimeOffset Start, DateTimeOffset End, double Average);

/// <summary>
/// The nights of a Week, Month or Year (behaviour-rules.md "Aggregation" rule 8). <see cref="Average"/> is the mean of the nights' averages.
/// <see cref="Lowest"/> and <see cref="Highest"/> are the nights with the lowest and highest average (CO₂ best and worst; Temperature coolest and warmest; Humidity driest and most humid); they are set for CO₂, Temperature and Humidity.
/// </summary>
public sealed record NightsSummary(Metric Metric, int Count, double Average, NightResult? Lowest, NightResult? Highest);
