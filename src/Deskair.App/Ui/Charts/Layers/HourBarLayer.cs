using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// The 24 bars of the typical day, one per hour of the day (x from 0 to 24), standing on the bottom of the plot (prototype): soft pills in the zone colour of the average, full colour for the
/// worst three hours, which also get a dashed rounded outline with <see cref="WorstText"/> ("worst 15–18h") above it. An hour with no average draws no bar.
/// </summary>
public sealed class HourBarLayer : IChartLayer
{
    private const double SideGap = 3;
    private const double SoftOpacity = 0.55;
    private const double OutlineOpacity = 0.5;
    private const double OutlineRise = 6;
    private const double LabelGap = 4;

    public IReadOnlyList<HourBar>? Hours { get; set; }

    /// <summary>The text above the worst window ("worst 15–18h"), already localized. Null or empty draws none.</summary>
    public string? WorstText { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Hours is null)
        {
            return;
        }

        foreach (var hour in Hours.Where(hour => hour.Average is not null && hour.Zone is not null))
        {
            var top = Math.Max(area.Y(hour.Average!.Value), area.Plot.Top);
            var width = Math.Max(1, area.X(hour.Hour + 1) - area.X(hour.Hour) - (2 * SideGap));
            var rect = new Rect(area.X(hour.Hour) + SideGap, top, width, Math.Max(0, area.Plot.Bottom - top));

            if (rect.Height > 0)
            {
                var radius = Math.Min(3, rect.Height / 2);

                context.PushOpacity(hour.InWorstWindow ? 1 : SoftOpacity);
                context.DrawRoundedRectangle(area.Brush(SegmentKey(hour.Zone!.Value)), null, rect, radius, radius);
                context.Pop();
            }
        }

        DrawWorstWindow(context, area);
    }

    private static List<(int First, int Last)> Runs(List<int> hours)
    {
        var runs = new List<(int First, int Last)>();

        foreach (var hour in hours)
        {
            if (runs.Count > 0 && runs[^1].Last + 1 == hour)
            {
                runs[^1] = (runs[^1].First, hour);
            }
            else
            {
                runs.Add((hour, hour));
            }
        }

        return runs;
    }

    private static string SegmentKey(Zone zone) => zone switch
    {
        Zone.Good => "Zone.Good.Segment.Brush",
        Zone.Elevated => "Zone.Elevated.Segment.Brush",
        _ => "Zone.High.Segment.Brush"
    };

    private void DrawWorstWindow(DrawingContext context, ChartArea area)
    {
        var ink = area.Brush("Text.Primary.Brush");
        var pen = new Pen(ink, 1) { DashStyle = new DashStyle([3, 3], 0) };
        var labelled = false;

        // A window is one or more neighbouring hours; one that crosses midnight is two runs, so each is outlined on its own and the first carries the text.
        foreach (var (first, last) in Runs(Hours!.Where(hour => hour.InWorstWindow).Select(hour => hour.Hour).Order().ToList()))
        {
            var rect = new Rect(area.X(first) + 0.5, area.Plot.Top - OutlineRise, area.X(last + 1) - area.X(first) - 1, area.Plot.Height + OutlineRise);

            context.PushOpacity(OutlineOpacity);
            context.DrawRoundedRectangle(null, pen, rect, 6, 6);
            context.Pop();

            if (!labelled && !string.IsNullOrEmpty(WorstText))
            {
                var text = area.Text(WorstText, ink);

                context.DrawText(text, new Point(rect.Left + ((rect.Width - text.Width) / 2), rect.Top - LabelGap - text.Height));
                labelled = true;
            }
        }
    }
}
