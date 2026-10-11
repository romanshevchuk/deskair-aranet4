using System.Globalization;
using Deskair.Domain.Readings;
using Deskair.Services.Storage;

namespace Deskair.Storage.Readings;

/// <summary>
/// One row of a monthly readings file: <c>ts_utc,co2_ppm,temp_c,rh_pct,pres_hpa,src</c>.
/// Always the invariant culture and UTC, whatever the user's language.
/// </summary>
public static class CsvReadingFormat
{
    public const string Header = "ts_utc,co2_ppm,temp_c,rh_pct,pres_hpa,src";

    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";
    private const int ColumnCount = 6;

    public static string Write(Reading reading, ReadingSource source)
    {
        var culture = CultureInfo.InvariantCulture;

        return string.Join(
            ',',
            reading.Timestamp.UtcDateTime.ToString(TimestampFormat, culture),
            reading.Co2Ppm.ToString("0.##", culture),
            reading.TemperatureC.ToString("0.0#", culture),
            reading.HumidityPct.ToString("0.##", culture),
            reading.PressureHpa.ToString("0.0#", culture),
            source == ReadingSource.Import ? "imp" : "dev");
    }

    public static bool TryParse(string line, out Reading reading, out ReadingSource source)
    {
        reading = default;
        source = ReadingSource.Device;

        var columns = line.Split(',');

        if (columns.Length != ColumnCount)
        {
            return false;
        }

        if (!TryParseTimestamp(columns[0], out var timestamp))
        {
            return false;
        }

        if (!TryParseNumber(columns[1], out var co2) ||
            !TryParseNumber(columns[2], out var temperature) ||
            !TryParseNumber(columns[3], out var humidity) ||
            !TryParseNumber(columns[4], out var pressure))
        {
            return false;
        }

        if (!TryParseSource(columns[5], out source))
        {
            return false;
        }

        reading = new Reading(timestamp, co2, temperature, humidity, pressure);

        return true;
    }

    private static bool TryParseTimestamp(string text, out DateTimeOffset timestamp)
    {
        return DateTimeOffset.TryParseExact(
            text,
            TimestampFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out timestamp);
    }

    private static bool TryParseNumber(string text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
            double.IsFinite(value);
    }

    private static bool TryParseSource(string text, out ReadingSource source)
    {
        source = ReadingSource.Device;

        switch (text)
        {
            case "dev":
                return true;
            case "imp":
                source = ReadingSource.Import;
                return true;
            default:
                return false;
        }
    }
}
