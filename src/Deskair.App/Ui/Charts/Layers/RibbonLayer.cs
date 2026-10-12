using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>The 24 h ribbon: a rounded track with a zone-coloured block for each segment. A time with no segment stays as bare track (a gap).</summary>
public sealed class RibbonLayer : IChartLayer
{
    private const double MaximumRadius = 6;

    public IReadOnlyList<ChartZoneSegment>? Segments { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        var radius = Math.Min(area.Plot.Height / 2, MaximumRadius);
        var track = new RectangleGeometry(area.Plot, radius, radius);

        context.DrawGeometry(area.Brush("Segment.Bg.Brush"), null, track);

        if (Segments is not { Count: > 0 } segments)
        {
            return;
        }

        context.PushClip(track);

        foreach (var segment in segments)
        {
            // Edges are snapped to device pixels so neighbouring blocks meet exactly, without a faint seam.
            var left = Math.Max(area.Plot.Left, Snap(area, area.X(segment.Start)));
            var right = Math.Min(area.Plot.Right, Snap(area, area.X(segment.End)));

            if (right > left)
            {
                context.DrawRectangle(area.Brush($"Zone.{segment.Zone}.Segment.Brush"), null, new Rect(left, area.Plot.Top, right - left, area.Plot.Height));
            }
        }

        context.Pop();
    }

    private static double Snap(ChartArea area, double coordinate)
        => Math.Round(coordinate * area.PixelsPerDip) / area.PixelsPerDip;
}
