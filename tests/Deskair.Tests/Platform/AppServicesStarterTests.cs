using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Composition;
using Deskair.App.Localization;
using Deskair.App.ViewModels.Connection;
using Deskair.App.ViewModels.Live;
using Deskair.App.ViewModels.Shell;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Sensors.Simulated;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Live;
using Deskair.Services.Messages;
using Deskair.Services.Queries;
using Deskair.Services.Settings;
using Deskair.Services.Sync;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.Platform;

public sealed class AppServicesStarterTests : IAsyncDisposable
{
    private static readonly DateTimeOffset now = new(2026, 10, 12, 18, 40, 0, TimeSpan.Zero);

    private readonly FakeClock clock = new(now);
    private readonly FakeReadingStore readings = new();
    private readonly FakeDeviceStore deviceStore = new();
    private readonly FakeSettingsStore settingsStore = new();
    private readonly StrongReferenceMessenger messenger = new();
    private readonly FakeBluetoothRadio radio = new();
    private readonly FakeSensor sensor = new();
    private readonly FakeSensorProvider provider;
    private readonly DeviceRegistry registry;
    private readonly SettingsService settings;
    private readonly SensorSession session;
    private readonly ConnectionViewModel connection;
    private readonly FooterViewModel footer;
    private readonly RoomChipViewModel room;
    private readonly LiveViewModel live;
    private readonly ListLogger<AppServicesStarter> logger = new();
    private readonly CancellationTokenSource cts = new();

    public AppServicesStarterTests()
    {
        sensor.OnReadCurrent = call => Samples.Co2(clock.GetUtcNow(), 800 + call);
        provider = new FakeSensorProvider(SimulatedProvider.ProviderId, sensor);
        var dispatcher = new FakeUiDispatcher();
        var localizer = new FakeLocalizer();
        var calendar = new LocalCalendar(TimeZoneInfo.Utc);
        registry = new DeviceRegistry(deviceStore);
        settings = new SettingsService(settingsStore, messenger);
        var formatter = TestFormatter.Create(localizer);
        var query = new ReadingQueryService(readings, registry, clock, calendar);
        session = new SensorSession(
            [provider],
            new LiveFeed(readings, messenger, clock, NullLogger<LiveFeed>.Instance),
            new SyncService(readings, deviceStore, clock, NullLogger<SyncService>.Instance),
            messenger,
            clock,
            NullLogger<SensorSession>.Instance);
        connection = new ConnectionViewModel(messenger, dispatcher);
        footer = new FooterViewModel(registry, messenger, dispatcher, clock, localizer);
        room = new RoomChipViewModel(registry, settings, messenger, dispatcher, formatter);
        live = new LiveViewModel(
            new HeroViewModel(localizer, new HeroStatsBuilder(localizer, formatter, calendar), formatter),
            new GaugeViewModel(),
            new MetricTabsViewModel(),
            new ChartCardViewModel(query, registry, formatter, localizer, clock),
            new MiniCardsViewModel(localizer, formatter),
            query,
            registry,
            messenger,
            dispatcher,
            clock,
            NullLogger<LiveViewModel>.Instance);
        Tracker = new ConnectionTracker(registry, radio, messenger, clock, new ListLogger<ConnectionTracker>());
    }

    private ConnectionTracker Tracker { get; }

    public async ValueTask DisposeAsync()
    {
        await cts.CancelAsync();
        await session.DisposeAsync();
        cts.Dispose();
    }

    private AppServicesStarter Create(bool simulated)
        => new(new AppOptions(false, simulated), settings, registry, deviceStore, readings, messenger, session, Tracker, live, footer, room, connection, logger);

    [Fact]
    public async Task The_simulated_app_creates_two_rooms_on_first_run_and_shows_the_first()
    {
        await Create(simulated: true).StartAsync(cts.Token);

        Assert.Equal(["Office", "Bedroom"], deviceStore.Snapshot().Select(device => device.RoomName));
        Assert.Equal("SIM-0001", registry.Selected?.Address);
        Assert.Equal(SimulatedProvider.ProviderId, registry.Selected?.ProviderId);
        Assert.Equal(["Office", "Bedroom"], room.Rooms.Select(item => item.Name));
    }

    [Fact]
    public async Task The_simulated_app_starts_the_sensor_and_readings_flow()
    {
        await Create(simulated: true).StartAsync(cts.Token);

        await Wait.Until(() => provider.Created.Count == 1 && live.Hero.Co2Ppm is not null);
        Assert.Equal("SIM-0001", provider.Created[0].Address);
    }

    [Fact]
    public async Task The_simulated_rooms_are_only_created_once()
    {
        await Create(simulated: true).StartAsync(cts.Token);
        await cts.CancelAsync();
        await session.StopAsync();

        // The next launch finds the rooms already there.
        using var nextLaunch = new CancellationTokenSource();
        await Create(simulated: true).StartAsync(nextLaunch.Token);
        await nextLaunch.CancelAsync();

        Assert.Equal(2, deviceStore.Snapshot().Count);
    }

    [Fact]
    public async Task The_real_app_without_a_device_creates_nothing_and_starts_no_sensor()
    {
        await Create(simulated: false).StartAsync(cts.Token);

        Assert.Empty(deviceStore.Snapshot());
        Assert.Null(registry.Selected);
        Assert.Empty(provider.Created);
        Assert.Equal(ConnectionState.FirstRun, Tracker.Current);
        Assert.Equal("Footer_NoSensor", footer.StatusText);
    }

    [Fact]
    public async Task The_room_chosen_last_time_is_restored()
    {
        var bedroom = new DeviceId(SimulatedProvider.ProviderId, "SIM-0002");
        deviceStore.Devices.Add(new DeviceInfo(new DeviceId(SimulatedProvider.ProviderId, "SIM-0001"), "Office", null, [], null));
        deviceStore.Devices.Add(new DeviceInfo(bedroom, "Bedroom", null, [], null));
        settingsStore.Stored = AppSettings.Default with { SelectedDevice = bedroom };
        await settings.LoadAsync(cts.Token);

        await Create(simulated: false).StartAsync(cts.Token);

        Assert.Equal(bedroom, registry.Selected);
        await Wait.Until(() => provider.Created.Count == 1);
        Assert.Equal("SIM-0002", provider.Created[0].Address);
    }

    [Fact]
    public async Task Choosing_another_room_starts_that_sensor()
    {
        await Create(simulated: true).StartAsync(cts.Token);
        await Wait.Until(() => provider.Created.Count == 1);

        await room.ChooseRoomCommand.ExecuteAsync(room.Rooms[1]);

        await Wait.Until(() => provider.Created.Count == 2);
        Assert.Equal("SIM-0002", provider.Created[1].Address);
    }

    [Fact]
    public async Task Try_again_restarts_the_sensor_of_the_selected_room()
    {
        await Create(simulated: true).StartAsync(cts.Token);
        await Wait.Until(() => provider.Created.Count == 1);

        connection.RetryCommand.Execute(null);

        await Wait.Until(() => provider.Created.Count == 2);
        Assert.Equal("SIM-0001", provider.Created[1].Address);
    }

    [Fact]
    public async Task The_connection_state_reaches_the_view_models()
    {
        await Create(simulated: true).StartAsync(cts.Token);

        await Wait.Until(() => connection.State == ConnectionState.Normal);
        Assert.NotNull(footer.StatusText);
        Assert.Equal(Deskair.Domain.Zones.Zone.Good, footer.DotZone);
    }

    [Fact]
    public async Task After_cancelling_a_chosen_room_no_longer_starts_a_sensor()
    {
        await Create(simulated: true).StartAsync(cts.Token);
        await Wait.Until(() => provider.Created.Count == 1);
        await cts.CancelAsync();

        messenger.Send(new DeviceSelected(new DeviceId(SimulatedProvider.ProviderId, "SIM-0002")));
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.Single(provider.Created);
        Assert.DoesNotContain(logger.Entries, entry => entry.Level >= LogLevel.Error);
    }

    [Fact]
    public async Task Silence_turns_the_connection_stale_without_any_new_event()
    {
        sensor.OnReadCurrent = _ => throw new InvalidOperationException("no reading");
        deviceStore.Devices.Add(new DeviceInfo(new DeviceId(SimulatedProvider.ProviderId, "SIM-0001"), "Office", null, [new IntervalChange(now.AddYears(-1), TimeSpan.FromMinutes(2))], null));
        readings.Readings.Add(Samples.Co2(now.AddMinutes(-1), 700));
        await Create(simulated: false).StartAsync(cts.Token);
        await Wait.Until(() => connection.State == ConnectionState.Normal);

        clock.Advance(TimeSpan.FromMinutes(7));

        await Wait.Until(() => connection.State == ConnectionState.Stale);
    }
}
