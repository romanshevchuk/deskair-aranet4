using Deskair.App.Localization;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Comfort;
using Deskair.Domain.Analytics.HourGrid;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;

namespace Deskair.App.ViewModels.History;

/// <summary>
/// Turns the grid rows of a period into what the History grid draws, in the selected language (D-0018, screens-and-states.md "History views"):
/// row labels, two or more days without readings folded into one row, the best and worst day, tooltips, the typical row and its worst three hours,
/// the night bracket and the legend.
/// </summary>
public sealed class HourGridBuilder(ILocalizer localizer, MetricFormatter formatter)
{
    private static readonly double[] co2Steps = [800, 1000, 1200, 1400, 2000];
    private static readonly int[] yearLabelDays = [1, 5, 10, 15, 20, 25, 31];

    public HourGridModel Build(
        IReadOnlyList<HourGridDay> days,
        IReadOnlyList<HourOfDayAverage> typical,
        Metric metric,
        Period range,
        NightWindow night,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(days);
        ArgumentNullException.ThrowIfNull(typical);
        ArgumentNullException.ThrowIfNull(night);

        var scale = ComfortScale.For(metric);
        var (best, worst) = metric == Metric.Co2 ? HourGridCalculator.Extremes(days) : scale is null ? (null, null) : (null, HourGridCalculator.ComfortWorst(days));
        var rows = new List<HourGridRow>();
        var run = new List<HourGridDay>();

        foreach (var day in days)
        {
            // Today is never folded away, even before its first reading.
            if (day.IsEmpty && day.Date != today)
            {
                run.Add(day);
                continue;
            }

            Flush();
            rows.Add(Row(day, best, worst));
        }

        Flush();

        var (worstStart, worstText) = WorstWindow(metric, scale, typical);

        return new HourGridModel(
            rows,
            [.. Enumerable.Range(0, 8).Select(index => (index * 3, $"{index * 3:00}:00"))],
            Steps(metric, scale))
        {
            Typical = typical.Count == 0 ? null : TypicalRow(typical),
            WorstStart = worstStart,
            WorstText = worstText,
            Night = night.IsEmpty ? null : (night.Start.Hour, night.End.Hour),
            NightText = night.IsEmpty ? null : localizer.Format("History_Grid_Night", ("from", night.Start.ToString("HH:mm", localizer.Culture)), ("to", night.End.ToString("HH:mm", localizer.Culture))),
            BestText = localizer.Get("History_Grid_TagBest"),
            WorstTagText = localizer.Get("History_Grid_TagWorst"),
            Palette = PaletteOf(metric),
            LegendGroups = LegendGroups(metric),
            NoDataText = localizer.Get("Chart_NoData"),
            UnitText = localizer.Get("History_Grid_UnitHour"),
            BarCaption = BarCaption(metric)
        };

        void Flush()
        {
            if (run.Count >= 2)
            {
                rows.Add(new HourGridRow(string.Empty, [], CollapsedText: localizer.Format("History_Grid_NoReadings", ("from", Short(run[0].Date)), ("to", Short(run[^1].Date)))));
            }
            else
            {
                rows.AddRange(run.Select(day => Row(day, best, worst)));
            }

            run.Clear();
        }

        HourGridRow Row(HourGridDay day, DateOnly? bestDay, DateOnly? worstDay)
        {
            var isToday = day.Date == today;
            var label = isToday
                ? localizer.Get("Chart_Today")
                : day.Date.ToString(range == Period.Week ? "ddd d" : "ddd d MMM", localizer.Culture);
            var dayText = day.Date.ToString("ddd d MMM", localizer.Culture);
            var cells = Enumerable.Range(0, 24)
                .Select(hour => hour < day.HoursShown ? Cell(day.Hours[hour], localizer.Format("History_Grid_Cell", ("day", dayText), ("from", Hour(hour)), ("to", Hour(hour + 1)))) : null)
                .ToList();
            var tag = day.Date == bestDay ? HourGridTag.Best : day.Date == worstDay ? HourGridTag.Worst : HourGridTag.None;

            return new HourGridRow(
                label,
                cells,
                day.Average is { } average ? formatter.Value(metric, average) : null,
                day.Share,
                tag,
                LabelAlways: isToday || day.Date.DayOfWeek == DayOfWeek.Monday,
                Emphasis: isToday,
                Comfort: day.Comfort);
        }

        HourGridCell Cell(double? stored, string where)
        {
            return stored is { } value
                ? new HourGridCell(formatter.ToDisplay(metric, value), localizer.Format("History_Grid_Value", ("where", where), ("value", formatter.Value(metric, value)), ("unit", formatter.Unit(metric))))
                : new HourGridCell(null, localizer.Format("History_Grid_Value_None", ("where", where)));
        }

        HourGridRow TypicalRow(IReadOnlyList<HourOfDayAverage> hours)
        {
            var cells = Enumerable.Range(0, 24)
                .Select(hour => hours.FirstOrDefault(item => item.Hour == hour)?.Average)
                .Select((average, hour) => (HourGridCell?)Cell(average, localizer.Format("History_Grid_TypicalCell", ("from", Hour(hour)), ("to", Hour(hour + 1)))))
                .ToList();

            return new HourGridRow(localizer.Get("History_Grid_Typical"), cells, LabelAlways: true, Emphasis: true);
        }
    }

    /// <summary>
    /// The Year grid (D-0018, screens-and-states.md "History views"): one row per month, one square per day in the same heat scale, day numbers 1, 5, 10 ... 31 above, rows ending as in Week,
    /// the best and the worst month tagged, no typical row and no night bracket. Two or more months in a row without readings are folded into one row, as days are in Week and Month.
    /// </summary>
    public HourGridModel BuildYear(IReadOnlyList<HourGridMonth> months, Metric metric, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(months);

        var scale = ComfortScale.For(metric);
        var (best, worst) = metric == Metric.Co2 ? HourGridCalculator.Extremes(months) : scale is null ? (null, null) : (null, HourGridCalculator.ComfortWorst(months));
        var current = new DateOnly(today.Year, today.Month, 1);
        var rows = new List<HourGridRow>();
        var run = new List<HourGridMonth>();

        foreach (var month in months)
        {
            if (month.IsEmpty && month.Month != current)
            {
                run.Add(month);
                continue;
            }

            Flush();
            rows.Add(MonthRow(month));
        }

        Flush();

        return new HourGridModel(
            rows,
            [.. yearLabelDays.Select(day => (day - 1, day.ToString(localizer.Culture)))],
            Steps(metric, scale))
        {
            Columns = 31,
            CenterColumnLabels = true,
            BestText = localizer.Get("History_Grid_TagBest"),
            WorstTagText = localizer.Get("History_Grid_TagWorst"),
            Palette = PaletteOf(metric),
            LegendGroups = LegendGroups(metric),
            NoDataText = localizer.Get("Chart_NoData"),
            UnitText = localizer.Get("History_Grid_UnitDay"),
            BarCaption = BarCaption(metric)
        };

        void Flush()
        {
            if (run.Count >= 2)
            {
                rows.Add(new HourGridRow(string.Empty, [], CollapsedText: localizer.Format("History_Grid_NoReadings", ("from", MonthName(run[0].Month)), ("to", MonthName(run[^1].Month)))));
            }
            else
            {
                rows.AddRange(run.Select(MonthRow));
            }

            run.Clear();
        }

        HourGridRow MonthRow(HourGridMonth month)
        {
            var cells = Enumerable.Range(0, 31)
                .Select(index => index < month.DaysShown ? DayCell(metric, month.Days[index], month.Month.AddDays(index)) : null)
                .ToList();
            var tag = month.Month == best ? HourGridTag.Best : month.Month == worst ? HourGridTag.Worst : HourGridTag.None;

            return new HourGridRow(
                MonthName(month.Month),
                cells,
                month.Average is { } average ? formatter.Value(metric, average) : null,
                month.Share,
                tag,
                LabelAlways: true,
                Emphasis: month.Month == current,
                Comfort: month.Comfort);
        }
    }

    private string BarCaption(Metric metric) => localizer.Get(metric == Metric.Co2 ? "History_Grid_BarZones" : "History_Grid_BarRange");

    private static HourGridPalette PaletteOf(Metric metric) => metric switch
    {
        Metric.Temperature => HourGridPalette.Temperature,
        Metric.Humidity => HourGridPalette.Humidity,
        _ => HourGridPalette.Zones
    };

    /// <summary>The step limits in the unit shown, with their labels: CO₂ zones, or a comfort scale; none for other metrics.</summary>
    private IReadOnlyList<(double Value, string Text)> Steps(Metric metric, ComfortScale? scale)
    {
        IReadOnlyList<double> limits = metric == Metric.Co2 ? co2Steps : scale?.Steps ?? [];

        return [.. limits.Select(step => (formatter.ToDisplay(metric, step), formatter.Value(metric, step)))];
    }

    /// <summary>The names above the legend swatches: Good, Elevated and High over pairs of steps, or the low side, comfortable and the high side.</summary>
    private IReadOnlyList<(string Name, int First, int Count)> LegendGroups(Metric metric) => metric switch
    {
        Metric.Co2 => [(localizer.Get("Zone_Good"), 0, 2), (localizer.Get("Zone_Elevated"), 2, 2), (localizer.Get("Zone_High"), 4, 2)],
        Metric.Temperature => [(localizer.Get("Comfort_Cool"), 0, 2), (localizer.Get("Comfort_Comfortable"), 2, 1), (localizer.Get("Comfort_Warm"), 3, 2)],
        Metric.Humidity => [(localizer.Get("Comfort_Dry"), 0, 2), (localizer.Get("Comfort_Comfortable"), 2, 1), (localizer.Get("Comfort_Humid"), 3, 2)],
        _ => []
    };

    /// <summary>The outlined three hours of the typical row and their label: the highest for CO₂, the furthest outside the comfortable range for Temperature and Humidity.</summary>
    private (int? Start, string? Text) WorstWindow(Metric metric, ComfortScale? scale, IReadOnlyList<HourOfDayAverage> typical)
    {
        if (metric == Metric.Co2)
        {
            return TypicalDayCalculator.WorstWindowStartHour(typical) is { } start
                ? (start, localizer.Format("History_Grid_Worst", ("from", Hour(start)), ("to", Hour(start + 3))))
                : (null, null);
        }

        if (scale is null || TypicalDayCalculator.FurthestOutsideWindow(typical, scale) is not { } window)
        {
            return (null, null);
        }

        var key = (metric, scale.Side(window.Average)) switch
        {
            (Metric.Temperature, ComfortSide.Low) => "History_Grid_Coolest",
            (Metric.Temperature, _) => "History_Grid_Warmest",
            (_, ComfortSide.Low) => "History_Grid_Driest",
            _ => "History_Grid_MostHumid"
        };

        return (window.StartHour, localizer.Format(key, ("from", Hour(window.StartHour)), ("to", Hour(window.StartHour + 3))));
    }

    private HourGridCell DayCell(Metric metric, double? stored, DateOnly date)
    {
        var where = date.ToString("ddd d MMM yyyy", localizer.Culture);

        return stored is { } value
            ? new HourGridCell(formatter.ToDisplay(metric, value), localizer.Format("History_Grid_Value", ("where", where), ("value", formatter.Value(metric, value)), ("unit", formatter.Unit(metric))))
            : new HourGridCell(null, localizer.Format("History_Grid_Value_None", ("where", where)));
    }

    private string MonthName(DateOnly month) => month.ToString("MMM yyyy", localizer.Culture);

    private string Short(DateOnly date) => date.ToString("d MMM", localizer.Culture);

    private static string Hour(int hour) => $"{hour:00}:00";
}
