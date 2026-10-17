using Deskair.App.Localization;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.AirImproved;
using Deskair.Domain.Analytics.Comfort;
using Deskair.Domain.Analytics.HourGrid;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Analytics.Pressure;
using Deskair.Domain.Analytics.Sleep;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;

namespace Deskair.App.ViewModels.History;

/// <summary>
/// What the History Day chart draws besides the readings (screens-and-states.md "History views"): the night hours as spans of time, the typical day as a line over the
/// shown window, and the "air improved" markers with their hover texts.
/// </summary>
public sealed class DayChartBuilder(ILocalizer localizer, MetricFormatter formatter, LocalCalendar calendar)
{
    /// <summary>The nights that overlap the window, clipped to it. Empty when the night window is empty.</summary>
    public IReadOnlyList<ChartGap> NightSpans(NightWindow night, DateTimeOffset start, DateTimeOffset end)
    {
        ArgumentNullException.ThrowIfNull(night);

        if (night.IsEmpty || end <= start)
        {
            return [];
        }

        var spans = new List<ChartGap>();
        var last = calendar.LocalDateOf(end - TimeSpan.FromTicks(1));

        // One day early: the night that began the evening before may still be going on at the start.
        for (var date = calendar.LocalDateOf(start).AddDays(-1); date <= last; date = date.AddDays(1))
        {
            var from = Max(calendar.InstantAt(date, night.Start), start);
            var to = Min(calendar.InstantAt(night.CrossesMidnight ? date.AddDays(1) : date, night.End), end);

            if (to > from)
            {
                spans.Add(new ChartGap(from, to));
            }
        }

        return spans;
    }

    /// <summary>The Pressure line for Week and Month: one point in the middle of each hour that has readings, at that hour's mean.</summary>
    public PressureSeries PressureLine(IReadOnlyList<HourGridDay> days)
    {
        ArgumentNullException.ThrowIfNull(days);

        var series = new List<TimedValue>();

        foreach (var day in days)
        {
            for (var hour = 0; hour < day.HoursShown && hour < day.Hours.Count; hour++)
            {
                if (day.Hours[hour] is { } mean)
                {
                    series.Add(new TimedValue(mean, calendar.InstantAt(day.Date, new TimeOnly(hour, 30))));
                }
            }
        }

        return Series(series, TimeSpan.FromHours(1));
    }

    /// <summary>The Pressure line for Year: one point at noon of each day that has a daily mean.</summary>
    public PressureSeries PressureLine(IReadOnlyList<HourGridMonth> months)
    {
        ArgumentNullException.ThrowIfNull(months);

        var series = new List<TimedValue>();

        foreach (var month in months)
        {
            for (var index = 0; index < month.DaysShown && index < month.Days.Count; index++)
            {
                if (month.Days[index] is { } mean)
                {
                    series.Add(new TimedValue(mean, calendar.InstantAt(month.Month.AddDays(index), new TimeOnly(12, 0))));
                }
            }
        }

        return Series(series, TimeSpan.FromDays(1));
    }

    /// <summary>The shaded stretch of the Pressure line and its label ("+14.8 hPa in 24 h"). Null without a change.</summary>
    public ChartHighlight? ChangeHighlight(PressureChange? change)
    {
        if (change is null)
        {
            return null;
        }

        var sign = change.Delta < 0 ? "−" : "+";
        var label = localizer.Format(
            "History_Chart_PressureChange",
            ("value", $"{sign}{formatter.Difference(Metric.Pressure, change.Delta)}"),
            ("unit", formatter.Unit(Metric.Pressure)));

        return new ChartHighlight(change.Start, change.End, label);
    }

    private PressureSeries Series(List<TimedValue> series, TimeSpan interval)
    {
        var points = series.Select(value => new ChartPoint(value.Time, formatter.ToDisplay(Metric.Pressure, value.Value))).ToList();

        return new PressureSeries(points, PressureChangeCalculator.Biggest(series), interval);
    }

    /// <summary>The comfortable range of Temperature or Humidity in the unit shown, with its label ("comfort 20–24 °C"). Null for the other metrics.</summary>
    public ChartBand? ComfortBand(Metric metric)
    {
        if (ComfortScale.For(metric) is not { } scale)
        {
            return null;
        }

        var label = localizer.Format(
            "History_Chart_ComfortRange",
            ("from", formatter.Value(metric, scale.Low)),
            ("to", formatter.Value(metric, scale.High)),
            ("unit", formatter.Unit(metric)));

        return new ChartBand(formatter.ToDisplay(metric, scale.Low), formatter.ToDisplay(metric, scale.High), label);
    }

    /// <summary>
    /// The typical day laid over the window: one point in the middle of each local hour that falls inside it, at that hour of day's mean. For the latest Day, whose window starts mid-day,
    /// the line therefore starts at the hour the window starts in. Hours without readings leave a gap in the line.
    /// </summary>
    public IReadOnlyList<ChartPoint> TypicalLine(IReadOnlyList<HourOfDayAverage> typical, DateTimeOffset start, DateTimeOffset end, Metric metric = Metric.Co2)
    {
        ArgumentNullException.ThrowIfNull(typical);

        if (typical.Count == 0 || end <= start)
        {
            return [];
        }

        var localStart = TimeZoneInfo.ConvertTime(start, calendar.Zone).DateTime;
        var firstHour = localStart.Date.AddHours(localStart.Hour);
        var points = new List<ChartPoint>();

        for (var hour = firstHour; ; hour = hour.AddHours(1))
        {
            var slot = new DateTimeOffset(hour, calendar.Zone.GetUtcOffset(hour));
            var middle = slot.AddMinutes(30);

            if (middle > end)
            {
                break;
            }

            if (middle >= start)
            {
                var average = typical.FirstOrDefault(item => item.Hour == hour.Hour)?.Average;
                points.Add(new ChartPoint(middle, average is { } stored ? formatter.ToDisplay(metric, stored) : null));
            }
        }

        return points;
    }

    /// <summary>One marker per spaced event: a ring at the start of the event, with "Air improved at 14:16 · −934 ppm in 12 min" on hover.</summary>
    public IReadOnlyList<ChartMarker> Markers(IReadOnlyList<AirImprovedEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var unit = formatter.Unit(Metric.Co2);

        return [.. AirImprovedDetector.Markers(events).Select(item => new ChartMarker(
            item.Start,
            item.StartPpm,
            localizer.Format(
                "History_AirImproved_Hover",
                ("time", TimeZoneInfo.ConvertTime(item.Start, calendar.Zone).ToString("HH:mm", localizer.Culture)),
                ("drop", formatter.Value(Metric.Co2, item.DropPpm)),
                ("unit", unit),
                ("min", Math.Max(1, (int)Math.Round(item.Duration.TotalMinutes))))))];
    }

    private static DateTimeOffset Max(DateTimeOffset first, DateTimeOffset second) => first > second ? first : second;

    private static DateTimeOffset Min(DateTimeOffset first, DateTimeOffset second) => first < second ? first : second;
}
