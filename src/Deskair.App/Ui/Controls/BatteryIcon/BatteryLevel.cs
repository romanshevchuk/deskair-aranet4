namespace Deskair.App.Ui.Controls;

/// <summary>Colour band of the battery icon: green above 20 %, amber at 20 % or below, red at 10 % or below.</summary>
public enum BatteryLevel
{
    Unknown,
    Good,
    Low,
    Critical
}
