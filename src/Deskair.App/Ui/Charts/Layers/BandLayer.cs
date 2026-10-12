using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>Three horizontal zone bands behind the CO₂ line, in the Chart.Band.* brushes. Only the visible part of each band is drawn.</summary>
public sealed class BandLayer : IChartLayer
{
    public double ElevatedFrom { get; set; } = 1000.0;

    public double HighAbove { get; set; } = 1400.0;

    public void Draw(DrawingContext context, ChartArea area)
    {
        Fill(context, area, area.YMin, ElevatedFrom, "Chart.Band.Good.Brush");
        Fill(context, area, ElevatedFrom, HighAbove, "Chart.Band.Elevated.Brush");
        Fill(context, area, HighAbove, area.YMax, "Chart.Band.High.Brush");
    }

    private static void Fill(DrawingContext context, ChartArea area, double from, double to, string brushKey)
    {
        var low = Math.Max(from, area.YMin);
        var high = Math.Min(to, area.YMax);

        if (high <= low)
        {
            return;
        }

        var rect = new Rect(area.Plot.Left, area.Y(high), area.Plot.Width, area.Y(low) - area.Y(high));

        context.DrawRectangle(area.Brush(brushKey), null, rect);
    }
}
