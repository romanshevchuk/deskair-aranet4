using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.Services.Alerts;
using Deskair.Services.Messages;
using Deskair.Services.Platform;
using Deskair.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Settings;

/// <summary>
/// Alert switches and quiet hours. A change from the screen is saved at once; <see cref="Load"/> does not save. The quiet hours times are <see cref="TimeField"/>s:
/// a time that does not parse is flagged and not saved. <see cref="IsPausedByWindows"/> says Windows Do Not Disturb is holding the alerts.
/// </summary>
public sealed partial class AlertsSettingsViewModel : SettingsSectionViewModel
{
    private readonly IDoNotDisturb doNotDisturb;
    private readonly IUiDispatcher dispatcher;
    private bool loading;
    private bool windowsHoldsAlerts;

    [ObservableProperty]
    private bool alertsEnabled;

    [ObservableProperty]
    private bool notifyAtElevated;

    [ObservableProperty]
    private bool notifyAtHigh;

    [ObservableProperty]
    private bool quietHoursEnabled;

    /// <summary>True while alerts are on but Windows Do Not Disturb holds them back.</summary>
    [ObservableProperty]
    private bool isPausedByWindows;

    public AlertsSettingsViewModel(
        SettingsService settings,
        IDoNotDisturb doNotDisturb,
        IMessenger messenger,
        IUiDispatcher dispatcher,
        ILogger<AlertsSettingsViewModel> logger)
        : base(settings, logger)
    {
        this.doNotDisturb = doNotDisturb;
        this.dispatcher = dispatcher;

        QuietHoursStart.ValueChanged += (_, _) => Save(current => current with { QuietHoursStart = QuietHoursStart.Value });
        QuietHoursEnd.ValueChanged += (_, _) => Save(current => current with { QuietHoursEnd = QuietHoursEnd.Value });
        messenger.Register<AlertsSettingsViewModel, DoNotDisturbChanged>(this, static (self, message) => self.dispatcher.Post(() => self.OnDoNotDisturb(message.IsActive)));
    }

    public TimeField QuietHoursStart { get; } = new();

    public TimeField QuietHoursEnd { get; } = new();

    public void Load()
    {
        var current = Settings.Current;
        loading = true;

        try
        {
            AlertsEnabled = current.AlertsEnabled;
            NotifyAtElevated = current.NotifyAtElevated;
            NotifyAtHigh = current.NotifyAtHigh;
            QuietHoursEnabled = current.QuietHoursEnabled;
            QuietHoursStart.Load(current.QuietHoursStart);
            QuietHoursEnd.Load(current.QuietHoursEnd);
            windowsHoldsAlerts = doNotDisturb.IsActive;
            RefreshPaused();
        }
        finally
        {
            loading = false;
        }
    }

    partial void OnAlertsEnabledChanged(bool value)
    {
        RefreshPaused();
        Save(current => current with { AlertsEnabled = value });
    }

    partial void OnNotifyAtElevatedChanged(bool value) => Save(current => current with { NotifyAtElevated = value });

    partial void OnNotifyAtHighChanged(bool value) => Save(current => current with { NotifyAtHigh = value });

    partial void OnQuietHoursEnabledChanged(bool value) => Save(current => current with { QuietHoursEnabled = value });

    private void OnDoNotDisturb(bool active)
    {
        windowsHoldsAlerts = active;
        RefreshPaused();
    }

    private void RefreshPaused()
    {
        IsPausedByWindows = AlertsEnabled && windowsHoldsAlerts;
    }

    private void Save(Func<AppSettings, AppSettings> change)
    {
        if (!loading)
        {
            SaveInBackground(change, "Saving the alert settings failed.");
        }
    }
}
