namespace Deskair.App.ViewModels.ChartModels;

/// <summary>One point on a line chart. A null value is a gap, never an interpolated value.</summary>
public sealed record ChartPoint(DateTimeOffset Time, double? Value);
