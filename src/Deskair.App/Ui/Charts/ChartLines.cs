using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts;

/// <summary>Geometry shared by the line layers. A point without a value breaks the line, so a gap is never bridged.</summary>
internal static class ChartLines
{
    /// <summary>One figure per run of consecutive values. A run of one point is a zero-length segment, which a round cap draws as a dot.</summary>
    public static StreamGeometry Build(IReadOnlyList<ChartPoint> points, ChartArea area) => Build(points, area, smooth: false);

    /// <summary>
    /// As <see cref="Build(IReadOnlyList{ChartPoint}, ChartArea)"/>, with each run drawn as a monotone cubic curve when <paramref name="smooth"/> is set:
    /// the curve passes through every point and never overshoots it, so a peak on the chart is a real reading (Fritsch–Carlson tangents).
    /// </summary>
    public static StreamGeometry Build(IReadOnlyList<ChartPoint> points, ChartArea area, bool smooth)
    {
        var geometry = new StreamGeometry();

        using (var context = geometry.Open())
        {
            foreach (var run in Runs(points, area))
            {
                context.BeginFigure(run[0], isFilled: false, isClosed: false);
                AddRun(context, run, smooth);
            }
        }

        geometry.Freeze();

        return geometry;
    }

    /// <summary>The area under each run of two or more values, down to the bottom of the plot, following the same line as <see cref="Build(IReadOnlyList{ChartPoint}, ChartArea, bool)"/>.</summary>
    public static StreamGeometry BuildArea(IReadOnlyList<ChartPoint> points, ChartArea area, bool smooth)
    {
        var geometry = new StreamGeometry();

        using (var context = geometry.Open())
        {
            foreach (var run in Runs(points, area).Where(run => run.Count > 1))
            {
                context.BeginFigure(new Point(run[0].X, area.Plot.Bottom), isFilled: true, isClosed: true);
                context.LineTo(run[0], isStroked: false, isSmoothJoin: false);
                AddRun(context, run, smooth);
                context.LineTo(new Point(run[^1].X, area.Plot.Bottom), isStroked: false, isSmoothJoin: false);
            }
        }

        geometry.Freeze();

        return geometry;
    }

    public static Pen RoundPen(Brush brush, double thickness)
    {
        return new Pen(brush, thickness)
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
    }

    public static ChartPoint? LastValue(IReadOnlyList<ChartPoint> points)
    {
        for (var index = points.Count - 1; index >= 0; index--)
        {
            if (points[index].Value is not null)
            {
                return points[index];
            }
        }

        return null;
    }

    /// <summary>The runs of consecutive values, in pixels.</summary>
    internal static List<List<Point>> Runs(IReadOnlyList<ChartPoint> points, ChartArea area)
    {
        var runs = new List<List<Point>>();
        List<Point>? current = null;

        foreach (var point in points)
        {
            if (point.Value is not { } value)
            {
                current = null;
                continue;
            }

            if (current is null)
            {
                current = [];
                runs.Add(current);
            }

            current.Add(new Point(area.X(point.Time), area.Y(value)));
        }

        return runs;
    }

    /// <summary>Continues a figure that starts at the run's first point.</summary>
    private static void AddRun(StreamGeometryContext context, List<Point> run, bool smooth)
    {
        if (run.Count == 1 || !smooth || run.Count == 2)
        {
            foreach (var point in run.Count == 1 ? run : run.Skip(1))
            {
                context.LineTo(point, isStroked: true, isSmoothJoin: false);
            }

            return;
        }

        var tangents = MonotoneTangents(run);

        for (var index = 0; index < run.Count - 1; index++)
        {
            var from = run[index];
            var to = run[index + 1];
            var third = (to.X - from.X) / 3;

            context.BezierTo(
                new Point(from.X + third, from.Y + (tangents[index] * third)),
                new Point(to.X - third, to.Y - (tangents[index + 1] * third)),
                to,
                isStroked: true,
                isSmoothJoin: true);
        }
    }

    /// <summary>Fritsch–Carlson tangents (dy/dx) for a monotone cubic through the points; x must not decrease.</summary>
    internal static double[] MonotoneTangents(IReadOnlyList<Point> run)
    {
        var count = run.Count;
        var slopes = new double[count - 1];

        for (var index = 0; index < count - 1; index++)
        {
            var dx = run[index + 1].X - run[index].X;
            slopes[index] = dx > 0 ? (run[index + 1].Y - run[index].Y) / dx : 0;
        }

        var tangents = new double[count];
        tangents[0] = slopes[0];
        tangents[count - 1] = slopes[count - 2];

        for (var index = 1; index < count - 1; index++)
        {
            // A change of direction gets a flat tangent, so the curve turns at the reading instead of swinging past it.
            tangents[index] = slopes[index - 1] * slopes[index] <= 0 ? 0 : (slopes[index - 1] + slopes[index]) / 2;
        }

        for (var index = 0; index < count - 1; index++)
        {
            if (slopes[index] == 0)
            {
                tangents[index] = 0;
                tangents[index + 1] = 0;
                continue;
            }

            var a = tangents[index] / slopes[index];
            var b = tangents[index + 1] / slopes[index];
            var length = (a * a) + (b * b);

            if (length > 9)
            {
                var scale = 3 / Math.Sqrt(length);
                tangents[index] = scale * a * slopes[index];
                tangents[index + 1] = scale * b * slopes[index];
            }
        }

        return tangents;
    }

    /// <summary>
    /// The colour of a COâ‚‚ value at each height of the plot: green below <paramref name="elevatedFrom"/>, amber up to <paramref name="highAbove"/>, red above (the Zone.*.Line colours),
    /// with hard stops at the limits. The line and the fill under it both use it, so they always agree.
    /// </summary>
    public static LinearGradientBrush ZoneGradient(ChartArea area, double elevatedFrom, double highAbove)
    {
        var elevated = Offset(area, elevatedFrom);
        var high = Offset(area, highAbove);
        var good = ((SolidColorBrush)area.Brush("Zone.Good.Line.Brush")).Color;
        var amber = ((SolidColorBrush)area.Brush("Zone.Elevated.Line.Brush")).Color;
        var red = ((SolidColorBrush)area.Brush("Zone.High.Line.Brush")).Color;

        // The gradient runs from the bottom of the plot (offset 0) to the top (offset 1), in absolute pixels.
        var brush = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint = new Point(0, area.Plot.Bottom),
            EndPoint = new Point(0, area.Plot.Top),
            GradientStops = [new GradientStop(good, 0), new GradientStop(good, elevated), new GradientStop(amber, elevated), new GradientStop(amber, high), new GradientStop(red, high), new GradientStop(red, 1)]
        };
        brush.Freeze();

        return brush;
    }

    private static double Offset(ChartArea area, double value) => Math.Clamp((value - area.YMin) / (area.YMax - area.YMin), 0, 1);
}
