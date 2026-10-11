using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Services.Platform;

namespace Deskair.App.ViewModels.Settings;

public sealed partial class StartupSettingsViewModel : ObservableObject
{
    private readonly IStartupRegistration registration;

    [ObservableProperty]
    private bool startWithWindows;

    public StartupSettingsViewModel(IStartupRegistration registration)
    {
        this.registration = registration;
    }

    // TODO(SET-01h) [S, haiku]
    // Spec: docs/design/screens-and-states.md "Settings" (Start with Windows / Keep in tray)
    // Input : A CancellationToken.
    // Output: StartWithWindows from IStartupRegistration.IsEnabled; changing it calls SetEnabledAsync.
    // Rules : Keep in tray and Show CO2 on tray icon arrive with the settings list (questions.md Q6).
    // Edge  : A failing registration reverts the switch.
    // Done  : Add Tests/ViewModels/StartupSettingsViewModelTests with a fake registration.
    public Task LoadAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(SET-01h)");
}
