using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Readings;
using Deskair.Services.Alerts;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Services;

public sealed class AlertFeedTests : IDisposable
{
    private static readonly DeviceId office = new("aranet4", "AA:01");
    private static readonly DateTimeOffset start = Samples.Utc(2026, 10, 9, 12);

    private sealed class RecordingNotifier : INotifier
    {
        private readonly ThreadSafeLog<AlertNotification> sent = new();

        public ThreadSafeLog<AlertNotification> Sent => sent;

        public Task NotifyAsync(AlertNotification notification, CancellationToken cancellationToken)
        {
            sent.Add(notification);

            return Task.CompletedTask;
        }
    }

    private sealed class NoDoNotDisturb : IDoNotDisturb
    {
        public bool IsActive => false;
    }

    private sealed class DefaultSettingsStore : ISettingsStore
    {
        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(AppSettings.Default);

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private readonly StrongReferenceMessenger messenger = new();
    private readonly RecordingNotifier notifier = new();
    private readonly FakeClock clock = new(start);
    private readonly FakeDeviceStore deviceStore = new();
    private readonly CancellationTokenSource stop = new();
    private readonly AlertEngine engine;
    private readonly DeviceRegistry registry;
    private readonly ListLogger<AlertFeed> logger = new();

    public AlertFeedTests()
    {
        engine = new AlertEngine(notifier, new NoDoNotDisturb(), new SettingsService(new DefaultSettingsStore(), messenger), clock);
        registry = new DeviceRegistry(deviceStore);
        deviceStore.Devices.Add(new DeviceInfo(office, "Office", null, [], null));
    }

    public void Dispose()
    {
        stop.Cancel();
        engine.Dispose();
        stop.Dispose();
    }

    private async Task<AlertFeed> StartAsync()
    {
        await registry.LoadAsync(TestContext.Current.CancellationToken);
        var feed = new AlertFeed(messenger, registry, engine, logger);
        feed.Start(stop.Token);

        return feed;
    }

    private void Reading(double ppm, DeviceId? device = null)
        => messenger.Send(new ReadingReceived(device ?? office, Samples.Co2(clock.GetUtcNow(), ppm)));

    [Fact]
    public async Task A_reading_that_stays_high_for_the_dwell_time_becomes_an_alert_with_the_room_name()
    {
        await StartAsync();

        Reading(1100);
        clock.Advance(TimeSpan.FromMinutes(6));
        Reading(1100);

        await Wait.Until(() => notifier.Sent.Count == 1);
        Assert.Equal("Office", notifier.Sent[0].RoomName);
        Assert.Equal(office, notifier.Sent[0].Device);
    }

    [Fact]
    public async Task Good_air_sends_nothing()
    {
        await StartAsync();

        Reading(600);
        clock.Advance(TimeSpan.FromMinutes(30));
        Reading(600);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public async Task A_device_that_is_not_known_is_named_by_its_address()
    {
        await StartAsync();
        var stranger = new DeviceId("aranet4", "FF:99");

        Reading(1100, stranger);
        clock.Advance(TimeSpan.FromMinutes(6));
        Reading(1100, stranger);

        await Wait.Until(() => notifier.Sent.Count == 1);
        Assert.Equal("FF:99", notifier.Sent[0].RoomName);
    }

    [Fact]
    public async Task A_notifier_that_fails_is_logged_and_later_readings_still_count()
    {
        var failing = new FailingOnceNotifier();
        using var failingEngine = new AlertEngine(failing, new NoDoNotDisturb(), new SettingsService(new DefaultSettingsStore(), messenger), clock);
        await registry.LoadAsync(TestContext.Current.CancellationToken);
        new AlertFeed(messenger, registry, failingEngine, logger).Start(stop.Token);

        Reading(1100);
        clock.Advance(TimeSpan.FromMinutes(6));
        Reading(1100);
        await Wait.Until(() => logger.Entries.Any(entry => entry.Exception is InvalidOperationException));
        clock.Advance(TimeSpan.FromMinutes(1));
        Reading(1100);

        await Wait.Until(() => failing.Calls == 2);
    }

    [Fact]
    public async Task After_the_feed_is_stopped_readings_are_ignored()
    {
        await StartAsync();
        await stop.CancelAsync();

        Reading(1100);
        clock.Advance(TimeSpan.FromMinutes(6));
        Reading(1100);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.Empty(notifier.Sent);
    }

    private sealed class FailingOnceNotifier : INotifier
    {
        private int calls;

        public int Calls => Volatile.Read(ref calls);

        public Task NotifyAsync(AlertNotification notification, CancellationToken cancellationToken)
        {
            return Interlocked.Increment(ref calls) == 1 ? throw new InvalidOperationException("toast failed") : Task.CompletedTask;
        }
    }
}
