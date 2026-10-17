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

    /// <summary>Draws the line as a monotone curve through the readings (the Live chart); off keeps straight segments.</summary>
    public bool Smooth { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Points is not { Count: > 0 } points)
        {
            return;
        }

        var pen = ChartLines.RoundPen(ChartLines.ZoneGradient(area, ElevatedFrom, HighAbove), Thickness);

        context.PushClip(new RectangleGeometry(new Rect(area.Plot.Left - Thickness, area.Plot.Top - Thickness, area.Plot.Width + (2 * Thickness), area.Plot.Height + (2 * Thickness))));
        context.DrawGeometry(null, pen, ChartLines.Build(points, area, Smooth));
        context.Pop();

        DrawMarker(context, area, points);
    }

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
