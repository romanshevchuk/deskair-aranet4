using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Domain.Metrics;
using Deskair.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Settings;

/// <summary>Language, theme, temperature unit and the value on the tray icon. A change from the screen is saved at once (and announced, so the app follows); <see cref="Load"/> does not save.</summary>
public sealed partial class AppearanceSettingsViewModel : ObservableObject
{
    private readonly SettingsService settings;
    private readonly ILogger<AppearanceSettingsViewModel> logger;
    private bool loading;

    [ObservableProperty]
    private LanguageChoice language;

    [ObservableProperty]
    private ThemeChoice theme;

    [ObservableProperty]
    private TemperatureUnit temperatureUnit;

    [ObservableProperty]
    private bool showValueOnTrayIcon;

    public AppearanceSettingsViewModel(SettingsService settings, ILogger<AppearanceSettingsViewModel> logger)
    {
        this.settings = settings;
        this.logger = logger;
    }

    public void Load()
    {
        var current = settings.Current;
        loading = true;

        try
        {
            Language = current.Language;
            Theme = current.Theme;
            TemperatureUnit = current.TemperatureUnit;
            ShowValueOnTrayIcon = current.ShowValueOnTrayIcon;
        }
        finally
        {
            loading = false;
        }
    }

    public Task ApplyAsync(CancellationToken cancellationToken)
    {
        return settings.UpdateAsync(current => current with { Language = Language, Theme = Theme, TemperatureUnit = TemperatureUnit, ShowValueOnTrayIcon = ShowValueOnTrayIcon }, cancellationToken);
    }

    partial void OnLanguageChanged(LanguageChoice value) => ApplyLater();

    partial void OnThemeChanged(ThemeChoice value) => ApplyLater();

    partial void OnTemperatureUnitChanged(TemperatureUnit value) => ApplyLater();

    partial void OnShowValueOnTrayIconChanged(bool value) => ApplyLater();

    private void ApplyLater()
    {
        if (!loading)
        {
            _ = ApplyQuietlyAsync();
        }
    }

    private async Task ApplyQuietlyAsync()
    {
        try
        {
            await ApplyAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Saving the appearance settings failed.");
            }
        }
    }
}
