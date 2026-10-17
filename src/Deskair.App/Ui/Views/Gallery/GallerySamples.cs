using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Views.Gallery;

/// <summary>Fixed sample data for the gallery. It uses a constant date so the pictures are the same on every run.</summary>
public static class GallerySamples
{
    private static readonly DateTimeOffset end = new(2026, 10, 12, 20, 30, 0, TimeSpan.Zero);

    public static DateTimeOffset End => end;

    /// <summary>A week of the History grid: busy afternoons, an evening without data, today up to 20:00 (fixed texts: the gallery shows the control, not the copy).</summary>
    public static HourGridModel Grid { get; } = BuildGrid();

    /// <summary>One day of CO₂ every 10 minutes, ending at <see cref="End"/>: a stuffy night, a drop in the morning when the air was changed, a worse afternoon than usual.</summary>
    public static IReadOnlyList<ChartPoint> DayLine { get; } = BuildDayLine();

    /// <summary>The typical day over the same 24 hours: one point in the middle of each hour.</summary>
    public static IReadOnlyList<ChartPoint> DayTypical { get; } = [.. Enumerable.Range(0, 24).Select(hour => new ChartPoint(end.AddHours(-24 + hour).AddMinutes(30), 650 + (500 * Math.Max(0, Math.Sin(Math.PI * (end.AddHours(-24 + hour).Hour - 6) / 12.0)))))];

    public static IReadOnlyList<ChartGap> DayNight { get; } = [new ChartGap(new DateTimeOffset(2026, 10, 11, 22, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 12, 7, 0, 0, TimeSpan.Zero))];

    public static IReadOnlyList<ChartMarker> DayMarkers { get; } = [new ChartMarker(new DateTimeOffset(2026, 10, 12, 8, 20, 0, TimeSpan.Zero), 1430, "Air improved at 08:20 · −640 ppm in 20 min")];

    /// <summary>The Year grid: months as rows, days as squares, centred day numbers.</summary>
    public static HourGridModel YearGrid { get; } = BuildYearGrid();

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

    private static List<ChartPoint> BuildDayLine()
    {
        var points = new List<ChartPoint>();

        for (var step = 0; step <= 144; step++)
        {
            var time = end.AddHours(-24).AddMinutes(step * 10);
            var hour = time.Hour + (time.Minute / 60.0);
            var value = hour switch
            {
                < 6 => 1150 - (hour * 20),
                < 8.3 => 1030 + ((hour - 6) * 180),
                < 8.7 => 1430 - ((hour - 8.3) * 1600),
                < 12 => 800 + ((hour - 8.7) * 60),
                < 17 => 1000 + (500 * Math.Sin((hour - 12) * Math.PI / 5)) + 150,
                _ => 1150 - ((hour - 17) * 90)
            };

            points.Add(new ChartPoint(time, value));
        }

        return points;
    }

    private static HourGridModel BuildGrid()
    {
        string[] days = ["Tue 6", "Wed 7", "Thu 8", "Fri 9", "Sat 10", "Sun 11", "Today"];

        static double Value(int day, int hour)
            => 650 + (day * 30) + (hour is >= 9 and <= 18 ? 450 + (60 * Math.Sin(hour + day)) : 120 * Math.Cos(hour)) + (hour is >= 15 and <= 17 ? 420 : 0);

        var rows = new List<HourGridRow>();
        for (var day = 0; day < days.Length; day++)
        {
            var cells = Enumerable.Range(0, 24)
                .Select(hour => day == 6 && hour > 20 ? null : day == 1 && hour >= 19 ? new HourGridCell(null, $"{days[day]}, {hour:00}:00 · No data") : new HourGridCell(Value(day, hour), $"{days[day]}, {hour:00}:00 · {Value(day, hour):0} ppm"))
                .ToList();
            var tag = day == 0 ? HourGridTag.Best : day == 5 ? HourGridTag.Worst : HourGridTag.None;
            rows.Add(new HourGridRow(days[day], cells, $"{950 + (day * 30)}", new ZoneShare(0.5 - (day * 0.04), 0.35, 0.15 + (day * 0.04)), tag, LabelAlways: day == 6, Emphasis: day == 6));
        }

        var typical = new HourGridRow("Typical", Enumerable.Range(0, 24).Select(hour => (HourGridCell?)new HourGridCell(Value(3, hour), $"Typical, {hour:00}:00")).ToList(), LabelAlways: true, Emphasis: true);

        return new HourGridModel(
            rows,
            [.. Enumerable.Range(0, 8).Select(index => (index * 3, $"{index * 3:00}:00"))],
            [(800, "800"), (1000, "1000"), (1200, "1200"), (1400, "1400"), (2000, "2000")])
        {
            Typical = typical,
            WorstStart = 15,
            WorstText = "Worst 15:00–18:00",
            Night = (22, 7),
            NightText = "Night · 22:00–07:00",
            BestText = "best",
            WorstTagText = "worst",
            LegendGroups = [("Good", 0, 2), ("Elevated", 2, 2), ("High", 4, 2)],
            NoDataText = "No data",
            UnitText = "Each square is one hour"
        };
    }

    private static HourGridModel BuildYearGrid()
    {
        string[] months = ["Jul 2026", "Aug 2026", "Sep 2026", "Oct 2026"];
        int[] lengths = [31, 31, 30, 14];

        var rows = new List<HourGridRow>();
        for (var month = 0; month < months.Length; month++)
        {
            var cells = Enumerable.Range(0, 31)
                .Select(day => day >= lengths[month] ? null : month == 1 && day is >= 9 and <= 11 ? new HourGridCell(null, $"{day + 1} {months[month]} · No data") : new HourGridCell(700 + (month * 90) + (50 * Math.Sin(day + month)) + (day % 9 == 0 ? 450 : 0), $"{day + 1} {months[month]}"))
                .ToList();
            var tag = month == 0 ? HourGridTag.Best : month == 2 ? HourGridTag.Worst : HourGridTag.None;
            rows.Add(new HourGridRow(months[month], cells, $"{900 + (month * 60)}", new ZoneShare(0.6 - (month * 0.08), 0.3, 0.1 + (month * 0.08)), tag, LabelAlways: true, Emphasis: month == 3));
        }

        return new HourGridModel(
            rows,
            [(0, "1"), (4, "5"), (9, "10"), (14, "15"), (19, "20"), (24, "25"), (30, "31")],
            [(800, "800"), (1000, "1000"), (1200, "1200"), (1400, "1400"), (2000, "2000")])
        {
            Columns = 31,
            CenterColumnLabels = true,
            BestText = "best",
            WorstTagText = "worst",
            LegendGroups = [("Good", 0, 2), ("Elevated", 2, 2), ("High", 4, 2)],
            NoDataText = "No data",
            UnitText = "Each square is one day"
        };
    }
}
