using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// A hatched band for each stretch of time with no data, drawn with a tiled brush of 45° lines in the Text.Tertiary colour at low opacity.
/// Neighbouring and overlapping gaps are merged into one band. A band narrower than a tile is drawn as a solid, lighter strip instead.
/// </summary>
public sealed class GapHatchLayer : IChartLayer
{
    private const double Tile = 8;
    private const double LineOpacity = 0.4;
    private const double SolidOpacity = 0.25;

    public IReadOnlyList<ChartGap>? Gaps { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Gaps is not { Count: > 0 })
        {
            return;
        }

        var color = area.Brush("Text.Tertiary.Brush") is SolidColorBrush solid ? solid.Color : Colors.Gray;
        var hatch = HatchBrush(color);
        var strip = new SolidColorBrush(color) { Opacity = SolidOpacity };

        foreach (var gap in Merge(Gaps))
        {
            if (gap.End.UtcTicks <= area.XMin || gap.Start.UtcTicks >= area.XMax)
            {
                continue;
            }

            var left = Math.Max(area.X(gap.Start), area.Plot.Left);
            var right = Math.Min(area.X(gap.End), area.Plot.Right);

            if (right <= left)
            {
                continue;
            }

            var rect = new Rect(left, area.Plot.Top, right - left, area.Plot.Height);
            context.DrawRectangle(rect.Width < Tile ? strip : hatch, null, rect);
        }
    }

    /// <summary>Sorts the gaps and joins those that touch or overlap.</summary>
    public static IReadOnlyList<ChartGap> Merge(IReadOnlyList<ChartGap> gaps)
    {
        var merged = new List<ChartGap>();

        foreach (var gap in gaps.OrderBy(candidate => candidate.Start))
        {
            if (merged.Count > 0 && gap.Start <= merged[^1].End)
            {
                if (gap.End > merged[^1].End)
                {
                    merged[^1] = merged[^1] with { End = gap.End };
                }
            }
            else
            {
                merged.Add(gap);
            }
        }

        return merged;
    }

    private static DrawingBrush HatchBrush(Color color)
    {
        var pen = new Pen(new SolidColorBrush(color) { Opacity = LineOpacity }, 1);
        pen.Freeze();

        // One diagonal per tile, plus the corner pieces that make the lines continue across tile edges.
        var lines = new GeometryGroup();
        lines.Children.Add(new LineGeometry(new Point(0, Tile), new Point(Tile, 0)));
        lines.Children.Add(new LineGeometry(new Point(-1, 1), new Point(1, -1)));
        lines.Children.Add(new LineGeometry(new Point(Tile - 1, Tile + 1), new Point(Tile + 1, Tile - 1)));

        var brush = new DrawingBrush(new GeometryDrawing(null, pen, lines))
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, Tile, Tile),
            ViewportUnits = BrushMappingMode.Absolute,
            Viewbox = new Rect(0, 0, Tile, Tile),
            ViewboxUnits = BrushMappingMode.Absolute
        };
        brush.Freeze();

        return brush;
    }
}
