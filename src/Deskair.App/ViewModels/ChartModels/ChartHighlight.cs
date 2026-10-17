namespace Deskair.App.ViewModels.ChartModels;

/// <summary>A stretch of time shaded across a line chart with a label above it (the biggest 24 hour change of the Pressure line: "+14.8 hPa in 24 h").</summary>
public sealed record ChartHighlight(DateTimeOffset Start, DateTimeOffset End, string Label);
