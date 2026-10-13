using System.IO;
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
    private readonly ThreadSafeLog<object> messages = new();
    private readonly ThreadSafeLog<object> syncMessages = new();

    public SensorSessionTests()
    {
        provider = new TestProvider(sensor);
        var feed = new LiveFeed(store, messenger, clock, new ListLogger<LiveFeed>());
        var syncService = new SyncService(store, deviceStore, clock, new ListLogger<SyncService>());

        session = new SensorSession([provider], feed, syncService, messenger, clock, logger);
        messenger.Register<ReadingReceived>(this, (_, message) => messages.Add(message));
        messenger.Register<SensorStatusUpdated>(this, (_, message) => messages.Add(message));
        messenger.Register<SyncStarted>(this, (_, message) => syncMessages.Add(message));
        messenger.Register<SyncProgressed>(this, (_, message) => syncMessages.Add(message));
        messenger.Register<SyncCompleted>(this, (_, message) =>
        {
            messages.Add(message);
            syncMessages.Add(message);
        });
    }

    public async ValueTask DisposeAsync()
    {
        await session.DisposeAsync();
        messenger.UnregisterAll(this);
    }

    [Fact]
    public async Task Starting_connects_shows_the_current_reading_syncs_and_then_keeps_polling()
    {
        await session.StartAsync(device);

        await Wait.Until(() => messages.OfType<SyncCompleted>().Any());

        // The current value comes first, so the screen is not empty while the history downloads.
        Assert.IsType<SensorStatusUpdated>(messages[0]);
        Assert.IsType<ReadingReceived>(messages[1]);
        Assert.IsType<SyncCompleted>(messages[2]);
        Assert.Equal(device, ((ReadingReceived)messages[1]).Device);
    }

    [Fact]
    public async Task The_current_reading_arrives_before_the_history_download_finishes()
    {
        sensor.History.Add(Samples.Co2(start.AddMinutes(-30), 700));
        sensor.PauseHistoryAfter = 0;

        await session.StartAsync(device);
        await sensor.HistoryPaused.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Single(messages.OfType<ReadingReceived>());
        Assert.DoesNotContain(messages, message => message is SyncCompleted);
    }

    [Fact]
    public async Task The_early_reading_is_not_stored_so_the_history_sync_still_asks_for_the_whole_week()
    {
        sensor.History.Add(Samples.Co2(start.AddDays(-3), 700));

        await session.StartAsync(device);
        await Wait.Until(() => messages.OfType<SyncCompleted>().Any());

        // Nothing is stored yet, so the sync starts from the sensor memory (7 days), not from the reading just shown.
        Assert.True(sensor.HistorySince < start.AddDays(-5));
        Assert.Single(store.Merges);
        Assert.Equal(700, store.Readings.Single().Co2Ppm);
    }

    [Fact]
    public async Task After_the_sync_the_next_poll_waits_one_interval()
    {
        await session.StartAsync(device);
        await Wait.Until(() => messages.OfType<SyncCompleted>().Any());
        await Wait.Until(() => clock.PendingTimers >= 1);
        await Wait.Quiet();

        Assert.Single(messages.OfType<ReadingReceived>());

        clock.Advance(TimeSpan.FromMinutes(2));

        await Wait.Until(() => messages.OfType<ReadingReceived>().Count() == 2);
    }

    [Fact]
    public async Task The_sync_is_announced_with_its_progress_and_always_ends()
    {
        sensor.History.AddRange([Samples.Co2(start.AddMinutes(-6), 700), Samples.Co2(start.AddMinutes(-4), 710)]);

        await session.StartAsync(device);
        await Wait.Until(() => syncMessages.OfType<SyncCompleted>().Any());

        Assert.IsType<SyncStarted>(syncMessages[0]);
        Assert.Equal([new HistoryProgress(1, 2), new HistoryProgress(2, 2)], syncMessages.OfType<SyncProgressed>().Select(message => message.Progress));
        Assert.IsType<SyncCompleted>(syncMessages[^1]);
    }

    [Fact]
    public async Task A_sync_that_fails_still_ends_so_the_note_goes_away()
    {
        sensor.History.AddRange([Samples.Co2(start.AddMinutes(-6), 700), Samples.Co2(start.AddMinutes(-4), 710)]);
        sensor.FailHistoryAfter = 1;

        await session.StartAsync(device);

        await Wait.Until(() => syncMessages.OfType<SyncCompleted>().Any());
        Assert.IsType<SyncStarted>(syncMessages[0]);
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
        await Wait.Until(() => clock.PendingTimers >= 1);

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
        await Wait.Quiet();

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
        await Wait.Until(() => clock.PendingTimers >= 1);

        clock.Advance(TimeSpan.FromSeconds(30));

        await Wait.Until(() => messages.OfType<ReadingReceived>().Any());
    }

    [Fact]
    public async Task A_connection_lost_while_polling_makes_the_session_connect_again_after_30_seconds()
    {
        sensor.OnReadCurrent = call => call == 1
            ? throw new IOException("Connection lost.")
            : Samples.Co2(start.AddMinutes(call), 800);
        await session.StartAsync(device);
        await Wait.Until(() => messages.OfType<ReadingReceived>().Count() == 1);
        await Wait.Until(() => clock.PendingTimers >= 1);

        clock.Advance(TimeSpan.FromMinutes(2));
        await Wait.Until(() => logger.Entries.Any(entry => entry.Level == LogLevel.Warning));
        await Wait.Until(() => clock.PendingTimers >= 1);
        clock.Advance(TimeSpan.FromSeconds(30));

        await Wait.Until(() => provider.Created.Count == 2);
    }

    [Fact]
    public async Task A_failed_attempt_is_announced_so_the_screen_can_say_so()
    {
        var failures = new ThreadSafeLog<SensorConnectionFailed>();
        messenger.Register<SensorConnectionFailed>(this, (_, message) => failures.Add(message));
        provider.FailCreates = 1;

        await session.StartAsync(device);

        await Wait.Until(() => failures.Count == 1);
        Assert.Equal(device, failures[0].Device);
    }

    [Fact]
    public async Task A_connection_that_never_answers_is_given_up_after_the_timeout_and_announced()
    {
        var failures = new ThreadSafeLog<SensorConnectionFailed>();
        messenger.Register<SensorConnectionFailed>(this, (_, message) => failures.Add(message));
        sensor.HangConnect = true;
        await session.StartAsync(device);
        await Wait.Until(() => clock.PendingTimers >= 1);

        clock.Advance(SensorSession.ConnectTimeout);

        await Wait.Until(() => failures.Count == 1);
    }

    [Fact]
    public async Task A_stopped_session_does_not_announce_a_failure()
    {
        var failures = new ThreadSafeLog<SensorConnectionFailed>();
        messenger.Register<SensorConnectionFailed>(this, (_, message) => failures.Add(message));
        sensor.HangConnect = true;
        await session.StartAsync(device);
        await Wait.Until(() => clock.PendingTimers >= 1);

        await session.StopAsync();
        await Wait.Quiet();

        Assert.Empty(failures);
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
        await Wait.Quiet();

        Assert.Equal(count, messages.OfType<ReadingReceived>().Count());
    }

    [Fact(Timeout = 20000)]
    public async Task The_session_never_depends_on_the_thread_that_started_it()
    {
        // Like a UI thread that is busy or shutting down: posted work never runs.
        var starter = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new NeverRunsContext());
            session.StartAsync(device).Wait();
        });
        starter.Start();
        starter.Join();

        await Wait.Until(() => messages.OfType<ReadingReceived>().Any());
        var stop = session.StopAsync();

        await stop.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(stop.IsCompletedSuccessfully);
    }

    private sealed class NeverRunsContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
        }
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
