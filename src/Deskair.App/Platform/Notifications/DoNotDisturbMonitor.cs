using Deskair.Services.Alerts;

namespace Deskair.App.Platform.Notifications;

public sealed class DoNotDisturbMonitor : IDoNotDisturb
{
    // TODO(ALERT-03a) [M, sonnet]
    // Spec: docs/product/behaviour-rules.md "Alerts"; docs/decisions.md D-0006.
    // Input : none.
    // Output: IsActive is true while Windows Do Not Disturb (focus assist) suppresses toasts.
    // Rules : read the Windows state; the tray tooltip and Settings use it to show "Alerts paused by Windows".
    // Edge  : if the state cannot be read, return false (see questions.md Q13).
    // Done  : manual check by turning Do Not Disturb on and off.
    // Until the state can be read, alerts count as allowed (Q13). Windows itself holds native toasts while Do Not Disturb is on, so nothing is overridden.
    public bool IsActive => false;
}
