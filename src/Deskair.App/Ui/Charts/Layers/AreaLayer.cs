using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// A soft fill under a line (design-tokens.md "Live chart"): the colour of <see cref="BrushKey"/>, or the zone colours of the line, at <see cref="TopOpacity"/> at the line's
/// highest point, fading to nothing at the bottom of the plot. Runs of one point and gaps get no fill. Follows the same smoothing as the line above it.
/// </summary>
public sealed class AreaLayer : IChartLayer
{
    public IReadOnlyList<ChartPoint>? Points { get; set; }

    /// <summary>The resource key of the brush the fill takes, for example "Zone.Elevated.Line.Brush". Not used with <see cref="ZoneColoured"/>.</summary>
    public string BrushKey { get; set; } = "Text.Primary.Brush";

    public double TopOpacity { get; set; } = 0.22;

    public bool Smooth { get; set; }

    /// <summary>Fills with the zone colours of the line (<see cref="ChartLines.ZoneGradient"/>) instead of <see cref="BrushKey"/>, so the line and the fill always match.</summary>
    public bool ZoneColoured { get; set; }

    public double ElevatedFrom { get; set; } = 1000.0;

    public double HighAbove { get; set; } = 1400.0;

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Points is not { Count: > 1 } points)
        {
            return;
        }

        var runs = ChartLines.Runs(points, area);
        if (!runs.Any(run => run.Count > 1))
        {
            return;
        }

        // The fade is a mask, so the same code serves a solid colour and the zone gradient.
        var top = Math.Max(area.Plot.Top, runs.SelectMany(run => run).Min(point => point.Y));
        var fill = ZoneColoured ? ChartLines.ZoneGradient(area, ElevatedFrom, HighAbove) : area.Brush(BrushKey);
        var fade = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint = new Point(0, top),
            EndPoint = new Point(0, area.Plot.Bottom),
            GradientStops = [new GradientStop(Color.FromArgb((byte)Math.Round(TopOpacity * 255), 0, 0, 0), 0), new GradientStop(Color.FromArgb(0, 0, 0, 0), 1)]
        };
        fade.Freeze();

        context.PushClip(new RectangleGeometry(area.Plot));
        context.PushOpacityMask(fade);
        context.DrawGeometry(fill, null, ChartLines.BuildArea(points, area, Smooth));
        context.Pop();
        context.Pop();
    }
}
