using System.Collections.Concurrent;
using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Readings;
using Deskair.Services.Live;
using Deskair.Services.Messages;
using Deskair.Services.Sensors;
using Deskair.Services.Storage;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging;

namespace Deskair.Tests.Services;

public sealed class LiveFeedTests : IDisposable
{
    private static readonly DeviceId device = new("aranet4", "AA:01");
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(2);

    private readonly FakeReadingStore store = new();
    private readonly StrongReferenceMessenger messenger = new();
    private readonly FakeClock clock = new(Samples.Utc(2026, 10, 9, 12));
    private readonly ListLogger<LiveFeed> logger = new();
    private readonly FakeSensor sensor = new();
    private readonly ConcurrentQueue<ReadingReceived> received = new();
    private readonly CancellationTokenSource stop = new();
    private readonly LiveFeed feed;

    public LiveFeedTests()
    {
        feed = new LiveFeed(store, messenger, clock, logger);
        messenger.Register<ReadingReceived>(this, (_, message) => received.Enqueue(message));
    }

    public void Dispose()
    {
        stop.Cancel();
        stop.Dispose();
    }

    private Task Start() => feed.RunAsync(sensor, device, interval, stop.Token);

    [Fact]
    public async Task Run_polls_straight_away_stores_the_reading_as_a_device_reading_and_announces_it()
    {
        var running = Start();

        await Wait.Until(() => !received.IsEmpty);

        var message = Assert.Single(received);
        Assert.Equal(device, message.Device);
        Assert.Equal(800, message.Reading.Co2Ppm);
        Assert.Equal(ReadingSource.Device, Assert.Single(store.Merges).Source);
        Assert.Single(store.Readings);

        await stop.CancelAsync();
        await running;
    }

    [Fact]
    public async Task Poll_once_announces_one_reading_without_storing_it_or_starting_a_loop()
    {
        await feed.PollOnceAsync(sensor, device, interval, TestContext.Current.CancellationToken);

        Assert.Single(received);
        Assert.Empty(store.Merges);
        Assert.Equal(0, clock.PendingTimers);
    }

    [Fact]
    public async Task Poll_once_logs_a_failed_read_and_does_not_throw()
    {
        sensor.OnReadCurrent = _ => throw new InvalidOperationException("Bluetooth dropped.");

        await feed.PollOnceAsync(sensor, device, interval, TestContext.Current.CancellationToken);

        Assert.Empty(received);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Run_can_wait_one_interval_before_the_first_poll()
    {
        var running = feed.RunAsync(sensor, device, interval, stop.Token, pollFirst: false);
        await Wait.Until(() => clock.PendingTimers == 1);
        await Wait.Quiet();
        Assert.Empty(received);

        clock.Advance(interval);

        await Wait.Until(() => received.Count == 1);
        await stop.CancelAsync();
        await running;
    }

    [Fact]
    public async Task Run_waits_one_interval_between_polls()
    {
        var running = Start();
        await Wait.Until(() => received.Count == 1);
        await Wait.Until(() => clock.PendingTimers == 1);

        clock.Advance(interval);
        await Wait.Quiet();
        Assert.Equal(1, sensor.ReadCurrentCalls);

        clock.Advance(LiveFeed.PollMargin);
        await Wait.Until(() => received.Count == 2);

        await stop.CancelAsync();
        await running;
    }

    [Fact]
    public async Task Run_logs_a_failed_poll_and_tries_again_after_the_next_interval()
    {
        sensor.OnReadCurrent = call => call == 0
            ? throw new InvalidOperationException("Bluetooth dropped.")
            : Samples.Co2(Samples.Utc(2026, 10, 9, 12, 2), 900);
        var running = Start();

        await Wait.Until(() => sensor.ReadCurrentCalls == 1);
        await Wait.Until(() => logger.Entries.Count == 1);
        Assert.Empty(received);
        await Wait.Until(() => clock.PendingTimers >= 1);

        clock.Advance(interval);
        await Wait.Until(() => received.Count == 1);

        Assert.Equal(LogLevel.Warning, logger.Entries[0].Level);
        Assert.IsType<InvalidOperationException>(logger.Entries[0].Exception);
        Assert.Equal(900, received.Single().Reading.Co2Ppm);

        await stop.CancelAsync();
        await running;
    }

    [Theory]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(TimeoutException))]
    public async Task Run_gives_up_when_the_connection_itself_failed_so_the_session_can_reconnect(Type failure)
    {
        sensor.OnReadCurrent = _ => throw (Exception)Activator.CreateInstance(failure, "Connection lost.")!;

        await Assert.ThrowsAsync(failure, Start);

        Assert.Empty(received);
        Assert.Equal(LogLevel.Warning, logger.Entries.Single().Level);
    }

    [Fact]
    public async Task Run_gives_up_when_a_later_poll_finds_the_connection_gone()
    {
        sensor.OnReadCurrent = call => call == 0
            ? Samples.Co2(Samples.Utc(2026, 10, 9, 12), 800)
            : throw new IOException("Connection lost.");
        var running = Start();
        await Wait.Until(() => received.Count == 1);
        await Wait.Until(() => clock.PendingTimers >= 1);

        clock.Advance(interval + LiveFeed.PollMargin);

        await Assert.ThrowsAsync<IOException>(() => running);
        Assert.Equal(800, received.Single().Reading.Co2Ppm);
    }

    [Fact]
    public async Task Run_gives_up_on_a_read_that_never_answers_so_the_session_can_reconnect()
    {
        sensor.HangCurrentReads = true;
        var running = Start();
        await Wait.Until(() => clock.PendingTimers >= 1);

        clock.Advance(LiveFeed.ReadTimeout);

        await Assert.ThrowsAsync<TimeoutException>(() => running);
        Assert.Empty(received);
    }

    [Fact]
    public async Task Poll_once_stops_waiting_for_a_read_that_never_answers_and_does_not_throw()
    {
        sensor.HangCurrentReads = true;
        var polling = feed.PollOnceAsync(sensor, device, interval, stop.Token);
        await Wait.Until(() => clock.PendingTimers >= 1);

        clock.Advance(LiveFeed.ReadTimeout);
        await polling;

        Assert.Single(logger.Entries);
    }


    [Fact]
    public async Task A_store_that_fails_is_logged_and_never_looks_like_a_lost_connection()
    {
        store.FailMerges = new IOException("disk full");
        var running = Start();
        await Wait.Until(() => logger.Entries.Count == 1);
        await Wait.Until(() => clock.PendingTimers >= 1);

        store.FailMerges = null;
        clock.Advance(interval + LiveFeed.PollMargin);

        await Wait.Until(() => received.Count == 1);
        Assert.False(running.IsCompleted);
        Assert.Contains("Storing", logger.Entries[0].Message);

        await stop.CancelAsync();
        await running;
    }
    [Fact]
    public async Task Poll_once_still_does_not_throw_when_the_connection_failed()
    {
        sensor.OnReadCurrent = _ => throw new IOException("Connection lost.");

        await feed.PollOnceAsync(sensor, device, interval, stop.Token);

        Assert.Single(logger.Entries);
    }

    [Fact]
    public async Task Run_ends_quietly_when_cancelled()
    {
        var running = Start();
        await Wait.Until(() => received.Count == 1);

        await stop.CancelAsync();
        await running;

        Assert.True(running.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Run_never_polls_in_parallel()
    {
        var running = Start();

        for (var poll = 1; poll <= 4; poll++)
        {
            await Wait.Until(() => received.Count == poll);
            await Wait.Until(() => clock.PendingTimers >= 1);
            clock.Advance(interval + LiveFeed.PollMargin);
        }

        await Wait.Until(() => received.Count == 5);
        await stop.CancelAsync();
        await running;

        Assert.Equal(1, sensor.MaxConcurrentReads);
    }

    [Fact]
    public async Task The_next_poll_counts_from_the_measurement_not_from_the_end_of_the_poll()
    {
        sensor.OnReadCurrent = _ => Samples.Co2(clock.GetUtcNow() - TimeSpan.FromSeconds(30), 800);
        var running = Start();
        await Wait.Until(() => received.Count == 1);
        await Wait.Until(() => clock.PendingTimers == 1);

        // Measured 30 s ago: the next value is due in 90 s, and the poll waits the margin after that.
        clock.Advance(TimeSpan.FromSeconds(90));
        await Wait.Quiet();
        Assert.Equal(1, sensor.ReadCurrentCalls);

        clock.Advance(LiveFeed.PollMargin);
        await Wait.Until(() => received.Count == 2);

        await stop.CancelAsync();
        await running;
    }

    [Fact]
    public async Task A_poll_that_still_got_the_old_value_is_repeated_after_the_margin_so_no_slot_is_skipped()
    {
        sensor.OnReadCurrent = _ => Samples.Co2(clock.GetUtcNow() - interval, 800);
        var running = Start();
        await Wait.Until(() => received.Count == 1);
        await Wait.Until(() => clock.PendingTimers == 1);

        clock.Advance(LiveFeed.PollMargin);

        await Wait.Until(() => received.Count == 2);

        await stop.CancelAsync();
        await running;
    }

    [Fact]
    public async Task A_reading_far_in_the_past_falls_back_to_one_interval()
    {
        sensor.OnReadCurrent = _ => Samples.Co2(clock.GetUtcNow() - TimeSpan.FromHours(3), 800);
        var running = Start();
        await Wait.Until(() => received.Count == 1);
        await Wait.Until(() => clock.PendingTimers == 1);

        clock.Advance(interval - TimeSpan.FromSeconds(1));
        await Wait.Quiet();
        Assert.Equal(1, sensor.ReadCurrentCalls);

        clock.Advance(TimeSpan.FromSeconds(1));
        await Wait.Until(() => received.Count == 2);

        await stop.CancelAsync();
        await running;
    }

    [Fact]
    public async Task Run_does_not_throw_when_already_cancelled()
    {
        await stop.CancelAsync();

        await Start();

        Assert.Equal(0, sensor.ReadCurrentCalls);
    }

    private static SensorBroadcast Broadcast(int minute, double ppm, int? battery = 80)
        => new(Samples.Co2(Samples.Utc(2026, 10, 9, 12).AddMinutes(minute), ppm), interval, battery);

    [Fact]
    public async Task Listen_stores_and_announces_each_broadcast_reading()
    {
        sensor.Broadcasts.AddRange([Broadcast(0, 800), Broadcast(2, 820)]);
        var listening = feed.ListenAsync(sensor, device, stop.Token);

        await Wait.Until(() => received.Count == 2);

        Assert.Equal([800, 820], received.Select(message => message.Reading.Co2Ppm));
        Assert.Equal(2, store.Readings.Count);
        Assert.All(store.Merges, merge => Assert.Equal(ReadingSource.Device, merge.Source));
        Assert.Equal(0, sensor.ReadCurrentCalls);

        await stop.CancelAsync();
        await listening;
    }

    [Fact]
    public async Task Listen_passes_on_the_middle_of_the_last_batteries_and_only_when_it_changes()
    {
        var statuses = new ConcurrentQueue<SensorStatusUpdated>();
        messenger.Register<SensorStatusUpdated>(this, (_, message) => statuses.Enqueue(message));
        sensor.Broadcasts.AddRange(new[] { 48, 43, 47, 46, 44, 48 }.Select((percent, index) => Broadcast(index * 2, 800 + index, percent)));
        var listening = feed.ListenAsync(sensor, device, stop.Token);

        await Wait.Until(() => received.Count == 6);

        Assert.Equal([48, 47, 46], statuses.Select(message => message.Status.BatteryPercent));
        Assert.All(statuses, message => Assert.Equal(interval, message.Status.Interval));

        await stop.CancelAsync();
        await listening;
    }

    [Fact]
    public async Task Listen_ends_quietly_when_cancelled()
    {
        var listening = feed.ListenAsync(sensor, device, stop.Token);
        await Wait.Until(() => sensor.ListenCalls == 1);

        await stop.CancelAsync();

        await listening;
    }
}
