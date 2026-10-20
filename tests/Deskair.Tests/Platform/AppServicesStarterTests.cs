using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Composition;
using Deskair.App.Localization;
using Deskair.App.ViewModels.Connection;
using Deskair.App.ViewModels.Live;
using Deskair.App.ViewModels.Onboarding;
using Deskair.App.ViewModels.Settings;
using Deskair.App.ViewModels.Shell;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Sensors.Simulated;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Import;
using Deskair.Services.Live;
using Deskair.Services.Messages;
using Deskair.Services.Queries;
using Deskair.Services.Settings;
using Deskair.Services.Sync;
using Deskair.Storage.Export;
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
    private readonly OnboardingViewModel onboarding;
    private readonly SettingsViewModel settingsScreen;
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
        connection = new ConnectionViewModel(messenger, dispatcher, localizer);
        onboarding = new OnboardingViewModel([provider], new DeviceEnrollment(deviceStore, registry, settings, messenger), radio, localizer, clock, new FakeUiDispatcher(), new ListLogger<OnboardingViewModel>());
        settingsScreen = new SettingsViewModel(
            new AppearanceSettingsViewModel(settings, messenger, dispatcher, NullLogger<AppearanceSettingsViewModel>.Instance),
            new AlertsSettingsViewModel(settings, new FakeDoNotDisturb(), messenger, dispatcher, NullLogger<AlertsSettingsViewModel>.Instance),
            new SleepSettingsViewModel(settings, NullLogger<SleepSettingsViewModel>.Instance),
            new DevicesSettingsViewModel(registry, new DeviceEnrollment(deviceStore, registry, settings, messenger), messenger, NullLogger<DevicesSettingsViewModel>.Instance),
            Deskair.Tests.ViewModels.DataSettingsViewModelTests.Create(new TempKnownFolders()),
            new StartupSettingsViewModel(new FakeStartupRegistration(), NullLogger<StartupSettingsViewModel>.Instance));
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

    private AppServicesStarter Create(bool simulated, string? sensor = null)
        => new(new AppOptions(false, simulated, Sensor: sensor), settings, registry, deviceStore, readings, messenger, session, Tracker, live, footer, room, connection, onboarding, settingsScreen, logger);

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
    public async Task A_named_sensor_is_registered_selected_and_started()
    {
        await Create(simulated: false, sensor: "c3:de:c8:fc:c7:c6").StartAsync(cts.Token);

        var device = Assert.Single(deviceStore.Snapshot());
        Assert.Equal(new DeviceId("aranet4", "C3DEC8FCC7C6"), device.Id);
        Assert.Equal(device.Id, registry.Selected);
    }

    [Fact]
    public async Task A_named_sensor_wins_over_a_device_remembered_from_an_earlier_run()
    {
        var earlier = new DeviceId("aranet4", "000000000001");
        await deviceStore.SaveAsync(new DeviceInfo(earlier, "Earlier", null, [new IntervalChange(DateTimeOffset.UnixEpoch, TimeSpan.FromMinutes(2))], null), cts.Token);
        await settings.UpdateAsync(current => current with { SelectedDevice = earlier }, cts.Token);

        await Create(simulated: false, sensor: "C3DEC8FCC7C6").StartAsync(cts.Token);

        Assert.Equal(new DeviceId("aranet4", "C3DEC8FCC7C6"), registry.Selected);
    }

    [Fact]
    public async Task A_named_sensor_is_registered_only_once()
    {
        await Create(simulated: false, sensor: "C3DEC8FCC7C6").StartAsync(cts.Token);
        await cts.CancelAsync();
        await session.StopAsync();

        using var nextLaunch = new CancellationTokenSource();
        await Create(simulated: false, sensor: "C3DEC8FCC7C6").StartAsync(nextLaunch.Token);
        await nextLaunch.CancelAsync();

        Assert.Single(deviceStore.Snapshot());
    }

    [Fact]
    public async Task A_sensor_address_that_is_not_an_address_registers_nothing()
    {
        await Create(simulated: false, sensor: "SIM-0001").StartAsync(cts.Token);

        Assert.Empty(deviceStore.Snapshot());
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
    public async Task The_real_app_without_a_device_starts_looking_for_sensors()
    {
        await Create(simulated: false).StartAsync(cts.Token);

        await Wait.Until(() => onboarding.NoSensorFound);
        Assert.True(onboarding.RoomName.Length > 0);
    }

    [Fact]
    public async Task With_a_device_the_welcome_screen_does_not_scan()
    {
        await Create(simulated: true).StartAsync(cts.Token);

        Assert.False(onboarding.IsScanning);
        Assert.False(onboarding.NoSensorFound);
    }

    [Fact]
    public async Task Pairing_on_first_run_starts_the_sensor_and_shows_the_room()
    {
        await Create(simulated: false).StartAsync(cts.Token);
        Assert.Equal(ConnectionState.FirstRun, Tracker.Current);

        await onboarding.PairCommand.ExecuteAsync(new NearbySensorItem(new DeviceId(SimulatedProvider.ProviderId, "SIM-0001"), "Simulated sensor", string.Empty));

        await Wait.Until(() => provider.Created.Count == 1 && connection.State == ConnectionState.Normal);
        Assert.False(connection.ShowFirstRunView);
        Assert.Equal(["Onboarding_Room_DefaultName"], room.Rooms.Select(item => item.Name));
        Assert.Equal("SIM-0001", registry.Selected?.Address);
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
        await Wait.Quiet();

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

    [Fact]
    public async Task Waking_from_sleep_connects_to_the_selected_sensor_again()
    {
        await Create(simulated: true).StartAsync(cts.Token);
        await Wait.Until(() => provider.Created.Count == 1);

        messenger.Send(new SystemResumed());

        await Wait.Until(() => provider.Created.Count == 2);
        Assert.All(provider.Created, device => Assert.Equal("SIM-0001", device.Address));
    }

    [Fact]
    public async Task Waking_from_sleep_without_a_sensor_does_nothing()
    {
        await Create(simulated: false).StartAsync(cts.Token);

        messenger.Send(new SystemResumed());
        await Wait.Quiet();

        Assert.Empty(provider.Created);
    }

    [Fact]
    public async Task Asking_for_a_sync_connects_to_the_selected_sensor_again()
    {
        await Create(simulated: true).StartAsync(cts.Token);
        await Wait.Until(() => provider.Created.Count == 1);

        messenger.Send(new SyncRequested());

        await Wait.Until(() => provider.Created.Count == 2);
        Assert.All(provider.Created, device => Assert.Equal("SIM-0001", device.Address));
    }
}
