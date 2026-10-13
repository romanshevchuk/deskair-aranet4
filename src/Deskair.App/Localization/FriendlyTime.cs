using Deskair.Domain.Time;

namespace Deskair.App.Localization;

/// <summary>A moment as people say it: "14:30" when it is today, "14 Oct, 07:00" on another day. Local time, month in the selected language.</summary>
public sealed class FriendlyTime
{
    private readonly ILocalizer localizer;
    private readonly LocalCalendar calendar;
    private readonly TimeProvider timeProvider;

    public FriendlyTime(ILocalizer localizer, LocalCalendar calendar, TimeProvider timeProvider)
    {
        this.localizer = localizer;
        this.calendar = calendar;
        this.timeProvider = timeProvider;
    }

    public string Text(DateTimeOffset instant)
    {
        var local = TimeZoneInfo.ConvertTime(instant, calendar.Zone);
        var today = calendar.LocalDateOf(timeProvider.GetUtcNow());

        return calendar.LocalDateOf(instant) == today
            ? local.ToString("HH:mm", localizer.Culture)
            : local.ToString("d MMM, HH:mm", localizer.Culture);
    }
}
