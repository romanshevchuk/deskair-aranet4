using CommunityToolkit.Mvvm.ComponentModel;

namespace Deskair.App.ViewModels.Settings;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppearanceSettingsViewModel appearance;
    private readonly AlertsSettingsViewModel alerts;
    private readonly DevicesSettingsViewModel devices;
    private readonly DataSettingsViewModel data;
    private readonly StartupSettingsViewModel startup;

    public SettingsViewModel(AppearanceSettingsViewModel appearance, AlertsSettingsViewModel alerts, DevicesSettingsViewModel devices, DataSettingsViewModel data, StartupSettingsViewModel startup)
    {
        this.appearance = appearance;
        this.alerts = alerts;
        this.devices = devices;
        this.data = data;
        this.startup = startup;
    }

    // TODO(SET-01c) [S, haiku]
    // Spec: docs/design/screens-and-states.md "Settings"
    // Input : A CancellationToken.
    // Output: Loads all section view models.
    // Rules : Sections load independently; one failure must not stop the others.
    // Edge  : None.
    // Done  : Add Tests/ViewModels/SettingsViewModelTests.
    public Task LoadAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(SET-01c)");
}
