namespace Deskair.Services.Messages;

/// <summary>Alerts were snoozed until <see cref="Until"/>, or the snooze ended early (<see cref="Until"/> is null). The tray and the flyout show it.</summary>
public sealed record AlertsSnoozeChanged(DateTimeOffset? Until);
