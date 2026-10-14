using Deskair.App.Localization;
using Deskair.App.ViewModels.ChartModels;
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

        var (best, worst) = metric == Metric.Co2 ? HourGridCalculator.Extremes(days) : (null, null);
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

        var worstStart = metric == Metric.Co2 ? TypicalDayCalculator.WorstWindowStartHour(typical) : null;

        return new HourGridModel(
            rows,
            [.. Enumerable.Range(0, 8).Select(index => (index * 3, $"{index * 3:00}:00"))],
            metric == Metric.Co2 ? [.. co2Steps.Select(step => (step, formatter.Value(metric, step)))] : [])
        {
            Typical = typical.Count == 0 ? null : TypicalRow(typical),
            WorstStart = worstStart,
            WorstText = worstStart is { } start ? localizer.Format("History_Grid_Worst", ("from", Hour(start)), ("to", Hour(start + 3))) : null,
            Night = night.IsEmpty ? null : (night.Start.Hour, night.End.Hour),
            NightText = night.IsEmpty ? null : localizer.Format("History_Grid_Night", ("from", night.Start.ToString("HH:mm", localizer.Culture)), ("to", night.End.ToString("HH:mm", localizer.Culture))),
            BestText = localizer.Get("History_Grid_TagBest"),
            WorstTagText = localizer.Get("History_Grid_TagWorst"),
            GoodText = localizer.Get("Zone_Good"),
            ElevatedText = localizer.Get("Zone_Elevated"),
            HighText = localizer.Get("Zone_High"),
            NoDataText = localizer.Get("Chart_NoData"),
            UnitText = localizer.Get("History_Grid_UnitHour")
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
                Emphasis: isToday);
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

    private string Short(DateOnly date) => date.ToString("d MMM", localizer.Culture);

    private static string Hour(int hour) => $"{hour:00}:00";
}
