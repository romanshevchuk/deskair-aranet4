using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Services.Settings;

namespace Deskair.App.ViewModels.Settings;

public sealed partial class AppearanceSettingsViewModel : ObservableObject
{
    private readonly SettingsService settings;

    [ObservableProperty]
    private LanguageChoice language;

    [ObservableProperty]
    private ThemeChoice theme;

    public AppearanceSettingsViewModel(SettingsService settings)
    {
        this.settings = settings;
    }

    // TODO(SET-01d) [S, haiku]
    // Spec: docs/guides/add-a-setting.md; docs/design/screens-and-states.md "Settings"
    // Input : The chosen language and theme.
    // Output: SettingsService.UpdateAsync with the new values.
    // Rules : Language and theme apply immediately through SettingsChanged.
    // Edge  : Unchanged values do nothing.
    // Done  : Add Tests/ViewModels/AppearanceSettingsViewModelTests.
    public Task ApplyAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(SET-01d)");
}
