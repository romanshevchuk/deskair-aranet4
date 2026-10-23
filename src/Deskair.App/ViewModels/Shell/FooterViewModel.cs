using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;
using Deskair.Sensors.Aranet4;
using Deskair.Sensors.Simulated;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Platform;
using Deskair.Services.Sensors;

namespace Deskair.App.ViewModels.Shell;

/// <summary>The footer: status dot, "Connected · updated 12 s ago", the sensor model and the battery. The hardware ID is only in the tooltip. Text is already localized.</summary>
public sealed partial class FooterViewModel : ObservableObject
{
    private readonly DeviceRegistry devices;
    private readonly IMessenger messenger;
    private readonly IUiDispatcher dispatcher;
    private readonly TimeProvider timeProvider;
    private readonly ILocalizer localizer;
    private ConnectionState state = ConnectionState.FirstRun;
    private DateTimeOffset? lastReadingAt;
    private bool windowShown = true;

    /// <summary>The colour of the dot as a zone: green when connected, amber when stale, none (grey) otherwise.</summary>
    [ObservableProperty]
    private Zone? dotZone;

    [ObservableProperty]
    private string? statusText;

    /// <summary>"Syncing history… 42 of 154" while the history downloads; null otherwise.</summary>
    [ObservableProperty]
    private string? syncText;

    /// <summary>"Aranet4". Null without a sensor.</summary>
    [ObservableProperty]
    private string? detailText;

    /// <summary>The hardware ID of the sensor, for the tooltip of the detail text. Null without a sensor.</summary>
    [ObservableProperty]
    private string? detailTooltip;

    /// <summary>"Sensor battery is low (8%)" for the banner; null unless the state is low battery.</summary>
    [ObservableProperty]
    private string? lowBatteryText;

    [ObservableProperty]
    private int? batteryPercent;

    [ObservableProperty]
    private string? batteryText;

    public FooterViewModel(
        DeviceRegistry devices,
        IMessenger messenger,
        IUiDispatcher dispatcher,
        TimeProvider timeProvider,
        ILocalizer localizer)
    {
        this.devices = devices;
        this.messenger = messenger;
        this.dispatcher = dispatcher;
        this.timeProvider = timeProvider;
        this.localizer = localizer;
    }

    /// <summary>Shows the state, starts listening and ticks every second so "updated N s ago" stays current. Stops when cancelled.</summary>
    public void Start(ConnectionState initial, DateTimeOffset? lastReading, CancellationToken cancellationToken)
    {
        state = initial;
        lastReadingAt = lastReading;
        BatteryPercent = null;
        BatteryText = null;

        messenger.UnregisterAll(this);
        messenger.Register<FooterViewModel, ConnectionStateChanged>(this, static (self, message) => self.OnState(message.State));
        messenger.Register<FooterViewModel, ReadingReceived>(this, static (self, message) => self.OnReading(message));
        messenger.Register<FooterViewModel, SensorStatusUpdated>(this, static (self, message) => self.OnStatus(message));
        messenger.Register<FooterViewModel, DeviceSelected>(this, static (self, _) => self.OnDeviceSelected());
        messenger.Register<FooterViewModel, DeviceRenamed>(this, static (self, _) => self.OnRenamed());
        messenger.Register<FooterViewModel, SyncStarted>(this, static (self, _) => self.ShowSync(null));
        messenger.Register<FooterViewModel, SyncProgressed>(this, static (self, message) => self.ShowSync(message.Progress));
        messenger.Register<FooterViewModel, SyncCompleted>(this, static (self, _) => self.ShowSync(null, hide: true));
        cancellationToken.Register(() => messenger.UnregisterAll(this));

        Refresh();
        _ = TickAsync(cancellationToken);
    }

    /// <summary>Tells the footer whether anyone can see it. While the window is hidden or minimized the second-by-second refresh is skipped; showing it again refreshes at once.</summary>
    public void SetWindowShown(bool shown)
    {
        var wasHidden = !windowShown;
        windowShown = shown;

        if (shown && wasHidden)
        {
            dispatcher.Post(Refresh);
        }
    }

    /// <summary>Works out every text from what is known now.</summary>
    public void Refresh()
    {
        var now = timeProvider.GetUtcNow();
        var age = lastReadingAt is { } at ? now - at : (TimeSpan?)null;

        DotZone = state switch
        {
            ConnectionState.Normal or ConnectionState.LowBattery => Zone.Good,
            ConnectionState.Stale => Zone.Elevated,
            _ => null
        };
        StatusText = state switch
        {
            ConnectionState.FirstRun => localizer.Get("Footer_NoSensor"),
            ConnectionState.Connecting => Connecting(age),
            ConnectionState.NotConnected => localizer.Get("Footer_NotConnected"),
            ConnectionState.Stale => localizer.Format("Banner_Stale", ("min", Minutes(age))),
            _ => Connected(age)
        };
        DetailText = Detail();
        DetailTooltip = state == ConnectionState.FirstRun ? null : devices.Selected?.Address;
        LowBatteryText = state == ConnectionState.LowBattery && BatteryText is { } battery
            ? localizer.Format("Banner_LowBattery", ("percent", battery))
            : null;
    }

    private static string ModelKey(DeviceId device) => device.ProviderId switch
    {
        Aranet4Provider.ProviderId => "Sensor_Aranet4_Name",
        SimulatedProvider.ProviderId => "Sensor_Simulated_Name",
        _ => "Sensor_Aranet4_Name"
    };

    private string Connecting(TimeSpan? age)
    {
        var minutes = Minutes(age);

        return minutes < 1
            ? localizer.Get("Footer_Connecting")
            : localizer.Format("Footer_ConnectingLast", ("ago", localizer.Duration(minutes)));
    }

    private static int Minutes(TimeSpan? age) => (int)Math.Floor((age ?? TimeSpan.Zero).TotalMinutes);

    private string Connected(TimeSpan? age)
    {
        var seconds = (int)Math.Floor((age ?? TimeSpan.Zero).TotalSeconds);

        return seconds < 60
            ? localizer.Format("Footer_Connected", ("sec", seconds))
            : localizer.Format("Footer_ConnectedMinutes", ("min", seconds / 60));
    }

    private string? Detail()
    {
        if (devices.Selected is not { } device || state == ConnectionState.FirstRun)
        {
            return null;
        }

        return localizer.Get(ModelKey(device));
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), timeProvider, cancellationToken);

                if (windowShown)
                {
                    dispatcher.Post(Refresh);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelling is how the tick is stopped; it is not an error.
        }
    }

    private void OnState(ConnectionState newState)
    {
        dispatcher.Post(() =>
        {
            state = newState;
            Refresh();
        });
    }

    private void OnReading(ReadingReceived message)
    {
        if (message.Device != devices.Selected)
        {
            return;
        }

        dispatcher.Post(() =>
        {
            lastReadingAt = message.Reading.Timestamp;
            Refresh();
        });
    }

    private void OnStatus(SensorStatusUpdated message)
    {
        if (message.Device != devices.Selected)
        {
            return;
        }

        dispatcher.Post(() =>
        {
            BatteryPercent = message.Status.BatteryPercent;
            BatteryText = message.Status.BatteryPercent is { } percent ? NumberFormat.Percent(percent / 100.0, localizer.Culture) : null;
            Refresh();
        });
    }

    private void OnRenamed()
    {
        dispatcher.Post(Refresh);
    }

    private void OnDeviceSelected()
    {
        dispatcher.Post(() =>
        {
            lastReadingAt = null;
            BatteryPercent = null;
            BatteryText = null;
            Refresh();
        });
    }

    /// <summary>The share of the download that has arrived, as text in the selected culture. Never above 100 %.</summary>
    private string Percent(HistoryProgress progress)
    {
        var share = progress.Total <= 0 ? 0 : Math.Min(1.0, (double)progress.Done / progress.Total);

        return NumberFormat.Percent(share, localizer.Culture);
    }

    private void ShowSync(HistoryProgress? progress, bool hide = false)
    {
        dispatcher.Post(() => SyncText = hide
            ? null
            : progress is { } value
                ? localizer.Format("Footer_Syncing", ("percent", Percent(value)))
                : localizer.Get("Footer_SyncingStart"));
    }
}
