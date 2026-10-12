using System.Globalization;
using Deskair.Domain.Readings;

namespace Deskair.Sensors.Aranet4;

/// <summary>The row layout shared by the phone app's text and .xlsx exports: time, CO₂, temperature, humidity, pressure.</summary>
internal static class PhoneExportRow
{
    // Day first, always. Month-first files are not supported; the importer never guesses (sync-and-import.md rule 2).
    private static readonly string[] timeFormats = ["dd/MM/yyyy H:mm:ss", "d/M/yyyy H:mm:ss", "d/M/yyyy H:mm"];

    public const int ColumnCount = 5;

    public static bool TryParseTime(string text, out DateTime wallClock)
        => DateTime.TryParseExact(text.Trim(), timeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out wallClock);

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
