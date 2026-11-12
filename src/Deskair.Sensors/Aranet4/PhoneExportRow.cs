using System.Globalization;
using Deskair.Domain.Readings;

namespace Deskair.Sensors.Aranet4;

/// <summary>The row layout shared by the phone app's text and .xlsx exports: time, CO₂, temperature, humidity, pressure.</summary>
internal static class PhoneExportRow
{
    // The phone app writes dates in the order of its own settings and names that order in the header (sync-and-import.md rule 2). The 12-hour forms are for a phone set to a 12-hour clock.
    private static readonly string[] dayFirstFormats = ["d/M/yyyy H:mm:ss", "d/M/yyyy H:mm", "d/M/yyyy h:mm:ss tt", "d/M/yyyy h:mm tt"];
    private static readonly string[] monthFirstFormats = ["M/d/yyyy H:mm:ss", "M/d/yyyy H:mm", "M/d/yyyy h:mm:ss tt", "M/d/yyyy h:mm tt"];

    public const int ColumnCount = 5;

    public static bool TryParseTime(string text, DateOrder order, out DateTime wallClock)
    {
        var formats = order == DateOrder.MonthFirst ? monthFirstFormats : dayFirstFormats;

        return DateTime.TryParseExact(text.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out wallClock);
    }

    /// <summary>The date order a header names, such as <c>Time(DD/MM/YYYY H:mm:ss)</c> or <c>Time(MM/DD/YYYY h:mm:ss A)</c>. False when the text names none.</summary>
    public static bool TryReadDateOrder(string header, out DateOrder order)
    {
        if (header.Contains("DD/MM", StringComparison.OrdinalIgnoreCase))
        {
            order = DateOrder.DayFirst;
            return true;
        }

        if (header.Contains("MM/DD", StringComparison.OrdinalIgnoreCase))
        {
            order = DateOrder.MonthFirst;
            return true;
        }

        order = DateOrder.DayFirst;
        return false;
    }

    public static bool TryParseNumber(string text, bool commaIsDecimal, out double value)
    {
        var normalised = commaIsDecimal ? text.Trim().Replace(',', '.') : text.Trim();

        return double.TryParse(normalised, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            && double.IsFinite(value);
    }

    /// <summary>
    /// Converts a wall-clock time in the zone to UTC. A time repeated at the end of summer time maps to its first
    /// occurrence; a time skipped at the start of summer time maps with the offset from before the change.
    /// </summary>
    public static DateTimeOffset ToUtc(DateTime wallClock, TimeZoneInfo zone)
    {
        var unspecified = DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified);

        if (zone.IsInvalidTime(unspecified))
        {
            var offsetBefore = zone.GetUtcOffset(unspecified.AddHours(-1));

            return new DateTimeOffset(unspecified - offsetBefore, TimeSpan.Zero);
        }

        var offset = zone.IsAmbiguousTime(unspecified)
            ? zone.GetAmbiguousTimeOffsets(unspecified).Max()
            : zone.GetUtcOffset(unspecified);

        return new DateTimeOffset(unspecified - offset, TimeSpan.Zero);
    }

    public static Reading? Build(
        DateTime wallClock,
        double co2,
        double temperature,
        double humidity,
        double pressure,
        TimeZoneInfo zone)
        => new(ToUtc(wallClock, zone), co2, temperature, humidity, pressure);
}
