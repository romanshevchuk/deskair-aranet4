using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Onboarding;
using Deskair.Domain.Readings;
using Deskair.Sensors.Simulated;
using Deskair.Services.Devices;
using Deskair.Services.Sensors;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class OnboardingViewModelTests
{
    private readonly FakeBluetoothRadio radio = new();
    private readonly ListLogger<OnboardingViewModel> logger = new();
    private readonly FakeClock clock = new(new DateTimeOffset(2026, 10, 12, 19, 0, 0, TimeSpan.Zero));
    private readonly FakeDeviceStore store = new();
    private readonly StrongReferenceMessenger messenger = new();
    private readonly DeviceRegistry registry;
    private readonly DeviceEnrollment enrollment;

    public OnboardingViewModelTests()
    {
        registry = new DeviceRegistry(store);
        enrollment = new DeviceEnrollment(store, registry, new SettingsService(new FakeSettingsStore(), messenger), messenger);
    }

    private OnboardingViewModel Create(params ISensorProvider[] providers)
    {
        return new OnboardingViewModel(providers, enrollment, radio, new FakeLocalizer(), clock, logger);
    }

    private static DiscoveredSensor Sensor(string provider, string address, int signal = -60)
    {
        return new DiscoveredSensor(new DeviceId(provider, address), address, signal);
    }

    [Fact]
    public async Task The_simulated_provider_is_found()
    {
        var viewModel = Create(new SimulatedProvider(new FakeClock(DateTimeOffset.UnixEpoch)));

        await viewModel.ScanAsync(TestContext.Current.CancellationToken);

        var found = Assert.Single(viewModel.Nearby);
        Assert.Equal(SimulatedProvider.ProviderId, found.Id.ProviderId);
        Assert.False(viewModel.BluetoothOff);
    }

    [Fact]
    public async Task Sensors_show_up_as_they_are_found_from_every_provider()
    {
        var first = new ControlledProvider("first");
        var second = new ControlledProvider("second");
        var viewModel = Create(first, second);

        var scan = viewModel.ScanAsync(TestContext.Current.CancellationToken);
        first.Found.Writer.TryWrite(Sensor("first", "A"));
        await Wait.Until(() => viewModel.Nearby.Count == 1);
        second.Found.Writer.TryWrite(Sensor("second", "B"));
        await Wait.Until(() => viewModel.Nearby.Count == 2);
        first.Found.Writer.Complete();
        second.Found.Writer.Complete();
        await scan;

        Assert.Equal(["A", "B"], viewModel.Nearby.Select(sensor => sensor.Id.Address).Order());
    }

    [Fact]
    public async Task Seeing_a_sensor_again_updates_it_instead_of_listing_it_twice()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);

        var scan = viewModel.ScanAsync(TestContext.Current.CancellationToken);
        provider.Found.Writer.TryWrite(Sensor("first", "A", -80));
        provider.Found.Writer.TryWrite(Sensor("first", "A", -50));
        await Wait.Until(() => viewModel.Nearby.Count == 1 && viewModel.Nearby[0].SignalDbm == -50);
        provider.Found.Writer.Complete();
        await scan;

        Assert.Single(viewModel.Nearby);
    }

    [Fact]
    public async Task Bluetooth_off_ends_the_scan_with_the_hint()
    {
        radio.IsOn = false;
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);

        await viewModel.ScanAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.BluetoothOff);
        Assert.Empty(viewModel.Nearby);
        Assert.Equal(0, provider.Scans);
    }

    [Fact]
    public async Task Bluetooth_turned_off_during_the_scan_stops_it()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);

        var scan = viewModel.ScanAsync(TestContext.Current.CancellationToken);
        await Wait.Until(() => provider.Scans == 1);
        radio.IsOn = false;
        await scan.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.True(viewModel.BluetoothOff);
    }

    [Fact]
    public async Task Changing_the_step_stops_the_scan()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);

        var scan = viewModel.ScanAsync(TestContext.Current.CancellationToken);
        await Wait.Until(() => provider.Scans == 1);
        viewModel.StepIndex = 1;
        await scan.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.True(scan.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Cancelling_stops_the_scan()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);
        using var cts = new CancellationTokenSource();

        var scan = viewModel.ScanAsync(cts.Token);
        await Wait.Until(() => provider.Scans == 1);
        await cts.CancelAsync();
        await scan.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.True(scan.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task A_failing_provider_is_logged_and_the_others_continue()
    {
        var healthy = new ControlledProvider("healthy");
        var viewModel = Create(new FailingProvider(), healthy);

        var scan = viewModel.ScanAsync(TestContext.Current.CancellationToken);
        healthy.Found.Writer.TryWrite(Sensor("healthy", "A"));
        await Wait.Until(() => viewModel.Nearby.Count == 1);
        healthy.Found.Writer.Complete();
        await scan;

        Assert.Single(logger.Entries, entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning);
    }

    [Fact]
    public void The_room_name_starts_as_the_default_name()
    {
        Assert.Equal("Onboarding_Room_DefaultName", Create().RoomName);
    }

    [Fact]
    public async Task The_checklist_has_the_provider_steps_then_the_pick_step()
    {
        var provider = new ControlledProvider("first") { OnboardingStepKeys = ["Step_A", "Step_B"] };
        var viewModel = Create(provider);

        Assert.Equal(
            [(1, "Step_A", false), (2, "Step_B", false), (3, "Onboarding_Step_Pick", false)],
            viewModel.Checklist.Select(step => (step.Number, step.Text, step.IsDone)));

        var scan = viewModel.ScanAsync(TestContext.Current.CancellationToken);
        provider.Found.Writer.TryWrite(Sensor("first", "A"));
        await Wait.Until(() => viewModel.Nearby.Count == 1);
        provider.Found.Writer.Complete();
        await scan;

        Assert.Equal([true, true, false], viewModel.Checklist.Select(step => step.IsDone));
    }

    [Theory]
    [InlineData(-45, "Footer_Signal_Good")]
    [InlineData(-60, "Footer_Signal_Good")]
    [InlineData(-70, "Footer_Signal_Fair")]
    [InlineData(-90, "Footer_Signal_Weak")]
    public async Task A_nearby_sensor_shows_its_name_and_signal(int dbm, string expected)
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);

        var scan = viewModel.ScanAsync(TestContext.Current.CancellationToken);
        provider.Found.Writer.TryWrite(new DiscoveredSensor(new DeviceId("first", "C3DEC8FCC7C6"), "Aranet4 0A0B3", dbm));
        await Wait.Until(() => viewModel.NearbySensors.Count == 1);
        provider.Found.Writer.Complete();
        await scan;

        var item = Assert.Single(viewModel.NearbySensors);
        Assert.Equal(("Aranet4 0A0B3", expected), (item.Name, item.Detail));
    }

    [Fact]
    public async Task A_sensor_without_a_name_is_listed_by_its_address()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);

        var scan = viewModel.ScanAsync(TestContext.Current.CancellationToken);
        provider.Found.Writer.TryWrite(new DiscoveredSensor(new DeviceId("first", "C3DEC8FCC7C6"), null, null));
        await Wait.Until(() => viewModel.NearbySensors.Count == 1);
        provider.Found.Writer.Complete();
        await scan;

        Assert.Equal(("C3DEC8FCC7C6", string.Empty), (viewModel.NearbySensors[0].Name, viewModel.NearbySensors[0].Detail));
    }

    [Fact]
    public async Task Pairing_saves_the_room_with_the_typed_name_and_selects_it()
    {
        var viewModel = Create();
        viewModel.RoomName = "Office";
        var item = new NearbySensorItem(new DeviceId("aranet4", "C3DEC8FCC7C6"), "Aranet4 0A0B3", string.Empty);

        await viewModel.PairCommand.ExecuteAsync(item);

        var room = Assert.Single(store.Snapshot());
        Assert.Equal(("Office", item.Id), (room.RoomName, room.Id));
        Assert.Equal(item.Id, registry.Selected);
        Assert.False(viewModel.PairFailed);
        Assert.False(viewModel.IsPairing);
    }

    [Fact]
    public async Task Pairing_with_a_blank_name_uses_the_default_name()
    {
        var viewModel = Create();
        viewModel.RoomName = "   ";
        var item = new NearbySensorItem(new DeviceId("aranet4", "C3DEC8FCC7C6"), "Aranet4 0A0B3", string.Empty);

        await viewModel.PairCommand.ExecuteAsync(item);

        Assert.Equal("Onboarding_Room_DefaultName", Assert.Single(store.Snapshot()).RoomName);
    }

    [Fact]
    public async Task Pairing_stops_the_scan()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);
        viewModel.Start(TestContext.Current.CancellationToken);
        await Wait.Until(() => provider.Scans == 1);

        await viewModel.PairCommand.ExecuteAsync(new NearbySensorItem(new DeviceId("first", "A"), "A", string.Empty));

        await Wait.Until(() => !viewModel.IsScanning);
        Assert.False(viewModel.NoSensorFound);
    }

    [Fact]
    public async Task A_failure_while_pairing_is_logged_and_shown()
    {
        var failingStore = new ThrowingStore();
        var failingRegistry = new DeviceRegistry(failingStore);
        var failingEnrollment = new DeviceEnrollment(failingStore, failingRegistry, new SettingsService(new FakeSettingsStore(), messenger), messenger);
        var viewModel = new OnboardingViewModel([], failingEnrollment, radio, new FakeLocalizer(), clock, logger);

        await viewModel.PairCommand.ExecuteAsync(new NearbySensorItem(new DeviceId("aranet4", "A"), "A", string.Empty));

        Assert.True(viewModel.PairFailed);
        Assert.False(viewModel.IsPairing);
        Assert.Contains(logger.Entries, entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Error);
    }

    [Fact]
    public async Task Finding_nothing_for_fifteen_seconds_says_so()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);
        viewModel.Start(TestContext.Current.CancellationToken);
        await Wait.Until(() => provider.Scans == 1);
        Assert.True(viewModel.IsScanning);

        clock.Advance(OnboardingViewModel.NoSensorAfter);

        await Wait.Until(() => viewModel.NoSensorFound);
        Assert.True(viewModel.IsScanning);
    }

    [Fact]
    public async Task A_sensor_found_in_time_means_no_such_message()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);
        viewModel.Start(TestContext.Current.CancellationToken);
        await Wait.Until(() => provider.Scans == 1);
        provider.Found.Writer.TryWrite(Sensor("first", "A"));
        await Wait.Until(() => viewModel.Nearby.Count == 1);

        clock.Advance(OnboardingViewModel.NoSensorAfter);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.False(viewModel.NoSensorFound);
    }

    [Fact]
    public async Task A_scan_that_ends_without_a_sensor_says_so()
    {
        var provider = new ControlledProvider("first");
        provider.Found.Writer.Complete();
        var viewModel = Create(provider);

        viewModel.Start(TestContext.Current.CancellationToken);

        await Wait.Until(() => viewModel.NoSensorFound);
        Assert.False(viewModel.IsScanning);
    }

    [Fact]
    public async Task Bluetooth_off_says_so_and_shows_no_sensor()
    {
        radio.IsOn = false;
        var viewModel = Create(new ControlledProvider("first"));

        viewModel.Start(TestContext.Current.CancellationToken);

        await Wait.Until(() => viewModel.NoSensorFound);
        Assert.True(viewModel.BluetoothOff);
    }

    [Fact]
    public async Task Retry_starts_a_new_scan()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);
        viewModel.Start(TestContext.Current.CancellationToken);
        await Wait.Until(() => provider.Scans == 1);
        clock.Advance(OnboardingViewModel.NoSensorAfter);
        await Wait.Until(() => viewModel.NoSensorFound);
        var before = provider.Scans;

        viewModel.RetryCommand.Execute(null);

        await Wait.Until(() => provider.Scans > before && !viewModel.NoSensorFound);
    }

    [Fact]
    public void Only_one_hint_shows_at_a_time()
    {
        var viewModel = Create();

        viewModel.IsScanning = true;
        Assert.Equal((true, false, false), (viewModel.ShowSearching, viewModel.ShowNoSensor, viewModel.ShowBluetoothOff));

        viewModel.NoSensorFound = true;
        Assert.Equal((false, true, false), (viewModel.ShowSearching, viewModel.ShowNoSensor, viewModel.ShowBluetoothOff));

        viewModel.BluetoothOff = true;
        Assert.Equal((false, false, true), (viewModel.ShowSearching, viewModel.ShowNoSensor, viewModel.ShowBluetoothOff));
    }

    [Fact]
    public void No_hint_shows_once_a_sensor_is_listed()
    {
        var viewModel = Create();
        viewModel.IsScanning = true;
        viewModel.NoSensorFound = true;
        viewModel.BluetoothOff = true;

        viewModel.Nearby = [Sensor("first", "A")];

        Assert.Equal((false, false, false), (viewModel.ShowSearching, viewModel.ShowNoSensor, viewModel.ShowBluetoothOff));
    }

    [Fact]
    public async Task A_scan_that_finds_nothing_is_restarted_by_itself()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);
        viewModel.Start(TestContext.Current.CancellationToken);
        await Wait.Until(() => provider.Scans == 1);

        clock.Advance(OnboardingViewModel.RescanAfter);

        await Wait.Until(() => provider.Scans == 2);
        Assert.True(viewModel.IsScanning);
        Assert.Contains(logger.Entries, entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Information && entry.Message.Contains("scanning again", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_scan_keeps_being_restarted_while_nothing_is_found()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);
        viewModel.Start(TestContext.Current.CancellationToken);

        for (var scan = 1; scan <= 3; scan++)
        {
            await Wait.Until(() => provider.Scans == scan);
            await Wait.Until(() => clock.PendingTimers >= 1);
            clock.Advance(OnboardingViewModel.RescanAfter);
        }

        await Wait.Until(() => provider.Scans == 4);
    }

    [Fact]
    public async Task A_scan_that_found_a_sensor_is_left_running()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);
        viewModel.Start(TestContext.Current.CancellationToken);
        await Wait.Until(() => provider.Scans == 1);
        provider.Found.Writer.TryWrite(Sensor("first", "A"));
        await Wait.Until(() => viewModel.Nearby.Count == 1);

        clock.Advance(OnboardingViewModel.RescanAfter);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.Equal(1, provider.Scans);
        Assert.True(viewModel.IsScanning);
    }

    [Fact]
    public async Task Bluetooth_coming_on_after_the_start_starts_the_scan()
    {
        radio.IsOn = false;
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);
        viewModel.Start(TestContext.Current.CancellationToken);
        await Wait.Until(() => viewModel.BluetoothOff && !viewModel.IsScanning);
        Assert.Equal(0, provider.Scans);

        radio.IsOn = true;

        await Wait.Until(() => provider.Scans == 1);
        Assert.False(viewModel.BluetoothOff);
    }

    [Fact]
    public async Task Adding_a_sensor_opens_the_search_and_starts_scanning()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);
        Assert.False(viewModel.IsOpen);

        viewModel.OpenToAddSensor();

        Assert.True(viewModel.IsOpen);
        await Wait.Until(() => provider.Scans == 1 && viewModel.IsScanning);
    }

    [Fact]
    public async Task Cancel_closes_the_search_and_stops_scanning()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);
        viewModel.OpenToAddSensor();
        await Wait.Until(() => provider.Scans == 1);

        viewModel.CancelCommand.Execute(null);

        Assert.False(viewModel.IsOpen);
        await Wait.Until(() => !viewModel.IsScanning);
        Assert.False(viewModel.NoSensorFound);
    }

    [Fact]
    public async Task After_cancelling_Bluetooth_coming_on_does_not_start_a_scan()
    {
        radio.IsOn = false;
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);
        viewModel.OpenToAddSensor();
        await Wait.Until(() => viewModel.BluetoothOff && !viewModel.IsScanning);
        viewModel.CancelCommand.Execute(null);

        radio.IsOn = true;
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.Equal(0, provider.Scans);
    }

    [Fact]
    public async Task Pairing_a_new_sensor_closes_the_search()
    {
        var viewModel = Create();
        viewModel.OpenToAddSensor();

        await viewModel.PairCommand.ExecuteAsync(new NearbySensorItem(new DeviceId("aranet4", "C3DEC8FCC7C6"), "Aranet4 0A0B3", string.Empty));

        Assert.False(viewModel.IsOpen);
        Assert.Equal(new DeviceId("aranet4", "C3DEC8FCC7C6"), registry.Selected);
    }

    private sealed class ThrowingStore : IDeviceStore
    {
        public Task<IReadOnlyList<DeviceInfo>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<DeviceInfo>>([]);

        public Task SaveAsync(DeviceInfo device, CancellationToken cancellationToken)
            => throw new IOException("disk full");

        public Task DeleteAsync(DeviceId id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class ProviderBase : ISensorProvider
    {
        public ProviderBase(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public string DisplayNameKey => "Sensor_Simulated_Name";

        public IReadOnlyList<string> OnboardingStepKeys { get; init; } = [];

        public IReadOnlyList<IReadingImporter> Importers { get; } = [];

        public virtual IAsyncEnumerable<DiscoveredSensor> ScanAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ISensor Create(DeviceId id) => throw new NotSupportedException();
    }

    private sealed class ControlledProvider : ProviderBase
    {
        private int scans;

        public ControlledProvider(string id)
            : base(id)
        {
        }

        public Channel<DiscoveredSensor> Found { get; } = Channel.CreateUnbounded<DiscoveredSensor>();

        public int Scans => Volatile.Read(ref scans);

        public override async IAsyncEnumerable<DiscoveredSensor> ScanAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref scans);

            await foreach (var sensor in Found.Reader.ReadAllAsync(cancellationToken))
            {
                yield return sensor;
            }
        }
    }

    private sealed class FailingProvider : ProviderBase
    {
        public FailingProvider()
            : base("failing")
        {
        }

        public override async IAsyncEnumerable<DiscoveredSensor> ScanAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new InvalidOperationException("adapter");

#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }
    }
}
