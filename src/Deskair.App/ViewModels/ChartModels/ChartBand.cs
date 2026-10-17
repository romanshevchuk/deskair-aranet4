namespace Deskair.App.ViewModels.ChartModels;

/// <summary>A range of values drawn as a soft band behind a line (the comfortable range of Temperature and Humidity), in the unit shown, with its label ("comfort 20–24 °C").</summary>
public sealed record ChartBand(double Low, double High, string Label);
