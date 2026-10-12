namespace Deskair.Tests.Fakes;

/// <summary>
/// A clock tests move by hand. Timers (and so <c>Task.Delay</c> with this clock) fire only when a test calls <see cref="Advance"/>.
/// The local zone is UTC unless a test gives another one.
/// </summary>
public sealed class FakeClock : TimeProvider
{
    private readonly TimeZoneInfo localZone;
    private readonly Lock gate = new();
    private readonly List<FakeTimer> timers = [];
    private DateTimeOffset now;

    public FakeClock(DateTimeOffset start, TimeZoneInfo? localZone = null)
    {
        now = start;
        this.localZone = localZone ?? TimeZoneInfo.Utc;
    }

    public override TimeZoneInfo LocalTimeZone => localZone;

    public override DateTimeOffset GetUtcNow()
    {
        lock (gate)
        {
            return now;
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new FakeTimer(this, callback, state);
        timer.Change(dueTime, period);

        lock (gate)
        {
            timers.Add(timer);
        }

        return timer;
    }

    public void Advance(TimeSpan amount)
    {
        DateTimeOffset target;

        lock (gate)
        {
            target = now + amount;
        }

        while (true)
        {
            FakeTimer? next;

            lock (gate)
            {
                next = timers
                    .Where(timer => timer.Due is not null && timer.Due <= target)
                    .MinBy(timer => timer.Due);

                if (next is null)
                {
                    now = target;
                    return;
                }

                now = next.Due!.Value > now ? next.Due.Value : now;
            }

            next.Fire();
        }
    }

    private void Remove(FakeTimer timer)
    {
        lock (gate)
        {
            timers.Remove(timer);
        }
    }

    private sealed class FakeTimer : ITimer
    {
        private readonly FakeClock clock;
        private readonly TimerCallback callback;
        private readonly object? state;
        private TimeSpan period = Timeout.InfiniteTimeSpan;

        public FakeTimer(FakeClock clock, TimerCallback callback, object? state)
        {
            this.clock = clock;
            this.callback = callback;
            this.state = state;
        }

        public DateTimeOffset? Due { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan newPeriod)
        {
            period = newPeriod;
            Due = dueTime == Timeout.InfiniteTimeSpan ? null : clock.GetUtcNow() + dueTime;

            return true;
        }

        public void Fire()
        {
            Due = period == Timeout.InfiniteTimeSpan || period == TimeSpan.Zero ? null : Due + period;
            callback(state);
        }

        public void Dispose()
        {
            Due = null;
            clock.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();

            return ValueTask.CompletedTask;
        }
    }
}
