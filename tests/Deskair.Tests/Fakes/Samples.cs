using Deskair.Domain.Readings;

namespace Deskair.Tests.Fakes;

/// <summary>Small helpers that build readings and times for tests.</summary>
public static class Samples
{
    /// <summary>Kyiv is UTC+2, and UTC+3 in summer. Daylight saving 2026 starts on 29 March and ends on 25 October.</summary>
    public static TimeZoneInfo Kyiv => TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");

    public static Reading Co2(DateTimeOffset time, double ppm)
    {
        return new Reading(time, ppm, 21.0, 45, 1013);
    }

    public static Reading Climate(DateTimeOffset time, double temperature, double humidity, double pressure)
    {
        return new Reading(time, 600, temperature, humidity, pressure);
    }

    public static DateTimeOffset Utc(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
    {
        return new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
    }
}
