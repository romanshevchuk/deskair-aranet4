using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>Dashed horizontal lines at the two zone limits, in the zone line colours. A limit outside the visible range is skipped.</summary>
public sealed class DashedZoneLinesLayer : IChartLayer
{
    private const double LineOpacity = 0.55;

    public double ElevatedFrom { get; set; } = 1000.0;

    public double HighAbove { get; set; } = 1400.0;

    public void Draw(DrawingContext context, ChartArea area)
    {
        context.PushOpacity(LineOpacity);
        DrawLimit(context, area, ElevatedFrom, "Zone.Elevated.Line.Brush");
        DrawLimit(context, area, HighAbove, "Zone.High.Line.Brush");
        context.Pop();
    }

    private static void DrawLimit(DrawingContext context, ChartArea area, double limit, string brushKey)
    {
        if (limit < area.YMin || limit > area.YMax)
        {
            return;
        }

        var pen = new Pen(area.Brush(brushKey), 1 / area.PixelsPerDip) { DashStyle = new DashStyle([4, 4], 0) };
        var y = area.Snap(area.Y(limit));

        context.DrawLine(pen, new Point(area.Plot.Left, y), new Point(area.Plot.Right, y));
    }
}
