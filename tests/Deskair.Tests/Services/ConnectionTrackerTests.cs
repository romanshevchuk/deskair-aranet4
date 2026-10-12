using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Readings;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Services;

public sealed class ConnectionTrackerTests : IDisposable
{
    private static readonly DateTimeOffset start = Samples.Utc(2026, 10, 12, 12);
    private static readonly DeviceId device = new("test", "A1");
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(2);

    private readonly FakeClock clock = new(start);
    private readonly FakeBluetoothRadio radio = new();
    private readonly StrongReferenceMessenger messenger = new();
    private readonly FakeDeviceStore deviceStore = new();
    private readonly DeviceRegistry registry;
    private readonly ConnectionTracker tracker;
    private readonly List<ConnectionState> changes = [];
    private readonly CancellationTokenSource cts = new();

    public ConnectionTrackerTests()
    {
        registry = new DeviceRegistry(deviceStore);
        tracker = new ConnectionTracker(registry, radio, messenger, clock);
        messenger.Register<ConnectionStateChanged>(this, (_, message) => changes.Add(message.State));
    }

    public void Dispose()
    {
        cts.Dispose();
        messenger.UnregisterAll(this);
    }

    private async Task SelectDeviceAsync()
    {
        deviceStore.Devices.Add(new DeviceInfo(device, "Office", null, [new IntervalChange(start.AddYears(-1), interval)], null));
        await registry.LoadAsync(TestContext.Current.CancellationToken);
    }

    private void Reading(DateTimeOffset at, int? battery = 80)
    {
        messenger.Send(new SensorStatusUpdated(device, new SensorStatus(interval, battery, -60)));
        messenger.Send(new ReadingReceived(device, Samples.Co2(at, 700)));
    }

    [Fact]
    public async Task Without_a_device_the_state_is_first_run()
    {
        await tracker.StartAsync(null, cts.Token);

        Assert.Equal(ConnectionState.FirstRun, tracker.Current);
    }

    [Fact]
    public async Task A_fresh_reading_is_normal()
    {
        await SelectDeviceAsync();
        await tracker.StartAsync(start.AddMinutes(-1), cts.Token);

        Assert.Equal(ConnectionState.Normal, tracker.Current);
        Assert.Equal([ConnectionState.Normal], changes);
    }

    [Fact]
    public async Task Silence_makes_it_stale_then_not_connected()
    {
        await SelectDeviceAsync();
        await tracker.StartAsync(start, cts.Token);

        clock.Advance(TimeSpan.FromMinutes(7));
        tracker.Evaluate();
        Assert.Equal(ConnectionState.Stale, tracker.Current);

        clock.Advance(TimeSpan.FromMinutes(4));
        tracker.Evaluate();
        Assert.Equal(ConnectionState.NotConnected, tracker.Current);
    }

    [Fact]
    public async Task A_new_reading_brings_it_back_to_normal()
    {
        await SelectDeviceAsync();
        await tracker.StartAsync(start, cts.Token);
        clock.Advance(TimeSpan.FromMinutes(11));
        tracker.Evaluate();

        Reading(clock.GetUtcNow());

        Assert.Equal(ConnectionState.Normal, tracker.Current);
        Assert.Equal([ConnectionState.Normal, ConnectionState.NotConnected, ConnectionState.Normal], changes);
    }

    [Fact]
    public async Task A_low_battery_is_reported_while_readings_arrive()
    {
        await SelectDeviceAsync();
        await tracker.StartAsync(start, cts.Token);

        Reading(clock.GetUtcNow(), battery: 8);

        Assert.Equal(ConnectionState.LowBattery, tracker.Current);
    }

    [Fact]
    public async Task Bluetooth_off_means_not_connected()
    {
        await SelectDeviceAsync();
        await tracker.StartAsync(start, cts.Token);

        radio.IsOn = false;
        tracker.Evaluate();

        Assert.Equal(ConnectionState.NotConnected, tracker.Current);
    }

    [Fact]
    public async Task The_state_is_only_announced_when_it_changes()
    {
        await SelectDeviceAsync();
        await tracker.StartAsync(start, cts.Token);

        tracker.Evaluate();
        tracker.Evaluate();

        Assert.Single(changes);
    }

    [Fact]
    public async Task Another_device_starts_without_the_old_readings()
    {
        await SelectDeviceAsync();
        await tracker.StartAsync(start, cts.Token);

        messenger.Send(new DeviceSelected(new DeviceId("test", "B2")));

        Assert.Equal(ConnectionState.NotConnected, tracker.Current);
    }

    [Fact]
    public async Task Cancelling_stops_listening()
    {
        await SelectDeviceAsync();
        await tracker.StartAsync(start, cts.Token);

        await cts.CancelAsync();
        var before = changes.Count;
        clock.Advance(TimeSpan.FromMinutes(20));
        Reading(clock.GetUtcNow(), battery: 5);

        Assert.Equal(before, changes.Count);
    }

    [Fact]
    public async Task The_background_check_notices_silence_on_its_own()
    {
        await SelectDeviceAsync();
        var run = tracker.RunAsync(start, cts.Token);

        clock.Advance(TimeSpan.FromMinutes(7));
        await Wait.Until(() => tracker.Current == ConnectionState.Stale);

        await cts.CancelAsync();
        await run;
    }

    [Fact]
    public async Task Turning_bluetooth_off_is_noticed_without_waiting_for_the_next_check()
    {
        await SelectDeviceAsync();
        await tracker.StartAsync(start, cts.Token);

        radio.IsOn = false;

        Assert.Equal(ConnectionState.NotConnected, tracker.Current);

        radio.IsOn = true;

        Assert.Equal(ConnectionState.Normal, tracker.Current);
    }

    [Fact]
    public async Task After_cancelling_the_radio_is_no_longer_followed()
    {
        await SelectDeviceAsync();
        await tracker.StartAsync(start, cts.Token);

        await cts.CancelAsync();
        radio.IsOn = false;

        Assert.Equal(ConnectionState.Normal, tracker.Current);
    }
}
