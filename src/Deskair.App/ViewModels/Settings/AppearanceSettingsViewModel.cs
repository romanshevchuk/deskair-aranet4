using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Domain.Metrics;
using Deskair.Services.Settings;

namespace Deskair.App.ViewModels.Settings;

public sealed partial class AppearanceSettingsViewModel : ObservableObject
{
    private readonly SettingsService settings;

    [ObservableProperty]
    private LanguageChoice language;

    [ObservableProperty]
    private ThemeChoice theme;

    [ObservableProperty]
    private TemperatureUnit temperatureUnit;

    public AppearanceSettingsViewModel(SettingsService settings)
    {
        this.settings = settings;
    }

    public void Load()
    {
        var current = settings.Current;

        Language = current.Language;
        Theme = current.Theme;
        TemperatureUnit = current.TemperatureUnit;
    }

    public Task ApplyAsync(CancellationToken cancellationToken)
    {
        return settings.UpdateAsync(current => current with { Language = Language, Theme = Theme, TemperatureUnit = TemperatureUnit }, cancellationToken);
    }
}
