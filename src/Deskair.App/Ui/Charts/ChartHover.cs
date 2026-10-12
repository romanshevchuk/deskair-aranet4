namespace Deskair.App.Ui.Charts;

/// <summary>
/// What the mouse is over. <see cref="Value"/> is null when there is no data there ("no data", never the nearest value).
/// <see cref="X"/> and <see cref="Y"/> are chart pixels for the crosshair; <see cref="Text"/> is the readout, already localized.
/// </summary>
public sealed record ChartHover(DateTimeOffset Time, double? Value, double X, double? Y, string? Text = null);
