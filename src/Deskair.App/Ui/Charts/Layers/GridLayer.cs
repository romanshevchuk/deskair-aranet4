using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>Horizontal gridlines (1 px, pixel aligned) at <see cref="YTicks"/>, or at <see cref="YTickCount"/> evenly spaced values when no ticks are given.</summary>
public sealed class GridLayer : IChartLayer
{
    public int YTickCount { get; set; } = 4;

    public IReadOnlyList<double>? YTicks { get; set; }

    /// <summary>Draw the lines dashed (3 and 3), as the typical-day chart does for the zone limits.</summary>
    public bool Dashed { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        var ticks = YTicks ?? ChartTicks.Even(area.YMin, area.YMax, YTickCount);
        var pen = new Pen(area.Brush("Grid.Brush"), 1 / area.PixelsPerDip);

        if (Dashed)
        {
            pen.DashStyle = new DashStyle([3, 3], 0);
        }

        foreach (var tick in ticks.Where(tick => tick >= area.YMin && tick <= area.YMax))
        {
            var y = area.Snap(area.Y(tick));

            context.DrawLine(pen, new Point(area.Plot.Left, y), new Point(area.Plot.Right, y));
        }
    }
}
