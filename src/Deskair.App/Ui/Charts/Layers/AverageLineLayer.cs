using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Aggregation;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// A short horizontal tick at the average of each bar (prototype): 3 px thick, rounded, 2 px wider than the bar on each side. With <see cref="ZoneColoured"/> it is the strong colour of the zone
/// of the average, otherwise <see cref="BrushKey"/>. Buckets with no average, or no data, get none.
/// </summary>
public sealed class AverageLineLayer : IChartLayer
{
    private const double Thickness = 3;
    private const double Overhang = 2;

    public IReadOnlyList<ChartBar>? Bars { get; set; }

    public string BrushKey { get; set; } = "Text.Primary.Brush";

    public bool ZoneColoured { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Bars is null)
        {
            return;
        }

        foreach (var bar in Bars)
        {
            if (bar.State == BucketState.NoData || bar.Average is not { } average)
            {
                continue;
            }

            var y = area.Y(average);

            if (y < area.Plot.Top || y > area.Plot.Bottom)
            {
                continue;
            }

            var (barLeft, barRight) = BarGeometry.Horizontal(area, bar);
            var tick = new Rect(barLeft - Overhang, y - (Thickness / 2), barRight - barLeft + (2 * Overhang), Thickness);
            tick.Intersect(area.Plot);

            if (!tick.IsEmpty)
            {
                context.DrawRoundedRectangle(Brush(area, bar), null, tick, Thickness / 2, Thickness / 2);
            }
        }
    }

    private Brush Brush(ChartArea area, ChartBar bar)
        => ZoneColoured && BarGeometry.ZoneOf(bar) is { } zone ? area.Brush($"Zone.{zone}.Line.Brush") : area.Brush(BrushKey);
}
