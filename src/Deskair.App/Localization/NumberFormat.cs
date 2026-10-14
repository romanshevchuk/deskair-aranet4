using System.Globalization;
using Deskair.Domain.Metrics;

namespace Deskair.App.Localization;

/// <summary>
/// The number formats of the UI (behaviour-rules.md "Number and time format", D-0020): the decimal mark follows the language, metric values never have a group separator,
/// and a percent is "59%" in English and "59 %" (narrow no-break space) in Ukrainian.
/// </summary>
public static class NumberFormat
{
    /// <summary>The format string for whole numbers: no group separator.</summary>
    public const string WholeFormat = "0";

    /// <summary>The format string for one decimal: no group separator.</summary>
    public const string OneDecimalFormat = "0.0";

    private const string NarrowNoBreakSpace = " ";

    /// <summary>The format string for a metric: one decimal for temperature and pressure, none for the others.</summary>
    public static string FormatFor(Metric metric)
        => metric is Metric.Temperature or Metric.Pressure ? OneDecimalFormat : WholeFormat;

    public static string Whole(double value, CultureInfo culture)
        => value.ToString(WholeFormat, culture);

    public static string OneDecimal(double value, CultureInfo culture)
        => value.ToString(OneDecimalFormat, culture);

    public static string Value(Metric metric, double value, CultureInfo culture)
        => value.ToString(FormatFor(metric), culture);

    /// <summary>A share from 0 to 1 as a whole percent. Values outside the range are clamped.</summary>
    public static string Percent(double fraction, CultureInfo culture)
    {
        var whole = Math.Round(Math.Clamp(fraction, 0, 1) * 100, MidpointRounding.AwayFromZero);
        var separator = culture.TwoLetterISOLanguageName == "uk" ? NarrowNoBreakSpace : string.Empty;

        return whole.ToString(WholeFormat, culture) + separator + "%";
    }
}
