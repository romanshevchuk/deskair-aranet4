using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts;

/// <summary>Geometry shared by the line layers. A point without a value breaks the line, so a gap is never bridged.</summary>
internal static class ChartLines
{
    /// <summary>One figure per run of consecutive values. A run of one point is a zero-length segment, which a round cap draws as a dot.</summary>
    public static StreamGeometry Build(IReadOnlyList<ChartPoint> points, ChartArea area)
    {
        var geometry = new StreamGeometry();

        using (var context = geometry.Open())
        {
            var open = false;

            foreach (var point in points)
            {
                if (point.Value is not { } value)
                {
                    open = false;
                    continue;
                }

                var position = new Point(area.X(point.Time), area.Y(value));

                if (open)
                {
                    context.LineTo(position, isStroked: true, isSmoothJoin: false);
                    continue;
                }

                context.BeginFigure(position, isFilled: false, isClosed: false);
                context.LineTo(position, isStroked: true, isSmoothJoin: false);
                open = true;
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
}
