namespace Deskair.App.ViewModels.ChartModels;

/// <summary>A small ring on a line chart with a hover text ("Air improved at 14:16 · −934 ppm in 12 min"). The value is in the unit shown.</summary>
public sealed record ChartMarker(DateTimeOffset Time, double Value, string Text);
