using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Services.Settings;

namespace Deskair.App.ViewModels.Settings;

public sealed partial class AlertsSettingsViewModel : ObservableObject
{
    private readonly SettingsService settings;

    [ObservableProperty]
    private bool alertsEnabled;

    public AlertsSettingsViewModel(SettingsService settings)
    {
        this.settings = settings;
    }

    // TODO(SET-01e) [S, haiku]
    // Spec: docs/guides/add-a-setting.md; docs/product/behaviour-rules.md "Alerts"
    // Input : The alert switches.
    // Output: SettingsService.UpdateAsync with the new values.
    // Rules : More fields (notify at Elevated and High, quiet hours) arrive with the settings list (questions.md Q6).
    // Edge  : Unchanged values do nothing.
    // Done  : Add Tests/ViewModels/AlertsSettingsViewModelTests.
    public Task ApplyAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(SET-01e)");
}
