using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.Domain.Metrics;
using Deskair.Domain.Zones;
using Deskair.Services.Alerts;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Platform;
using Deskair.Services.Settings;

namespace Deskair.App.ViewModels.Tray;

/// <summary>
/// What the tray icon shows: the zone and value for the icon, whether the value replaces the ring (a setting), and the tooltip ("Office · 1,068 ppm · Elevated").
/// It follows readings, the connection state, the room, the language and the setting by itself, so the icon colour always updates (behaviour-rules.md "Alerts").
/// The icon is grey while the sensor is not connected or there is no sensor yet. The menu is <see cref="TrayMenuViewModel"/>.
/// </summary>
public sealed partial class TrayViewModel : ObservableObject
{
    private readonly DeviceRegistry devices;
    private readonly ILocalizer localizer;
    private readonly MetricFormatter formatter;
    private readonly IUiDispatcher dispatcher;
    private readonly SettingsService settings;
    private bool doNotDisturb;
    private ConnectionState connection = ConnectionState.Normal;

    /// <summary>The zone of the last reading, or null (grey icon) when there is none or the sensor is not connected.</summary>
    [ObservableProperty]
    private Zone? zone;

    /// <summary>The CO₂ value of the last reading, for the value icon. Null whenever <see cref="Zone"/> is.</summary>
    [ObservableProperty]
    private double? co2;

    /// <summary>True when the icon shows the value ("1.1k") instead of the ring.</summary>
    [ObservableProperty]
    private bool showValue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private string tooltip = string.Empty;

    public TrayViewModel(
        DeviceRegistry devices,
        ILocalizer localizer,
        MetricFormatter formatter,
        IMessenger messenger,
        IUiDispatcher dispatcher,
        SettingsService settings,
        IDoNotDisturb doNotDisturb)
    {
        this.devices = devices;
        this.localizer = localizer;
        this.formatter = formatter;
        this.dispatcher = dispatcher;
        this.settings = settings;
        this.doNotDisturb = doNotDisturb.IsActive;
        ShowValue = settings.Current.ShowValueOnTrayIcon;

        messenger.Register<TrayViewModel, ReadingReceived>(this, static (self, message) => self.OnReading(message));
        messenger.Register<TrayViewModel, ConnectionStateChanged>(this, static (self, message) => self.OnConnection(message.State));
        messenger.Register<TrayViewModel, DeviceSelected>(this, static (self, _) => self.OnDeviceSelected());
        messenger.Register<TrayViewModel, DeviceRenamed>(this, static (self, _) => self.dispatcher.Post(self.UpdateTooltip));
        messenger.Register<TrayViewModel, LanguageChanged>(this, static (self, _) => self.dispatcher.Post(self.UpdateTooltip));
        messenger.Register<TrayViewModel, SettingsChanged>(this, static (self, message) => self.dispatcher.Post(() => self.OnSettings(message.Settings)));
        messenger.Register<TrayViewModel, DoNotDisturbChanged>(this, static (self, message) => self.dispatcher.Post(() => self.OnDoNotDisturb(message.IsActive)));

        UpdateTooltip();
    }

    /// <summary>The first line of the menu: the same status as the tooltip.</summary>
    public string StatusText => Tooltip;

    private void OnReading(ReadingReceived message)
    {
        if (message.Device != devices.Selected)
        {
            return;
        }

        dispatcher.Post(() =>
        {
            Co2 = message.Reading.Co2Ppm;
            Zone = double.IsNaN(message.Reading.Co2Ppm) ? null : ZoneClassifier.Classify(message.Reading.Co2Ppm);
            UpdateTooltip();
        });
    }

    private void OnConnection(ConnectionState state)
    {
        dispatcher.Post(() =>
        {
            connection = state;

            if (state is ConnectionState.NotConnected or ConnectionState.FirstRun)
            {
                Zone = null;
                Co2 = null;
            }

            UpdateTooltip();
        });
    }

    private void OnDeviceSelected()
    {
        dispatcher.Post(() =>
        {
            Zone = null;
            Co2 = null;
            UpdateTooltip();
        });
    }

    private void OnSettings(AppSettings current)
    {
        ShowValue = current.ShowValueOnTrayIcon;
        UpdateTooltip();
    }

    private void OnDoNotDisturb(bool active)
    {
        doNotDisturb = active;
        UpdateTooltip();
    }

    private void UpdateTooltip()
    {
        var status = BuildStatus();
        Tooltip = doNotDisturb && settings.Current.AlertsEnabled ? $"{status} · {localizer.Get("Tray_AlertsPausedByWindows")}" : status;
    }

    private string BuildStatus()
    {
        var appName = localizer.Get("Tray_AppName");

        if (connection == ConnectionState.FirstRun || devices.Selected is not { } selected || devices.Info(selected) is not { } info)
        {
            return appName;
        }

        if (connection == ConnectionState.NotConnected)
        {
            return $"{info.RoomName} · {localizer.Get("Footer_NotConnected")}";
        }

        if (Zone is not { } known || Co2 is not { } value)
        {
            return info.RoomName;
        }

        return $"{info.RoomName} · {formatter.Value(Metric.Co2, value)} {formatter.Unit(Metric.Co2)} · {localizer.Get($"Zone_{known}")}";
    }
}
