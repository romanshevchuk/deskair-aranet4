using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Metrics;
using Deskair.Services.Messages;
using Deskair.Services.Platform;
using Deskair.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Settings;

/// <summary>Language, theme, temperature unit and the value on the tray icon. A change from the screen is saved at once (and announced, so the app follows); <see cref="Load"/> does not save. It reloads when the settings change elsewhere.</summary>
public sealed partial class AppearanceSettingsViewModel : SettingsSectionViewModel
{
    private bool loading;

    [ObservableProperty]
    private LanguageChoice language;

    [ObservableProperty]
    private ThemeChoice theme;

    [ObservableProperty]
    private TemperatureUnit temperatureUnit;

    [ObservableProperty]
    private bool showValueOnTrayIcon;

    public AppearanceSettingsViewModel(SettingsService settings, IMessenger messenger, IUiDispatcher dispatcher, ILogger<AppearanceSettingsViewModel> logger)
        : base(settings, logger)
    {
        // Another part of the app (the tray menu) can change these too; the screen follows, so it never shows or writes back an old value.
        messenger.Register<AppearanceSettingsViewModel, SettingsChanged>(this, (self, _) => dispatcher.Post(self.Load));
    }

    public void Load()
    {
        var current = Settings.Current;
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

    partial void OnLanguageChanged(LanguageChoice value) => Save(current => current with { Language = value });

    partial void OnThemeChanged(ThemeChoice value) => Save(current => current with { Theme = value });

    partial void OnTemperatureUnitChanged(TemperatureUnit value) => Save(current => current with { TemperatureUnit = value });

    partial void OnShowValueOnTrayIconChanged(bool value) => Save(current => current with { ShowValueOnTrayIcon = value });

    private void Save(Func<AppSettings, AppSettings> change)
    {
        if (!loading)
        {
            SaveInBackground(change, "Saving the appearance settings failed.");
        }
    }
}
