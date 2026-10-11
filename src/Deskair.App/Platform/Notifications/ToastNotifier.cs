using Deskair.Services.Alerts;

namespace Deskair.App.Platform.Notifications;

public sealed class ToastNotifier : INotifier
{
    // TODO(ALERT-02a) [L, sonnet]
    // Spec: docs/engineering/wpf-mapping.md "Toasts"; docs/design/screens-and-states.md "Toasts"; spike S5.
    // Input : an AlertNotification.
    // Output: a Windows toast with Snooze and Dismiss actions, text from the localizer (Toast_* keys).
    // Rules : one code path for packaged and unpackaged builds; a 4 px left edge in the zone colour if supported.
    // Edge  : Windows applies Do Not Disturb itself; never override it.
    // Done  : manual check with both build types (spike S5).
    public Task NotifyAsync(AlertNotification notification, CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(ALERT-02a)");
}
