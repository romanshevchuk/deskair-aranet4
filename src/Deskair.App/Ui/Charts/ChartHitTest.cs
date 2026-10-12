using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts;

/// <summary>Finds what a mouse position is over on a line chart. Pure, so it is tested without a window.</summary>
public static class ChartHitTest
{
    /// <summary>
    /// The point under <paramref name="mouseX"/>. A point with a null value marks a gap: from that marker to the next real point there is
    /// no data, and the result has a null value. Returns null when the mouse is outside the plot or there are no points.
    /// </summary>
    public static ChartHover? NearestPoint(IReadOnlyList<ChartPoint>? points, ChartArea area, double mouseX)
    {
        if (points is not { Count: > 0 } || area.IsEmpty || !area.ContainsX(mouseX))
        {
            return null;
        }

        var time = area.TimeAt(mouseX);
        var before = LastAtOrBefore(points, time);
        var after = before + 1 < points.Count ? before + 1 : -1;

        if (before < 0)
        {
            return Hover(points[0], area, mouseX);
        }

        if (points[before].Value is null)
        {
            return new ChartHover(time, null, mouseX, null);
        }

        if (after < 0 || points[after].Value is null)
        {
            return Hover(points[before], area, mouseX);
        }

        var nearer = Math.Abs(area.X(points[before].Time) - mouseX) <= Math.Abs(area.X(points[after].Time) - mouseX) ? before : after;

        return Hover(points[nearer], area, mouseX);
    }

    private static ChartHover Hover(ChartPoint point, ChartArea area, double mouseX)
    {
        return point.Value is { } value
            ? new ChartHover(point.Time, value, area.X(point.Time), area.Y(value))
            : new ChartHover(point.Time, null, mouseX, null);
    }

    private static int LastAtOrBefore(IReadOnlyList<ChartPoint> points, DateTimeOffset time)
    {
        var low = 0;
        var high = points.Count - 1;
        var found = -1;

        while (low <= high)
        {
            var middle = (low + high) / 2;

            if (points[middle].Time <= time)
            {
                found = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return found;
    }
}
