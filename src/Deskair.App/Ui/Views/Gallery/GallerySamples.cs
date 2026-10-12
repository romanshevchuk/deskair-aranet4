using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Views.Gallery;

/// <summary>Fixed sample data for the gallery. It uses a constant date so the pictures are the same on every run.</summary>
public static class GallerySamples
{
    private static readonly DateTimeOffset end = new(2026, 10, 12, 20, 30, 0, TimeSpan.Zero);

    public static DateTimeOffset End => end;

    private static readonly double[] co2 =
    [
        1660, 1640, 1670, 1680, 1590, 1540, 1535, 1475, 1410, 1330, 1395, 1350, 1310, 1285, 1275, 1240, 1195, 1150, 1115, 1068
    ];

    public static IReadOnlyList<ChartPoint> Co2Falling { get; } = Series(co2, 3, gapAfter: -1);

    public static IReadOnlyList<ChartPoint> Co2WithGap { get; } = Series(co2, 3, gapAfter: 8);

    public static IReadOnlyList<ChartPoint> Temperature { get; } = Series(
        [22.1, 22.1, 22.2, 22.2, 22.2, 22.3, 22.2, 22.3, 22.3, 22.4, 22.3, 22.3, 22.4, 22.3, 22.3, 22.2, 22.3, 22.3, 22.3, 22.3], 3, -1);

    public static IReadOnlyList<ChartPoint> Humidity { get; } = Series(
        [43, 43, 43, 44, 44, 44, 44, 44, 45, 45, 44, 44, 44, 44, 44, 44, 44, 44, 44, 44], 3, -1);

    public static IReadOnlyList<ChartPoint> Pressure { get; } = Series(
        [975, 975.2, 975.1, 975.3, 975.2, 975.4, 975.3, 975.3, 975.5, 975.4, 975.3, 975.4, 975.5, 975.4, 975.3, 975.4, 975.5, 975.4, 975.3, 975.3], 3, -1);

    public static IReadOnlyList<ChartPoint> NoPoints { get; } = [];

    public static IReadOnlyList<ChartZoneSegment> Day { get; } = BuildDay();

    private static List<ChartPoint> Series(double[] values, int minutesApart, int gapAfter)
    {
        var points = new List<ChartPoint>();

        for (var index = 0; index < values.Length; index++)
        {
            var time = end - TimeSpan.FromMinutes((values.Length - 1 - index) * minutesApart);
            points.Add(new ChartPoint(time, values[index]));

            if (index == gapAfter)
            {
                points.Add(new ChartPoint(time + TimeSpan.FromMinutes(minutesApart), null));
            }
        }

        return points;
    }

    private static List<ChartZoneSegment> BuildDay()
    {
        var segments = new List<ChartZoneSegment>();
        var start = end - TimeSpan.FromHours(24);

        for (var slot = 0; slot < 144; slot++)
        {
            if (slot is >= 20 and < 23)
            {
                continue;
            }

            var zone = slot < 56 ? Zone.Good : slot < 104 ? Zone.Elevated : slot < 142 ? Zone.High : Zone.Elevated;
            var from = start + TimeSpan.FromMinutes(slot * 10);

            segments.Add(new ChartZoneSegment(from, from + TimeSpan.FromMinutes(10), zone));
        }

        return segments;
    }
}
