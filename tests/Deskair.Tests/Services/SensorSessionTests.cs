using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Readings;
using Deskair.Services.Live;
using Deskair.Services.Messages;
using Deskair.Services.Sensors;
using Deskair.Services.Sync;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging;

namespace Deskair.Tests.Services;

public sealed class SensorSessionTests : IAsyncDisposable
{
    private static readonly DateTimeOffset start = Samples.Utc(2026, 10, 12, 12);
    private static readonly DeviceId device = new("test", "A1");

    private readonly FakeClock clock = new(start);
    private readonly FakeReadingStore store = new();
    private readonly FakeDeviceStore deviceStore = new();
    private readonly StrongReferenceMessenger messenger = new();
    private readonly ListLogger<SensorSession> logger = new();
    private readonly FakeSensor sensor = new();
    private readonly TestProvider provider;
    private readonly SensorSession session;
    private readonly List<object> messages = [];

    public SensorSessionTests()
    {
        provider = new TestProvider(sensor);
        var feed = new LiveFeed(store, messenger, clock, new ListLogger<LiveFeed>());
        var syncService = new SyncService(store, deviceStore, clock, new ListLogger<SyncService>());

        session = new SensorSession([provider], feed, syncService, messenger, clock, logger);
        messenger.Register<ReadingReceived>(this, (_, message) => messages.Add(message));
        messenger.Register<SensorStatusUpdated>(this, (_, message) => messages.Add(message));
        messenger.Register<SyncCompleted>(this, (_, message) => messages.Add(message));
    }

    public async ValueTask DisposeAsync()
    {
        await session.DisposeAsync();
        messenger.UnregisterAll(this);
    }

    [Fact]
    public async Task Starting_connects_syncs_and_then_polls_live_readings()
    {
        await session.StartAsync(device);

        await Wait.Until(() => messages.OfType<ReadingReceived>().Any());

        Assert.IsType<SensorStatusUpdated>(messages[0]);
        Assert.IsType<SyncCompleted>(messages[1]);
        Assert.IsType<ReadingReceived>(messages[2]);
        Assert.Equal(device, ((ReadingReceived)messages[2]).Device);
    }

    [Fact]
    public async Task The_status_carries_battery_and_signal()
    {
        await session.StartAsync(device);

        await Wait.Until(() => messages.OfType<SensorStatusUpdated>().Any());

        var status = messages.OfType<SensorStatusUpdated>().First().Status;
        Assert.Equal(80, status.BatteryPercent);
        Assert.Equal(-60, status.SignalDbm);
    }

    [Fact]
    public async Task Readings_keep_coming_at_the_sensor_interval()
    {
        await session.StartAsync(device);
        await Wait.Until(() => messages.OfType<ReadingReceived>().Count() == 1);

        clock.Advance(TimeSpan.FromMinutes(2));

        await Wait.Until(() => messages.OfType<ReadingReceived>().Count() == 2);
    }

    [Fact]
    public async Task Stopping_ends_the_polling()
    {
        await session.StartAsync(device);
        await Wait.Until(() => messages.OfType<ReadingReceived>().Any());

        await session.StopAsync();
        var count = messages.OfType<ReadingReceived>().Count();
        clock.Advance(TimeSpan.FromMinutes(10));
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.Equal(count, messages.OfType<ReadingReceived>().Count());
    }

    [Fact]
    public async Task Starting_another_device_stops_the_first()
    {
        await session.StartAsync(device);
        await Wait.Until(() => messages.OfType<ReadingReceived>().Any());

        await session.StartAsync(new DeviceId("test", "B2"));
        await Wait.Until(() => messages.OfType<ReadingReceived>().Any(message => message.Device.Address == "B2"));

        Assert.Equal(2, provider.Created.Count);
        Assert.Equal(["A1", "B2"], provider.Created.Select(id => id.Address));
    }

    [Fact]
    public async Task An_unreachable_sensor_is_logged_and_tried_again_after_30_seconds()
    {
        provider.FailCreates = 1;

        await session.StartAsync(device);
        await Wait.Until(() => logger.Entries.Any(entry => entry.Level == LogLevel.Warning));
        Assert.DoesNotContain(messages, message => message is ReadingReceived);

        clock.Advance(TimeSpan.FromSeconds(30));

        await Wait.Until(() => messages.OfType<ReadingReceived>().Any());
    }

    [Fact]
    public async Task An_unknown_provider_is_an_error_and_starts_nothing()
    {
        await session.StartAsync(new DeviceId("nobody", "X"));

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
        Assert.Empty(provider.Created);
    }

    [Fact]
    public async Task Stopping_without_starting_does_nothing()
    {
        await session.StopAsync();

        Assert.Empty(messages);
    }

    [Fact]
    public async Task Disposing_synchronously_stops_the_polling()
    {
        await session.StartAsync(device);
        await Wait.Until(() => messages.OfType<ReadingReceived>().Any());

        session.Dispose();
        var count = messages.OfType<ReadingReceived>().Count();
        clock.Advance(TimeSpan.FromMinutes(10));
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.Equal(count, messages.OfType<ReadingReceived>().Count());
    }

    private sealed class TestProvider : ISensorProvider
    {
        private readonly FakeSensor sensor;

        public TestProvider(FakeSensor sensor)
        {
            this.sensor = sensor;
        }

        public List<DeviceId> Created { get; } = [];

        public int FailCreates { get; set; }

        public string Id => "test";

        public string DisplayNameKey => "Sensor_Simulated_Name";

        public IReadOnlyList<string> OnboardingStepKeys { get; } = [];

        public IReadOnlyList<IReadingImporter> Importers { get; } = [];

        public async IAsyncEnumerable<DiscoveredSensor> ScanAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ISensor Create(DeviceId id)
        {
            if (FailCreates > 0)
            {
                FailCreates--;
                throw new InvalidOperationException("adapter missing");
            }

            Created.Add(id);

            return sensor;
        }
    }
}
