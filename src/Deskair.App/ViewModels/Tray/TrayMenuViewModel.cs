using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.ViewModels.Shell;
using Deskair.Services.Alerts;
using Deskair.Services.Messages;
using Deskair.Services.Platform;
using Deskair.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Tray;

/// <summary>
/// The tray icon's right-click menu: Open Deskair, Snooze 1 h, Snooze until tomorrow (and Resume alerts while snoozed), Show CO₂ on tray icon, Settings, Quit.
/// It follows the snooze, the setting and the language by itself, so a snooze from a toast button or the flyout shows here too. <see cref="Refresh"/> is for when the menu opens,
/// because a snooze ends without any event.
/// </summary>
public sealed partial class TrayMenuViewModel : ObservableObject
{
    private readonly ILocalizer localizer;
    private readonly IMainWindowController windows;
    private readonly SnoozeService snooze;
    private readonly SettingsService settings;
    private readonly FriendlyTime time;
    private readonly IUiDispatcher dispatcher;
    private readonly ILogger<TrayMenuViewModel> logger;

    [ObservableProperty]
    private string openText = string.Empty;

    [ObservableProperty]
    private string settingsText = string.Empty;

    [ObservableProperty]
    private string quitText = string.Empty;

    [ObservableProperty]
    private string snoozeHourText = string.Empty;

    [ObservableProperty]
    private string snoozeTomorrowText = string.Empty;

    [ObservableProperty]
    private string showValueText = string.Empty;

    /// <summary>True while alerts are snoozed: the menu then offers "Resume alerts".</summary>
    [ObservableProperty]
    private bool isSnoozed;

    /// <summary>"Resume alerts (snoozed until 14:30)".</summary>
    [ObservableProperty]
    private string resumeText = string.Empty;

    /// <summary>Whether the tray icon shows the value; the menu item is ticked then.</summary>
    [ObservableProperty]
    private bool showValue;

    public TrayMenuViewModel(
        ILocalizer localizer,
        IMainWindowController windows,
        SnoozeService snooze,
        SettingsService settings,
        FriendlyTime time,
        IMessenger messenger,
        IUiDispatcher dispatcher,
        ILogger<TrayMenuViewModel> logger)
    {
        this.localizer = localizer;
        this.windows = windows;
        this.snooze = snooze;
        this.settings = settings;
        this.time = time;
        this.dispatcher = dispatcher;
        this.logger = logger;

        messenger.Register<TrayMenuViewModel, AlertsSnoozeChanged>(this, static (self, _) => self.dispatcher.Post(self.Refresh));
        messenger.Register<TrayMenuViewModel, SettingsChanged>(this, static (self, message) => self.dispatcher.Post(() => self.ShowValue = message.Settings.ShowValueOnTrayIcon));
        messenger.Register<TrayMenuViewModel, LanguageChanged>(this, static (self, _) => self.dispatcher.Post(self.Refresh));

        ShowValue = settings.Current.ShowValueOnTrayIcon;
        Refresh();
    }

    /// <summary>Brings the texts and the snooze state up to date. Called when the menu opens.</summary>
    public void Refresh()
    {
        OpenText = localizer.Get("Action_OpenApp");
        SettingsText = localizer.Get("Nav_Settings");
        QuitText = localizer.Get("Tray_Quit");
        SnoozeHourText = localizer.Get("Toast_Snooze");
        SnoozeTomorrowText = localizer.Get("Tray_SnoozeTomorrow");
        ShowValueText = localizer.Get("Settings_TrayValue");

        var until = snooze.Until;
        IsSnoozed = until is not null;
        ResumeText = until is { } end ? localizer.Format("Tray_ResumeUntil", ("time", time.Text(end))) : string.Empty;
    }

    [RelayCommand]
    private void Open()
    {
        windows.Show();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        windows.Show(ShellSection.Settings);
    }

    [RelayCommand]
    private void Quit()
    {
        windows.Quit();
    }

    [RelayCommand]
    private void SnoozeHour()
    {
        snooze.ForOneHour();
    }

    [RelayCommand]
    private void SnoozeTomorrow()
    {
        snooze.UntilTomorrow();
    }

    [RelayCommand]
    private void Resume()
    {
        snooze.Resume();
    }

    [RelayCommand]
    private async Task ToggleShowValueAsync()
    {
        try
        {
            await settings.UpdateAsync(current => current with { ShowValueOnTrayIcon = !current.ShowValueOnTrayIcon }, CancellationToken.None);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Saving the tray icon setting failed.");
            }
        }
    }
}
