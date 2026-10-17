namespace Deskair.App.ViewModels.Flyout;

/// <summary>A time under the flyout ribbon: its text ("18:00" or "Now") and where it sits along the ribbon, from 0 (left end) to 1 (right end).</summary>
public sealed record FlyoutRibbonLabel(string Text, double Position);
