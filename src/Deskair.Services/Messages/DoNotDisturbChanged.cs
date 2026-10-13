namespace Deskair.Services.Messages;

/// <summary>Windows Do Not Disturb (Focus assist, a Focus session) was turned on or off. While it is on, Windows holds Deskair's toasts; the tray and Settings say "Alerts paused by Windows".</summary>
public sealed record DoNotDisturbChanged(bool IsActive);
