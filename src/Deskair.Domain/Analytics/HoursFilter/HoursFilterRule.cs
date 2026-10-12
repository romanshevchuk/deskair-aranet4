using Deskair.Domain.Readings;
using Deskair.Domain.Time;

namespace Deskair.Domain.Analytics.HoursFilter;

public static class HoursFilterRule
{
    private static readonly TimeSpan nightStart = TimeSpan.FromHours(22);
    private static readonly TimeSpan nightEnd = TimeSpan.FromHours(7);

    public static bool Includes(DateTimeOffset instant, HoursFilterMode filter, LocalCalendar calendar)
    {
        if (filter == HoursFilterMode.AllDay)
        {
            return true;
        }

        var timeOfDay = TimeZoneInfo.ConvertTime(instant, calendar.Zone).TimeOfDay;
        return timeOfDay >= nightStart || timeOfDay < nightEnd;
    }

    public static DateOnly NightOwner(DateTimeOffset instant, LocalCalendar calendar)
    {
        var local = TimeZoneInfo.ConvertTime(instant, calendar.Zone);
        var date = DateOnly.FromDateTime(local.DateTime);
        return local.TimeOfDay < nightEnd ? date.AddDays(-1) : date;
    }

    public static IReadOnlyList<Reading> Apply(
        IReadOnlyList<Reading> readings,
        HoursFilterMode filter,
        LocalCalendar calendar)
        => readings.Where(reading => Includes(reading.Timestamp, filter, calendar)).ToList();
}
