using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Aggregation;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// Floating bars from each bucket's lowest to its highest value (prototype): soft pills at 40 % of their colour, narrower than the slot. With <see cref="ZoneColoured"/> (CO₂) the colour
/// is the zone of the bucket's average; otherwise the metric colour. A partial bucket (under 80 % of its readings) is fainter with a dashed outline in the strong colour; a bucket with
/// no data draws nothing here (the gap layer hatches it). A bar never gets thinner than 2 px.
/// </summary>
public sealed class RangeBarLayer : IChartLayer
{
    private const double MinimumHeight = 2;
    private const double CompleteOpacity = 0.4;
    private const double PartialOpacity = 0.18;
    private const double MaximumRadius = 6;

    public IReadOnlyList<ChartBar>? Bars { get; set; }

    public string BrushKey { get; set; } = "Metric.Co2.Brush";

    /// <summary>Colour each bar by the CO₂ zone of its average (soft fill, strong dashed outline) instead of one metric colour.</summary>
    public bool ZoneColoured { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Bars is null)
        {
            return;
        }

        foreach (var bar in Bars)
        {
            if (bar.State == BucketState.NoData || bar.Minimum is not { } minimum || bar.Maximum is not { } maximum)
            {
                continue;
            }

            var rect = BarRect(area, bar, minimum, maximum);

            if (rect.IsEmpty)
            {
                continue;
            }

            var (fill, strong) = Brushes(area, bar);
            var radius = Math.Min(rect.Width / 2, MaximumRadius);

            if (bar.State == BucketState.Partial)
            {
                context.PushOpacity(PartialOpacity);
                context.DrawRoundedRectangle(fill, null, rect, radius, radius);
                context.Pop();
                context.DrawRoundedRectangle(null, new Pen(strong, 1.2) { DashStyle = new DashStyle([3, 3], 0) }, rect, radius, radius);
            }
            else
            {
                context.PushOpacity(CompleteOpacity);
                context.DrawRoundedRectangle(fill, null, rect, radius, radius);
                context.Pop();
            }
        }
    }

    private (Brush Fill, Brush Strong) Brushes(ChartArea area, ChartBar bar)
    {
        if (ZoneColoured && BarGeometry.ZoneOf(bar) is { } zone)
        {
            return (area.Brush($"Zone.{zone}.Segment.Brush"), area.Brush($"Zone.{zone}.Line.Brush"));
        }

        var brush = area.Brush(BrushKey);

        return (brush, brush);
    }

    /// <summary>The bar's rectangle inside the plot, or <see cref="Rect.Empty"/> when it is wholly outside.</summary>
    private static Rect BarRect(ChartArea area, ChartBar bar, double minimum, double maximum)
    {
        var (left, right) = BarGeometry.Horizontal(area, bar);
        var top = area.Y(maximum);
        var bottom = area.Y(minimum);

        if (bottom - top < MinimumHeight)
        {
            var middle = (top + bottom) / 2;
            top = middle - (MinimumHeight / 2);
            bottom = middle + (MinimumHeight / 2);
        }

        var rect = new Rect(left, top, right - left, bottom - top);
        rect.Intersect(area.Plot);

        return rect;
    }
}
