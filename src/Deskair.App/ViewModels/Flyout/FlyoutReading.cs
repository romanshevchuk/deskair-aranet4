namespace Deskair.App.ViewModels.Flyout;

/// <summary>One of the smaller readings under the ribbon: "Temperature", "22.3" and "°C". The unit is separate so it can be quieter than the value.</summary>
public sealed record FlyoutReading(string Label, string Value, string Unit);
