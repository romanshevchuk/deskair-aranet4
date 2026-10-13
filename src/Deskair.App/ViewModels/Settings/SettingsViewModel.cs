using CommunityToolkit.Mvvm.ComponentModel;

namespace Deskair.App.ViewModels.Settings;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppearanceSettingsViewModel appearance;
    private readonly AlertsSettingsViewModel alerts;
    private readonly SleepSettingsViewModel sleep;
    private readonly DevicesSettingsViewModel devices;
    private readonly DataSettingsViewModel data;
    private readonly StartupSettingsViewModel startup;

    public AppearanceSettingsViewModel Appearance => appearance;

    public AlertsSettingsViewModel Alerts => alerts;

    public SleepSettingsViewModel Sleep => sleep;

    public DevicesSettingsViewModel Devices => devices;

    public DataSettingsViewModel Data => data;

    public StartupSettingsViewModel Startup => startup;

    public SettingsViewModel(AppearanceSettingsViewModel appearance, AlertsSettingsViewModel alerts, SleepSettingsViewModel sleep, DevicesSettingsViewModel devices, DataSettingsViewModel data, StartupSettingsViewModel startup)
    {
        this.appearance = appearance;
        this.alerts = alerts;
        this.sleep = sleep;
        this.devices = devices;
        this.data = data;
        this.startup = startup;
    }

    /// <summary>Loads every section. A section that fails does not stop the others; the failures are rethrown together at the end.</summary>
    public Task LoadAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            Run(() =>
            {
                appearance.Load();
                return Task.CompletedTask;
            }),
            Run(() =>
            {
                alerts.Load();
                return Task.CompletedTask;
            }),
            Run(() =>
            {
                sleep.Load();
                return Task.CompletedTask;
            }),
            Run(() => devices.LoadAsync(cancellationToken)),
            Run(() => data.LoadAsync(cancellationToken)),
            Run(() => startup.LoadAsync(cancellationToken)));
    }

    private static async Task Run(Func<Task> load)
    {
        await load();
    }
}
