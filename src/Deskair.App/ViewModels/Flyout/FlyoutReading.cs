namespace Deskair.App.ViewModels.Flyout;

/// <summary>One of the smaller readings under the ribbon: "Temperature" and "22.3 °C".</summary>
public sealed record FlyoutReading(string Label, string Value);
