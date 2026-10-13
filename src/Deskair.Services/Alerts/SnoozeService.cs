using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Time;
using Deskair.Services.Messages;

namespace Deskair.Services.Alerts;

/// <summary>
/// The one place that snoozes alerts, for the toast button, the tray menu and the flyout. "1 hour" is an hour from now; "until tomorrow" is the next 07:00 local time
/// (the end of the default quiet hours), which is later the same morning when it is still night. Every change is announced as <see cref="AlertsSnoozeChanged"/>.
/// </summary>
public sealed class SnoozeService
{
    private static readonly TimeSpan oneHour = TimeSpan.FromHours(1);
    private static readonly TimeSpan morning = TimeSpan.FromHours(7);

    private readonly AlertEngine engine;
    private readonly TimeProvider timeProvider;
    private readonly LocalCalendar calendar;
    private readonly IMessenger messenger;

    public SnoozeService(AlertEngine engine, TimeProvider timeProvider, LocalCalendar calendar, IMessenger messenger)
    {
        this.engine = engine;
        this.timeProvider = timeProvider;
        this.calendar = calendar;
        this.messenger = messenger;
    }

    /// <summary>When the snooze ends, or null when alerts are not snoozed (a snooze that has ended counts as none).</summary>
    public DateTimeOffset? Until => engine.SnoozedUntil is { } until && until > timeProvider.GetUtcNow() ? until : null;

    public void ForOneHour() => Snooze(timeProvider.GetUtcNow() + oneHour);

    public void UntilTomorrow() => Snooze(NextMorning(timeProvider.GetUtcNow()));

    /// <summary>Ends the snooze now.</summary>
    public void Resume()
    {
        engine.ClearSnooze();
        messenger.Send(new AlertsSnoozeChanged(null));
    }

    private void Snooze(DateTimeOffset until)
    {
        engine.SnoozeUntil(until);
        messenger.Send(new AlertsSnoozeChanged(until));
    }

    private DateTimeOffset NextMorning(DateTimeOffset now)
    {
        var date = calendar.LocalDateOf(now);
        var candidate = AtMorning(date);

        return candidate > now ? candidate : AtMorning(date.AddDays(1));
    }

    private DateTimeOffset AtMorning(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.FromTimeSpan(morning));

        return new DateTimeOffset(local, calendar.Zone.GetUtcOffset(local));
    }
}
