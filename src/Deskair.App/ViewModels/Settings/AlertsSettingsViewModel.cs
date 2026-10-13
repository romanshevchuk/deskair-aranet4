using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Settings;

/// <summary>Alert switches and quiet hours. A change from the screen is saved at once; <see cref="Load"/> does not save.</summary>
public sealed partial class AlertsSettingsViewModel : ObservableObject
{
    private readonly SettingsService settings;
    private readonly ILogger<AlertsSettingsViewModel> logger;
    private bool loading;

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

    public AlertsSettingsViewModel(SettingsService settings, ILogger<AlertsSettingsViewModel> logger)
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
            AlertsEnabled = current.AlertsEnabled;
            NotifyAtElevated = current.NotifyAtElevated;
            NotifyAtHigh = current.NotifyAtHigh;
            QuietHoursEnabled = current.QuietHoursEnabled;
            QuietHoursStart = current.QuietHoursStart;
            QuietHoursEnd = current.QuietHoursEnd;
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

    partial void OnAlertsEnabledChanged(bool value) => ApplyLater();

    partial void OnNotifyAtElevatedChanged(bool value) => ApplyLater();

    partial void OnNotifyAtHighChanged(bool value) => ApplyLater();

    partial void OnQuietHoursEnabledChanged(bool value) => ApplyLater();

    partial void OnQuietHoursStartChanged(TimeOnly value) => ApplyLater();

    partial void OnQuietHoursEndChanged(TimeOnly value) => ApplyLater();

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
