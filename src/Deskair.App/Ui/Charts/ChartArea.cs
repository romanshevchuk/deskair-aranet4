using System.Windows;

namespace Deskair.App.Ui.Charts;

/// <summary>The plot rectangle and the data ranges it shows. Layers use it to turn data values into pixels.</summary>
public sealed record ChartArea(Rect Plot, double XMin, double XMax, double YMin, double YMax);
