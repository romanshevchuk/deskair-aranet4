using Deskair.Domain.Readings;
using Deskair.Domain.Time;

namespace Deskair.Domain.Analytics.HoursFilter;

public static class HoursFilterRule
{
    public static bool Includes(DateTimeOffset instant, HoursFilterMode filter, LocalCalendar calendar, NightWindow night)
    {
        if (filter == HoursFilterMode.AllDay)
        {
            return true;
        }

        return night.Includes(TimeZoneInfo.ConvertTime(instant, calendar.Zone).TimeOfDay);
    }

    public static IReadOnlyList<Reading> Apply(
        IReadOnlyList<Reading> readings,
        HoursFilterMode filter,
        LocalCalendar calendar,
        NightWindow night)
        => readings.Where(reading => Includes(reading.Timestamp, filter, calendar, night)).ToList();
}
