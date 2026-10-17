using System.Globalization;
using Deskair.App.Localization;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;

namespace Deskair.App.ViewModels.History;

/// <summary>
/// The title of a History period in the selected language (behaviour-rules.md "History periods"): "Last 24 hours"; a calendar day ("Tue 13 Oct");
/// a span of days ("8 – 14 Oct 2026", "29 Sep – 5 Oct 2026"); a span of months ("Nov 2025 – Oct 2026"). Each side shows only what differs.
/// </summary>
public sealed class PeriodLabeler(ILocalizer localizer, LocalCalendar calendar)
{
    public string Label(HistoryPeriod period)
    {
        ArgumentNullException.ThrowIfNull(period);

        if (period.IsRolling)
        {
            return localizer.Get("History_Period_Last24Hours");
        }

        var culture = localizer.Culture;
        var (first, last) = period.Dates(calendar);

        return period.Range switch
        {
            Period.Day => first.ToString("ddd d MMM", culture),
            Period.Year => Span(first.ToString("MMM yyyy", culture), last.ToString("MMM yyyy", culture)),
            _ => Span(From(first, last, culture), last.ToString("d MMM yyyy", culture))
        };
    }

    /// <summary>"No readings for 24 – 30 Sep 2026", for a period before the first reading.</summary>
    public string EmptyTitle(string periodLabel) => localizer.Format("History_Period_Empty_Title", ("period", periodLabel));

    /// <summary>"Deskair has readings from 6 Oct 2026."</summary>
    public string EmptyText(DateTimeOffset firstReading)
        => localizer.Format("History_Period_Empty_Text", ("date", calendar.LocalDateOf(firstReading).ToString("d MMM yyyy", localizer.Culture)));

    /// <summary>What Narrator reads for the History grid: "CO₂ by hour, 8 – 14 Oct 2026", or "CO₂ by day, Nov 2025 – Oct 2026" for Year.</summary>
    public string GridName(Metric metric, string period, bool byDay = false)
        => localizer.Format(byDay ? "History_Grid_NameDay" : "History_Grid_Name", ("metric", localizer.Get($"Metric_{metric}")), ("period", period));

    private string Span(string from, string to) => localizer.Format("History_Period_Span", ("from", from), ("to", to));

    private static string From(DateOnly first, DateOnly last, CultureInfo culture)
    {
        if (first.Year != last.Year)
        {
            return first.ToString("d MMM yyyy", culture);
        }

        return first.Month != last.Month ? first.ToString("d MMM", culture) : first.ToString("%d", culture);
    }
}
