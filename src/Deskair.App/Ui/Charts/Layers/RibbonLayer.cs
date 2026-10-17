using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>The 24 h ribbon: a rounded track with a zone-coloured block for each segment. A time with no segment stays as bare track (a gap).</summary>
public sealed class RibbonLayer : IChartLayer
{
    private const double MaximumRadius = 6;

    public IReadOnlyList<ChartZoneSegment>? Segments { get; set; }

    /// <summary>The space between neighbouring segments. 0 keeps them flush inside the rounded track; above 0 each segment is its own rounded block (the flyout ribbon: 2 px apart).</summary>
    public double SegmentGap { get; set; }

    /// <summary>The corner radius of a separate segment, used when <see cref="SegmentGap"/> is above 0.</summary>
    public double SegmentRadius { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (SegmentGap > 0)
        {
            DrawSeparate(context, area);

            return;
        }

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

    /// <summary>Each segment as its own rounded block, half a gap in from a neighbour and flush with the ends of the plot.</summary>
    private void DrawSeparate(DrawingContext context, ChartArea area)
    {
        if (Segments is not { Count: > 0 } segments)
        {
            return;
        }

        foreach (var segment in MergeRuns(segments))
        {
            var start = area.X(segment.Start);
            var end = area.X(segment.End);
            var left = Math.Max(area.Plot.Left, Snap(area, start <= area.Plot.Left ? area.Plot.Left : start + (SegmentGap / 2)));
            var right = Math.Min(area.Plot.Right, Snap(area, end >= area.Plot.Right ? area.Plot.Right : end - (SegmentGap / 2)));

            if (right > left)
            {
                context.DrawRoundedRectangle(area.Brush($"Zone.{segment.Zone}.Segment.Brush"), null, new Rect(left, area.Plot.Top, right - left, area.Plot.Height), SegmentRadius, SegmentRadius);
            }
        }
    }

    /// <summary>Joins neighbouring segments of the same zone into one run, so a zone that lasted an hour is one block and not one block per reading.</summary>
    private static List<ChartZoneSegment> MergeRuns(IReadOnlyList<ChartZoneSegment> segments)
    {
        var runs = new List<ChartZoneSegment>();

        foreach (var segment in segments.OrderBy(item => item.Start))
        {
            if (runs.Count > 0 && runs[^1].Zone == segment.Zone && segment.Start <= runs[^1].End)
            {
                runs[^1] = runs[^1] with { End = DateTimeOffset.Compare(runs[^1].End, segment.End) >= 0 ? runs[^1].End : segment.End };
            }
            else
            {
                runs.Add(segment);
            }
        }

        return runs;
    }

    private static double Snap(ChartArea area, double coordinate)
        => Math.Round(coordinate * area.PixelsPerDip) / area.PixelsPerDip;
}
