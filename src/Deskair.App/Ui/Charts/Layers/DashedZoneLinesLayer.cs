using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>Dashed horizontal lines at the two zone limits (prototype: thin, neutral grey, the same for both). A limit outside the visible range is skipped.</summary>
public sealed class DashedZoneLinesLayer : IChartLayer
{
    private const string BrushKey = "Card.Border.Hover.Brush";

    public double ElevatedFrom { get; set; } = 1000.0;

    public double HighAbove { get; set; } = 1400.0;

    public void Draw(DrawingContext context, ChartArea area)
    {
        DrawLimit(context, area, ElevatedFrom);
        DrawLimit(context, area, HighAbove);
    }

    private static void DrawLimit(DrawingContext context, ChartArea area, double limit)
    {
        if (limit < area.YMin || limit > area.YMax)
        {
            return;
        }

        var pen = new Pen(area.Brush(BrushKey), 1 / area.PixelsPerDip) { DashStyle = new DashStyle([3, 3], 0) };
        var y = area.Snap(area.Y(limit));

        context.DrawLine(pen, new Point(area.Plot.Left, y), new Point(area.Plot.Right, y));
    }
}
