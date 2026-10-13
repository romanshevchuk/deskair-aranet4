using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Aggregation;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>A short horizontal tick at the average of each bar. Buckets with no average, or no data, get none.</summary>
public sealed class AverageLineLayer : IChartLayer
{
    private const double SideGap = 3;
    private const double Thickness = 2;

    public IReadOnlyList<ChartBar>? Bars { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Bars is null)
        {
            return;
        }

        var pen = new Pen(area.Brush("Text.Primary.Brush"), Thickness) { StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat };

        foreach (var bar in Bars)
        {
            if (bar.State == BucketState.NoData || bar.Average is not { } average)
            {
                continue;
            }

            var y = area.Y(average);
            var left = area.X(bar.Start) + SideGap;
            var right = area.X(bar.End) - SideGap;

            if (right - left < 2)
            {
                var middle = (left + right) / 2;
                left = middle - 1;
                right = middle + 1;
            }

            if (y < area.Plot.Top || y > area.Plot.Bottom)
            {
                continue;
            }

            context.DrawLine(pen, new Point(Math.Max(left, area.Plot.Left), y), new Point(Math.Min(right, area.Plot.Right), y));
        }
    }
}
