using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.Services.Alerts;
using Deskair.Services.Messages;
using Deskair.Services.Platform;
using Deskair.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Settings;

/// <summary>
/// Alert switches and quiet hours. A change from the screen is saved at once; <see cref="Load"/> does not save. The quiet hours times are typed as "HH:mm": a time that does
/// not parse is flagged and not saved. <see cref="IsPausedByWindows"/> says Windows Do Not Disturb is holding the alerts.
/// </summary>
public sealed partial class AlertsSettingsViewModel : ObservableObject
{
    private static readonly string[] timeFormats = ["H:mm", "HH:mm"];

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

    [ObservableProperty]
    private TimeOnly quietHoursStart;

    [ObservableProperty]
    private TimeOnly quietHoursEnd;

    /// <summary>The start time as typed.</summary>
    [ObservableProperty]
    private string quietHoursStartText = string.Empty;

    /// <summary>The end time as typed.</summary>
    [ObservableProperty]
    private string quietHoursEndText = string.Empty;

    [ObservableProperty]
    private bool quietHoursStartInvalid;

    [ObservableProperty]
    private bool quietHoursEndInvalid;

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

        messenger.Register<AlertsSettingsViewModel, DoNotDisturbChanged>(this, static (self, message) => self.dispatcher.Post(() => self.OnDoNotDisturb(message.IsActive)));
    }

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
            QuietHoursStart = current.QuietHoursStart;
            QuietHoursEnd = current.QuietHoursEnd;
            QuietHoursStartText = Format(current.QuietHoursStart);
            QuietHoursEndText = Format(current.QuietHoursEnd);
            QuietHoursStartInvalid = false;
            QuietHoursEndInvalid = false;
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
                QuietHoursStart = QuietHoursStart,
                QuietHoursEnd = QuietHoursEnd
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

    partial void OnQuietHoursStartChanged(TimeOnly value) => ApplyLater();

    partial void OnQuietHoursEndChanged(TimeOnly value) => ApplyLater();

    partial void OnQuietHoursStartTextChanged(string value)
    {
        if (loading)
        {
            return;
        }

        if (TryParse(value, out var time))
        {
            QuietHoursStartInvalid = false;
            QuietHoursStart = time;
        }
        else
        {
            QuietHoursStartInvalid = true;
        }
    }

    partial void OnQuietHoursEndTextChanged(string value)
    {
        if (loading)
        {
            return;
        }

        if (TryParse(value, out var time))
        {
            QuietHoursEndInvalid = false;
            QuietHoursEnd = time;
        }
        else
        {
            QuietHoursEndInvalid = true;
        }
    }

    private static string Format(TimeOnly time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    private static bool TryParse(string text, out TimeOnly time) => TimeOnly.TryParseExact(text.Trim(), timeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);

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
