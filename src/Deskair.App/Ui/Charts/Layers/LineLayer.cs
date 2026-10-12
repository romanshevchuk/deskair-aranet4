using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>A plain line in one brush (a metric colour). A null value breaks the line. Optionally marks the latest value with a ring.</summary>
public sealed class LineLayer : IChartLayer
{
    private const double MarkerRadius = 4;

    public IReadOnlyList<ChartPoint>? Points { get; set; }

    /// <summary>The resource key of the line brush, for example "Metric.Temperature.Brush".</summary>
    public string BrushKey { get; set; } = "Text.Primary.Brush";

    public double Thickness { get; set; } = 2;

    public bool ShowEndMarker { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Points is not { Count: > 0 } points)
        {
            return;
        }

        var brush = area.Brush(BrushKey);

        context.PushClip(new RectangleGeometry(new Rect(area.Plot.Left - Thickness, area.Plot.Top - Thickness, area.Plot.Width + (2 * Thickness), area.Plot.Height + (2 * Thickness))));
        context.DrawGeometry(null, ChartLines.RoundPen(brush, Thickness), ChartLines.Build(points, area));
        context.Pop();

        if (ShowEndMarker && ChartLines.LastValue(points) is { Value: { } value } last)
        {
            context.DrawEllipse(area.Brush("Surface.Brush"), new Pen(brush, Thickness), new Point(area.X(last.Time), area.Y(value)), MarkerRadius, MarkerRadius);
        }
    }
}
