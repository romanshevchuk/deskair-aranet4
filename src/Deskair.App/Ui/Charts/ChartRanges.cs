namespace Deskair.App.Ui.Charts;

/// <summary>The data ranges a chart shows. Time on the x axis is UTC ticks.</summary>
public readonly record struct ChartRanges(double XMin, double XMax, double YMin, double YMax);
