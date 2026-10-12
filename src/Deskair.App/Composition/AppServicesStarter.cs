using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Connection;
using Deskair.App.ViewModels.Live;
using Deskair.App.ViewModels.Shell;
using Deskair.Domain.Readings;
using Deskair.Sensors.Simulated;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Live;
using Deskair.Services.Messages;
using Deskair.Services.Settings;
using Deskair.Services.Storage;
using Microsoft.Extensions.Logging;

namespace Deskair.App.Composition;

/// <summary>
/// Starts the parts of the app that need no window: the known devices, the view models that follow readings, the connection tracker and
/// the sensor session. The simulated app creates two simulated rooms on first run. Settings are loaded before this runs.
/// </summary>
public sealed class AppServicesStarter
{
    private static readonly TimeSpan simulatedInterval = TimeSpan.FromMinutes(2);

    private readonly AppOptions options;
    private readonly SettingsService settings;
    private readonly DeviceRegistry registry;
    private readonly IDeviceStore deviceStore;
    private readonly IReadingStore readings;
    private readonly IMessenger messenger;
    private readonly SensorSession session;
    private readonly ConnectionTracker tracker;
    private readonly LiveViewModel live;
    private readonly FooterViewModel footer;
    private readonly RoomChipViewModel room;
    private readonly ConnectionViewModel connection;
    private readonly ILogger<AppServicesStarter> logger;

    public AppServicesStarter(
        AppOptions options,
        SettingsService settings,
        DeviceRegistry registry,
        IDeviceStore deviceStore,
        IReadingStore readings,
        IMessenger messenger,
        SensorSession session,
        ConnectionTracker tracker,
        LiveViewModel live,
        FooterViewModel footer,
        RoomChipViewModel room,
        ConnectionViewModel connection,
        ILogger<AppServicesStarter> logger)
    {
        this.options = options;
        this.settings = settings;
        this.registry = registry;
        this.deviceStore = deviceStore;
        this.readings = readings;
        this.messenger = messenger;
        this.session = session;
        this.tracker = tracker;
        this.live = live;
        this.footer = footer;
        this.room = room;
        this.connection = connection;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await LoadDevicesAsync(cancellationToken);

        room.Load();
        room.Start(cancellationToken);

        var lastReading = registry.Selected is { } selected ? await readings.GetLastReadingTimeAsync(selected, cancellationToken) : null;

        footer.Start(ConnectionState.FirstRun, lastReading, cancellationToken);
        connection.Update(ConnectionState.FirstRun);

        messenger.Register<AppServicesStarter, DeviceSelected>(this, static (self, message) => self.Restart(message.Device));
        messenger.Register<AppServicesStarter, RetryRequested>(this, static (self, _) => self.RestartSelected());
        cancellationToken.Register(() => messenger.UnregisterAll(this));

        await tracker.StartAsync(lastReading, cancellationToken);
        await live.StartAsync(cancellationToken);

        if (registry.Selected is { } device)
        {
            await session.StartAsync(device);
        }
    }

    private async Task LoadDevicesAsync(CancellationToken cancellationToken)
    {
        var known = await registry.LoadAsync(settings.Current.SelectedDevice, cancellationToken);

        if (options.Simulated && known.Count == 0)
        {
            await SeedSimulatedRoomsAsync(cancellationToken);
            await registry.LoadAsync(settings.Current.SelectedDevice, cancellationToken);
        }
    }

    private async Task SeedSimulatedRoomsAsync(CancellationToken cancellationToken)
    {
        var interval = new IntervalChange(DateTimeOffset.UnixEpoch, simulatedInterval);

        await deviceStore.SaveAsync(new DeviceInfo(new DeviceId(SimulatedProvider.ProviderId, "SIM-0001"), "Office", null, [interval], null), cancellationToken);
        await deviceStore.SaveAsync(new DeviceInfo(new DeviceId(SimulatedProvider.ProviderId, "SIM-0002"), "Bedroom", null, [interval], null), cancellationToken);
    }

    private void RestartSelected()
    {
        if (registry.Selected is { } device)
        {
            Restart(device);
        }
    }

    private void Restart(DeviceId device)
    {
        _ = RestartAsync(device);
    }

    private async Task RestartAsync(DeviceId device)
    {
        try
        {
            await session.StartAsync(device);
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(exception, "Starting the sensor {Address} failed.", device.Address);
            }
        }
    }
}
