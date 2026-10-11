namespace Deskair.Domain.Analytics.Trend;

/// <summary>Direction and the change over the comparison window (ppm for CO₂, the metric's unit otherwise).</summary>
public sealed record TrendResult(TrendDirection Direction, double Change, TimeSpan Window);
