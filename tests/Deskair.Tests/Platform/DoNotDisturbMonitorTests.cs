using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Platform.Notifications;
using Deskair.Services.Messages;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Platform;

public sealed class DoNotDisturbMonitorTests : IDisposable
{
    private sealed class FakeReader : IDoNotDisturbReader
    {
        public bool? State { get; set; } = false;

        public int Reads { get; private set; }

        public bool? Read()
        {
            Reads++;

            return State;
        }
    }

    private readonly StrongReferenceMessenger messenger = new();
    private readonly FakeClock clock = new(Samples.Utc(2026, 10, 13, 12));
    private readonly FakeReader reader = new();
    private readonly List<DoNotDisturbChanged> changes = [];
    private readonly DoNotDisturbMonitor monitor;

    public DoNotDisturbMonitorTests()
    {
        monitor = new DoNotDisturbMonitor(reader, messenger, clock);
        messenger.Register<DoNotDisturbChanged>(this, (_, message) => changes.Add(message));
    }

    public void Dispose()
    {
        monitor.Dispose();
    }

    [Fact]
    public void It_is_active_while_windows_says_do_not_disturb_is_on()
    {
        reader.State = true;

        Assert.True(monitor.IsActive);
    }

    [Fact]
    public void It_is_not_active_while_windows_says_it_is_off()
    {
        reader.State = false;

        Assert.False(monitor.IsActive);
    }

    [Fact]
    public void A_state_that_cannot_be_read_counts_as_allowed()
    {
        reader.State = null;

        Assert.False(monitor.IsActive);
    }

    [Fact]
    public void The_state_is_read_when_asked_so_an_alert_never_sees_an_old_answer()
    {
        reader.State = false;
        Assert.False(monitor.IsActive);

        reader.State = true;

        Assert.True(monitor.IsActive);
    }

    [Fact]
    public async Task A_change_is_announced_once_when_the_next_look_finds_it()
    {
        monitor.Start();
        await Wait.Until(() => clock.PendingTimers >= 1);

        reader.State = true;
        clock.Advance(DoNotDisturbMonitor.PollEvery);

        await Wait.Until(() => changes.Count == 1);
        Assert.Equal([new DoNotDisturbChanged(true)], changes);
    }

    [Fact]
    public async Task Nothing_is_announced_while_the_state_stays_the_same()
    {
        monitor.Start();
        await Wait.Until(() => clock.PendingTimers >= 1);

        clock.Advance(DoNotDisturbMonitor.PollEvery);
        clock.Advance(DoNotDisturbMonitor.PollEvery);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.Empty(changes);
    }

    [Fact]
    public async Task Turning_it_off_again_is_announced_too()
    {
        reader.State = true;
        monitor.Start();
        await Wait.Until(() => clock.PendingTimers >= 1);

        reader.State = false;
        clock.Advance(DoNotDisturbMonitor.PollEvery);

        await Wait.Until(() => changes.Count == 1);
        Assert.Equal(new DoNotDisturbChanged(false), changes[0]);
    }

    [Fact]
    public async Task A_state_that_becomes_unreadable_is_announced_as_off()
    {
        reader.State = true;
        monitor.Start();
        await Wait.Until(() => clock.PendingTimers >= 1);

        reader.State = null;
        clock.Advance(DoNotDisturbMonitor.PollEvery);

        await Wait.Until(() => changes.Count == 1);
        Assert.Equal(new DoNotDisturbChanged(false), changes[0]);
    }

    [Fact]
    public async Task After_dispose_nothing_is_read_any_more()
    {
        monitor.Start();
        await Wait.Until(() => clock.PendingTimers >= 1);
        monitor.Dispose();
        var reads = reader.Reads;

        clock.Advance(DoNotDisturbMonitor.PollEvery);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.Equal(reads, reader.Reads);
    }
}
