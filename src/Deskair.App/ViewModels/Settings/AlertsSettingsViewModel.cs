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
public sealed partial class AlertsSettingsViewModel : ObservableObject
{
    private readonly SettingsService settings;
    private readonly IDoNotDisturb doNotDisturb;
    private readonly IUiDispatcher dispatcher;
    private readonly ILogger<AlertsSettingsViewModel> logger;
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
    {
        this.settings = settings;
        this.doNotDisturb = doNotDisturb;
        this.dispatcher = dispatcher;
        this.logger = logger;

        QuietHoursStart.ValueChanged += (_, _) => ApplyLater();
        QuietHoursEnd.ValueChanged += (_, _) => ApplyLater();
        messenger.Register<AlertsSettingsViewModel, DoNotDisturbChanged>(this, static (self, message) => self.dispatcher.Post(() => self.OnDoNotDisturb(message.IsActive)));
    }

    public TimeField QuietHoursStart { get; } = new();

    public TimeField QuietHoursEnd { get; } = new();

    public void Load()
    {
        var current = settings.Current;
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

    public Task ApplyAsync(CancellationToken cancellationToken)
    {
        return settings.UpdateAsync(
            current => current with
            {
                AlertsEnabled = AlertsEnabled,
                NotifyAtElevated = NotifyAtElevated,
                NotifyAtHigh = NotifyAtHigh,
                QuietHoursEnabled = QuietHoursEnabled,
                QuietHoursStart = QuietHoursStart.Value,
                QuietHoursEnd = QuietHoursEnd.Value
            },
            cancellationToken);
    }

    partial void OnAlertsEnabledChanged(bool value)
    {
        RefreshPaused();
        ApplyLater();
    }

    partial void OnNotifyAtElevatedChanged(bool value) => ApplyLater();

    partial void OnNotifyAtHighChanged(bool value) => ApplyLater();

    partial void OnQuietHoursEnabledChanged(bool value) => ApplyLater();

    private void OnDoNotDisturb(bool active)
    {
        windowsHoldsAlerts = active;
        RefreshPaused();
    }

    private void RefreshPaused()
    {
        IsPausedByWindows = AlertsEnabled && windowsHoldsAlerts;
    }

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
                logger.LogWarning(exception, "Saving the alert settings failed.");
            }
        }
    }
}
