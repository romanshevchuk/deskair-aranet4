using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.ViewModels.Shell;
using Deskair.Domain.Metrics;
using Deskair.Domain.Zones;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Platform;

namespace Deskair.App.ViewModels.Tray;

/// <summary>
/// What the tray icon shows and offers: the zone and value for the icon, the tooltip ("Office · 1,068 ppm · Elevated"), the menu texts and commands.
/// It follows readings, the connection state, the room and the language by itself, so the icon colour always updates (behaviour-rules.md "Alerts").
/// The icon is grey while the sensor is not connected or there is no sensor yet.
/// </summary>
public sealed partial class TrayViewModel : ObservableObject
{
    private readonly DeviceRegistry devices;
    private readonly ILocalizer localizer;
    private readonly MetricFormatter formatter;
    private readonly IUiDispatcher dispatcher;
    private readonly IMainWindowController windows;
    private ConnectionState connection = ConnectionState.Normal;

    /// <summary>The zone of the last reading, or null (grey icon) when there is none or the sensor is not connected.</summary>
    [ObservableProperty]
    private Zone? zone;

    /// <summary>The CO₂ value of the last reading, for the value icon. Null whenever <see cref="Zone"/> is.</summary>
    [ObservableProperty]
    private double? co2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private string tooltip = string.Empty;

    [ObservableProperty]
    private string openText = string.Empty;

    [ObservableProperty]
    private string settingsText = string.Empty;

    [ObservableProperty]
    private string quitText = string.Empty;

    public TrayViewModel(
        DeviceRegistry devices,
        ILocalizer localizer,
        MetricFormatter formatter,
        IMessenger messenger,
        IUiDispatcher dispatcher,
        IMainWindowController windows)
    {
        this.devices = devices;
        this.localizer = localizer;
        this.formatter = formatter;
        this.dispatcher = dispatcher;
        this.windows = windows;

        messenger.Register<TrayViewModel, ReadingReceived>(this, static (self, message) => self.OnReading(message));
        messenger.Register<TrayViewModel, ConnectionStateChanged>(this, static (self, message) => self.OnConnection(message.State));
        messenger.Register<TrayViewModel, DeviceSelected>(this, static (self, _) => self.OnDeviceSelected());
        messenger.Register<TrayViewModel, DeviceRenamed>(this, static (self, _) => self.dispatcher.Post(self.UpdateTexts));
        messenger.Register<TrayViewModel, LanguageChanged>(this, static (self, _) => self.dispatcher.Post(self.UpdateTexts));

        UpdateTexts();
    }

    /// <summary>The first line of the menu: the same status as the tooltip.</summary>
    public string StatusText => Tooltip;

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
            UpdateTexts();
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

            UpdateTexts();
        });
    }

    private void OnDeviceSelected()
    {
        dispatcher.Post(() =>
        {
            Zone = null;
            Co2 = null;
            UpdateTexts();
        });
    }

    private void UpdateTexts()
    {
        Tooltip = BuildTooltip();
        OpenText = localizer.Get("Action_OpenApp");
        SettingsText = localizer.Get("Nav_Settings");
        QuitText = localizer.Get("Tray_Quit");
    }

    private string BuildTooltip()
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
