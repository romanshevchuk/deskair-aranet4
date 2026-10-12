using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Services.Settings;

namespace Deskair.App.ViewModels.Settings;

public sealed partial class AlertsSettingsViewModel : ObservableObject
{
    private readonly SettingsService settings;

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

    public AlertsSettingsViewModel(SettingsService settings)
    {
        this.settings = settings;
    }

    public void Load()
    {
        var current = settings.Current;

        AlertsEnabled = current.AlertsEnabled;
        NotifyAtElevated = current.NotifyAtElevated;
        NotifyAtHigh = current.NotifyAtHigh;
        QuietHoursEnabled = current.QuietHoursEnabled;
        QuietHoursStart = current.QuietHoursStart;
        QuietHoursEnd = current.QuietHoursEnd;
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
}
