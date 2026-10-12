using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// Value labels on the y axis and time labels on the x axis, in Text.Tertiary. Numbers follow <see cref="Culture"/> (1,068 versus 1 068),
/// times are 24 h in the time zone <see cref="TimeZone"/>. Labels that would overlap are skipped.
/// </summary>
public sealed class AxisLabelLayer : IChartLayer
{
    private static readonly int[] stepMinutes = [5, 10, 15, 30, 60, 120, 180, 240, 360, 720, 1440];

    private const double LabelGap = 8;
    private const double MinimumLabelSpacing = 64;

    public CultureInfo? Culture { get; set; }

    public int YTickCount { get; set; } = 4;

    public IReadOnlyList<double>? YTicks { get; set; }

    /// <summary>The number format of the y labels: "N0" for whole numbers, "N1" for one decimal.</summary>
    public string YFormat { get; set; } = "N0";

    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    /// <summary>The label at the right end of the time axis ("Now"). Null shows none.</summary>
    public string? NowLabel { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        var culture = Culture ?? CultureInfo.CurrentUICulture;
        var brush = area.Brush("Text.Tertiary.Brush");

        DrawYLabels(context, area, brush, culture);
        DrawTimeLabels(context, area, brush);
    }

    private void DrawYLabels(DrawingContext context, ChartArea area, Brush brush, CultureInfo culture)
    {
        var ticks = YTicks ?? ChartTicks.Even(area.YMin, area.YMax, YTickCount);
        var drawn = new List<Rect>();

        foreach (var tick in ticks.Where(tick => tick >= area.YMin && tick <= area.YMax))
        {
            var text = area.Text(tick.ToString(YFormat, culture), brush);
            var bounds = new Rect(area.Plot.Left - LabelGap - text.Width, area.Y(tick) - (text.Height / 2), text.Width, text.Height);

            if (drawn.Any(other => other.IntersectsWith(bounds)))
            {
                continue;
            }

            context.DrawText(text, bounds.TopLeft);
            drawn.Add(bounds);
        }
    }

    private void DrawTimeLabels(DrawingContext context, ChartArea area, Brush brush)
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
            var text = area.Text(time.ToString("HH:mm", CultureInfo.InvariantCulture), brush);
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

    /// <summary>Round local times (on the hour, half hour and so on) inside the visible range, with a step that leaves room for the labels.</summary>
    public IEnumerable<DateTimeOffset> TimeTicks(ChartArea area)
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
