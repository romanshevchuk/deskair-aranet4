using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// A soft fill under a line (design-tokens.md "Live chart"): the colour of <see cref="BrushKey"/> at <see cref="TopOpacity"/> at the line's highest point,
/// fading to nothing at the bottom of the plot. Runs of one point and gaps get no fill. Follows the same smoothing as the line above it.
/// </summary>
public sealed class AreaLayer : IChartLayer
{
    public IReadOnlyList<ChartPoint>? Points { get; set; }

    /// <summary>The resource key of a solid brush whose colour the fill takes, for example "Zone.Elevated.Line.Brush".</summary>
    public string BrushKey { get; set; } = "Text.Primary.Brush";

    public double TopOpacity { get; set; } = 0.22;

    public bool Smooth { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Points is not { Count: > 1 } points || area.Brush(BrushKey) is not SolidColorBrush solid)
        {
            return;
        }

        var runs = ChartLines.Runs(points, area);
        if (!runs.Any(run => run.Count > 1))
        {
            return;
        }

        var top = Math.Max(area.Plot.Top, runs.SelectMany(run => run).Min(point => point.Y));
        var color = solid.Color;
        var fill = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint = new Point(0, top),
            EndPoint = new Point(0, area.Plot.Bottom),
            GradientStops =
            [
                new GradientStop(Color.FromArgb((byte)Math.Round(TopOpacity * 255), color.R, color.G, color.B), 0),
                new GradientStop(Color.FromArgb(0, color.R, color.G, color.B), 1)
            ]
        };
        fill.Freeze();

        context.PushClip(new RectangleGeometry(area.Plot));
        context.DrawGeometry(fill, null, ChartLines.BuildArea(points, area, Smooth));
        context.Pop();
    }
}
