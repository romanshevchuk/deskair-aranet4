using Deskair.Services.Platform;

namespace Deskair.App.Platform.Startup;

public sealed class StartupRegistration : IStartupRegistration
{
    // TODO(SET-03a) [M, sonnet]
    // Spec: docs/design/screens-and-states.md "Settings" (Start with Windows); docs/decisions.md D-0003 (Store and non-Store).
    // Input : none.
    // Output: IsEnabled reports whether the app starts with Windows.
    // Rules : packaged builds use the startup task, unpackaged builds the Run registry key; hide the difference here.
    // Edge  : a failure to read returns false.
    // Done  : manual check in Settings once the Settings view exists.
    public bool IsEnabled => throw new NotImplementedException("TODO(SET-03a)");

    // TODO(SET-03b) [M, sonnet]
    // Spec: docs/design/screens-and-states.md "Settings" (Start with Windows).
    // Input : the wanted state.
    // Output: the app is registered or unregistered for start-up.
    // Rules : same split as TODO(SET-03a).
    // Edge  : cancellation before the change leaves the state unchanged.
    // Done  : manual check.
    public Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(SET-03b)");
}
