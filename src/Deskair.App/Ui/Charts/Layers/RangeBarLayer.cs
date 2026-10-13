using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Aggregation;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// Floating bars from each bucket's lowest to its highest value, in the metric colour. A complete bucket is solid; a partial one (under 80 % of its readings)
/// is lighter with a dashed outline; a bucket with no data draws nothing here (the gap layer hatches it). A bar never gets thinner than 2 px.
/// </summary>
public sealed class RangeBarLayer : IChartLayer
{
    private const double SideGap = 2;
    private const double MinimumHeight = 2;
    private const double PartialOpacity = 0.35;

    public IReadOnlyList<ChartBar>? Bars { get; set; }

    public string BrushKey { get; set; } = "Metric.Co2.Brush";

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Bars is null)
        {
            return;
        }

        var brush = area.Brush(BrushKey);
        var dash = new Pen(brush, 1.2) { DashStyle = new DashStyle([3, 2], 0) };

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

            if (bar.State == BucketState.Partial)
            {
                context.PushOpacity(PartialOpacity);
                context.DrawRoundedRectangle(brush, null, rect, 2, 2);
                context.Pop();
                context.DrawRoundedRectangle(null, dash, rect, 2, 2);
            }
            else
            {
                context.DrawRoundedRectangle(brush, null, rect, 2, 2);
            }
        }
    }

    /// <summary>The bar's rectangle inside the plot, or <see cref="Rect.Empty"/> when it is wholly outside.</summary>
    private static Rect BarRect(ChartArea area, ChartBar bar, double minimum, double maximum)
    {
        var left = area.X(bar.Start) + SideGap;
        var right = area.X(bar.End) - SideGap;

        if (right - left < 1)
        {
            var middle = (left + right) / 2;
            left = middle - 0.5;
            right = middle + 0.5;
        }

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
