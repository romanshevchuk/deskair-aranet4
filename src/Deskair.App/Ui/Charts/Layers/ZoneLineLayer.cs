using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// The CO₂ line, coloured by zone: a vertical gradient with hard stops at the zone limits (Zone.*.Line colours).
/// A null value breaks the line. The latest value gets a ring marker.
/// </summary>
public sealed class ZoneLineLayer : IChartLayer
{
    private const double Thickness = 2;
    private const double MarkerRadius = 4;

    public IReadOnlyList<ChartPoint>? Points { get; set; }

    public double ElevatedFrom { get; set; } = 1000.0;

    public double HighAbove { get; set; } = 1400.0;

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Points is not { Count: > 0 } points)
        {
            return;
        }

        var pen = ChartLines.RoundPen(GradientBrush(area), Thickness);

        context.PushClip(new RectangleGeometry(new Rect(area.Plot.Left - Thickness, area.Plot.Top - Thickness, area.Plot.Width + (2 * Thickness), area.Plot.Height + (2 * Thickness))));
        context.DrawGeometry(null, pen, ChartLines.Build(points, area));
        context.Pop();

        DrawMarker(context, area, points);
    }

    private LinearGradientBrush GradientBrush(ChartArea area)
    {
        var elevated = Offset(area, ElevatedFrom);
        var high = Offset(area, HighAbove);
        var good = ((SolidColorBrush)area.Brush("Zone.Good.Line.Brush")).Color;
        var amber = ((SolidColorBrush)area.Brush("Zone.Elevated.Line.Brush")).Color;
        var red = ((SolidColorBrush)area.Brush("Zone.High.Line.Brush")).Color;

        // The gradient runs from the bottom of the plot (offset 0) to the top (offset 1), in absolute pixels.
        return new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint = new Point(0, area.Plot.Bottom),
            EndPoint = new Point(0, area.Plot.Top),
            GradientStops =
            [
                new GradientStop(good, 0),
                new GradientStop(good, elevated),
                new GradientStop(amber, elevated),
                new GradientStop(amber, high),
                new GradientStop(red, high),
                new GradientStop(red, 1)
            ]
        };
    }

    private static double Offset(ChartArea area, double value)
        => Math.Clamp((value - area.YMin) / (area.YMax - area.YMin), 0, 1);

    private void DrawMarker(DrawingContext context, ChartArea area, IReadOnlyList<ChartPoint> points)
    {
        if (ChartLines.LastValue(points) is not { Value: { } value } last)
        {
            return;
        }

        var zone = ZoneClassifier.Classify(value, new ZoneThresholds(ElevatedFrom, HighAbove));
        var ring = new Pen(area.Brush($"Zone.{zone}.Line.Brush"), Thickness);

        context.DrawEllipse(area.Brush("Surface.Brush"), ring, new Point(area.X(last.Time), area.Y(value)), MarkerRadius, MarkerRadius);
    }
}
