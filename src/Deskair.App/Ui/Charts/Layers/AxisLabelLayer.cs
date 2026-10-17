using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Deskair.App.Localization;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// Value labels on the y axis and time labels on the x axis, in Text.Tertiary. Numbers follow <see cref="Culture"/> (1,068 versus 1 068),
/// times are 24 h in the time zone <see cref="TimeZone"/>. Labels that would overlap are skipped.
/// </summary>
public sealed class AxisLabelLayer : IChartLayer
{
    private static readonly int[] stepMinutes = [5, 10, 15, 30, 60, 120, 180, 240, 360, 720, 1440];
    private static readonly int[] dayStepCounts = [1, 2, 7, 14, 30, 61, 122];

    private const double LabelGap = 8;
    private const double MinimumLabelSpacing = 64;

    public CultureInfo? Culture { get; set; }

    public int YTickCount { get; set; } = 4;

    public IReadOnlyList<double>? YTicks { get; set; }

    /// <summary>The number format of the y labels: "0" for whole numbers, "0.0" for one decimal, never a group separator.</summary>
    public string YFormat { get; set; } = NumberFormat.WholeFormat;

    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    /// <summary>The label at the right end of the time axis ("Now"). Null shows none.</summary>
    public string? NowLabel { get; set; }

    /// <summary>Label the time axis with dates ("12 Oct") at local midnights, for charts that span days; otherwise with clock times.</summary>
    public bool DateLabels { get; set; }

    /// <summary>Y labels drawn bold in another brush, by value: the CO₂ limits in the colour of their zone. The key is a resource key of a solid brush.</summary>
    public IReadOnlyDictionary<double, string>? TickBrushKeys { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        var culture = Culture ?? CultureInfo.CurrentUICulture;
        var brush = area.Brush("Text.Tertiary.Brush");

        DrawYLabels(context, area, brush, culture);
        DrawTimeLabels(context, area, brush, culture);
    }

    private void DrawYLabels(DrawingContext context, ChartArea area, Brush brush, CultureInfo culture)
    {
        var ticks = YTicks ?? ChartTicks.Even(area.YMin, area.YMax, YTickCount);
        var drawn = new List<Rect>();

        foreach (var tick in ticks.Where(tick => tick >= area.YMin && tick <= area.YMax))
        {
            var text = TickBrushKeys is not null && TickBrushKeys.TryGetValue(tick, out var key)
                ? area.Text(tick.ToString(YFormat, culture), area.Brush(key), bold: true)
                : area.Text(tick.ToString(YFormat, culture), brush);
            var bounds = new Rect(area.Plot.Left - LabelGap - text.Width, area.Y(tick) - (text.Height / 2), text.Width, text.Height);

            if (drawn.Any(other => other.IntersectsWith(bounds)))
            {
                continue;
            }

            context.DrawText(text, bounds.TopLeft);
            drawn.Add(bounds);
        }
    }

    private void DrawTimeLabels(DrawingContext context, ChartArea area, Brush brush, CultureInfo culture)
    {
        var drawn = new List<Rect>();
        var top = area.Plot.Bottom + LabelGap;

        if (NowLabel is not null)
        {
            var now = area.Text(NowLabel, area.Brush("Text.Primary.Brush"), bold: true);
            var bounds = new Rect(area.Plot.Right - (now.Width / 2), top, now.Width, now.Height);

            context.DrawText(now, bounds.TopLeft);
            drawn.Add(bounds);
        }

        foreach (var time in TimeTicks(area))
        {
            var text = area.Text(DateLabels ? time.ToString("d MMM", culture) : time.ToString("HH:mm", CultureInfo.InvariantCulture), brush);
            var bounds = new Rect(area.X(time) - (text.Width / 2), top, text.Width, text.Height);
            var padded = new Rect(bounds.X - (LabelGap / 2), bounds.Y, bounds.Width + LabelGap, bounds.Height);

            if (bounds.Left < 0 || drawn.Any(other => other.IntersectsWith(padded)))
            {
                continue;
            }

            context.DrawText(text, bounds.TopLeft);
            drawn.Add(bounds);
        }
    }

    /// <summary>Round local times (clock times, or local midnights for date labels) inside the visible range, with a step that leaves room for the labels.</summary>
    public IEnumerable<DateTimeOffset> TimeTicks(ChartArea area)
        => DateLabels ? DayTicks(area) : ClockTicks(area);

    private IEnumerable<DateTimeOffset> DayTicks(ChartArea area)
    {
        var start = new DateTimeOffset((long)area.XMin, TimeSpan.Zero);
        var end = new DateTimeOffset((long)area.XMax, TimeSpan.Zero);
        var maxLabels = Math.Max(1, (int)(area.Plot.Width / (MinimumLabelSpacing + 12)));
        var days = Math.Max(1, (end - start).TotalDays);
        var step = dayStepCounts.FirstOrDefault(count => days / count <= maxLabels);
        var stepDays = step == 0 ? dayStepCounts[^1] : step;
        var next = TimeZoneInfo.ConvertTime(start, TimeZone).Date;

        if (next < TimeZoneInfo.ConvertTime(start, TimeZone).DateTime)
        {
            next = next.AddDays(1);
        }

        for (; ; next = next.AddDays(stepDays))
        {
            var time = new DateTimeOffset(next, TimeZone.GetUtcOffset(next));

            if (time > end)
            {
                yield break;
            }

            yield return time;
        }
    }

    private IEnumerable<DateTimeOffset> ClockTicks(ChartArea area)
    {
        var start = new DateTimeOffset((long)area.XMin, TimeSpan.Zero);
        var end = new DateTimeOffset((long)area.XMax, TimeSpan.Zero);
        var maxLabels = Math.Max(1, (int)(area.Plot.Width / MinimumLabelSpacing));
        var span = end - start;
        var step = stepMinutes.FirstOrDefault(minutes => span.TotalMinutes / minutes <= maxLabels);
        var stepSize = TimeSpan.FromMinutes(step == 0 ? stepMinutes[^1] : step);
        var local = TimeZoneInfo.ConvertTime(start, TimeZone).DateTime;
        var next = local.Date + TimeSpan.FromTicks((long)Math.Ceiling((double)local.TimeOfDay.Ticks / stepSize.Ticks) * stepSize.Ticks);

        for (; ; next += stepSize)
        {
            var time = new DateTimeOffset(next, TimeZone.GetUtcOffset(next));

            if (time > end)
            {
                yield break;
            }

            yield return time;
        }
    }
}
