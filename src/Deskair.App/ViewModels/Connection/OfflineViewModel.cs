using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Platform;
using Deskair.Services.Queries;

namespace Deskair.App.ViewModels.Connection;

/// <summary>
/// The full-screen "can't reach your sensor" view: what is probably wrong (Bluetooth off, or the sensor out of range or asleep), the last reading,
/// recovery steps, and the two buttons that help. It follows readings, the sensor status and the Bluetooth radio on its own, so it needs no start-up call;
/// when the sensor cannot be reached from the start it takes the last reading from the stored history.
/// </summary>
public sealed partial class OfflineViewModel : RetryingViewModel
{
    private static readonly TimeSpan lookBack = TimeSpan.FromDays(7);

    private readonly DeviceRegistry devices;
    private readonly ReadingQueryService query;
    private readonly IBluetoothRadio radio;
    private readonly IMessenger messenger;
    private readonly IUiDispatcher dispatcher;
    private readonly MetricFormatter formatter;
    private readonly ILocalizer localizer;
    private readonly FriendlyTime time;
    private readonly ISystemSettings systemSettings;
    private Reading? lastReading;
    private int? batteryPercent;

    /// <summary>True when the Bluetooth radio is off: the view then offers to turn it on.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Message))]
    private bool bluetoothOff;

    /// <summary>"Your last reading was 1,068 ppm at 20:24", or null when no reading is known.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Message))]
    private string? lastReadingText;

    /// <summary>What to try, as short sentences. The battery step shows once the battery level is known.</summary>
    [ObservableProperty]
    private IReadOnlyList<string> steps = [];

    public OfflineViewModel(
        DeviceRegistry devices,
        ReadingQueryService query,
        IBluetoothRadio radio,
        IMessenger messenger,
        IUiDispatcher dispatcher,
        MetricFormatter formatter,
        ILocalizer localizer,
        FriendlyTime time,
        ISystemSettings systemSettings)
        : base(messenger, dispatcher, localizer)
    {
        this.devices = devices;
        this.query = query;
        this.radio = radio;
        this.messenger = messenger;
        this.dispatcher = dispatcher;
        this.formatter = formatter;
        this.localizer = localizer;
        this.time = time;
        this.systemSettings = systemSettings;

        messenger.Register<OfflineViewModel, ReadingReceived>(this, static (self, message) => self.OnReading(message));
        messenger.Register<OfflineViewModel, SensorStatusUpdated>(this, static (self, message) => self.OnStatus(message));
        messenger.Register<OfflineViewModel, DeviceSelected>(this, static (self, _) => self.OnDeviceSelected());
        messenger.Register<OfflineViewModel, ConnectionStateChanged>(this, static (self, message) => self.OnStateChanged(message));
        messenger.Register<OfflineViewModel, LanguageChanged>(this, static (self, _) => self.OnLanguageChanged());
        radio.StateChanged += (_, _) => dispatcher.Post(UpdateRadio);

        UpdateRadio();
        UpdateSteps();
    }

    /// <summary>The paragraph under the title: the likely cause, the last reading and that History still works.</summary>
    public string Message => string.Join(
        ' ',
        new[] { localizer.Get(BluetoothOff ? "Offline_Text_BluetoothOff" : "Offline_Text"), LastReadingText, localizer.Get("Offline_HistoryHint") }
            .Where(part => part is not null));

    [RelayCommand]
    private void TurnOnBluetooth()
    {
        systemSettings.OpenBluetoothSettings();
    }

    private void UpdateRadio()
    {
        BluetoothOff = !radio.IsOn;
    }

    private void OnReading(ReadingReceived message)
    {
        if (message.Device != devices.Selected)
        {
            return;
        }

        dispatcher.Post(() => ShowLastReading(message.Reading));
    }

    private void OnStatus(SensorStatusUpdated message)
    {
        if (message.Device != devices.Selected)
        {
            return;
        }

        dispatcher.Post(() =>
        {
            EndRetry();
            batteryPercent = message.Status.BatteryPercent;
            UpdateSteps();
        });
    }

    private void OnDeviceSelected()
    {
        dispatcher.Post(() =>
        {
            EndRetry();
            lastReading = null;
            batteryPercent = null;
            LastReadingText = null;
            UpdateSteps();
        });
    }

    private void OnLanguageChanged()
    {
        dispatcher.Post(() =>
        {
            if (lastReading is { } reading)
            {
                ShowLastReading(reading);
            }

            UpdateSteps();
            OnPropertyChanged(nameof(Message));
            RefreshRetryText();
        });
    }

    private void OnStateChanged(ConnectionStateChanged message)
    {
        if (message.State == ConnectionState.NotConnected && lastReading is null)
        {
            _ = LoadLastReadingAsync();
        }
    }

    private async Task LoadLastReadingAsync()
    {
        if (devices.Selected is not { } device)
        {
            return;
        }

        try
        {
            var recent = await query.GetRecentAsync(device, lookBack, CancellationToken.None);

            if (recent.Count > 0)
            {
                // By the time the history is read the room may have changed or a live reading may have arrived: the newer state wins.
                dispatcher.Post(() =>
                {
                    if (devices.Selected == device && lastReading is null)
                    {
                        ShowLastReading(recent[^1]);
                    }
                });
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The last reading is a nicety on this screen; without it the view still explains what to do.
        }
    }

    private void ShowLastReading(Reading reading)
    {
        lastReading = reading;
        LastReadingText = localizer.Format(
            "Offline_LastReading",
            ("value", $"{formatter.Value(Metric.Co2, reading.Co2Ppm)} {formatter.Unit(Metric.Co2)}"),
            ("time", time.Text(reading.Timestamp)));
    }


    private void UpdateSteps()
    {
        var list = new List<string> { localizer.Get("Offline_Step_Range"), localizer.Get("Offline_Step_Wake"), localizer.Get("Offline_Step_SmartHome") };

        if (batteryPercent is { } percent)
        {
            list.Add(localizer.Format("Offline_Step_Battery", ("percent", NumberFormat.Percent(percent / 100.0, localizer.Culture))));
        }

        Steps = list;
    }
}
