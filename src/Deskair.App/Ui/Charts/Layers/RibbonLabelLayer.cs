using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// The words around the 24 h ribbon on the Live chart (prototype): a caption ("24 h") at the left of the bar, a white notch across the bar at 00, 06, 12 and 18 o'clock local time and the hour
/// ("00h") under each notch. The ribbon ends now, so the notches are wherever those hours fall in the last 24 hours. A notch at the very ends of the bar is left out.
/// </summary>
public sealed class RibbonLabelLayer : IChartLayer
{
    private const double LabelGap = 8;
    private const double LabelTop = 4;
    private const double NotchWidth = 2;
    private static readonly int[] hours = [0, 6, 12, 18];

    public string? Caption { get; set; }

    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    public void Draw(DrawingContext context, ChartArea area)
    {
        var quiet = area.Brush("Text.Tertiary.Brush");

        if (!string.IsNullOrEmpty(Caption))
        {
            var caption = area.Text(Caption, quiet);

            context.DrawText(caption, new Point(area.Plot.Left - LabelGap - caption.Width, area.Plot.Top + ((area.Plot.Height - caption.Height) / 2)));
        }

        var notch = new Pen(area.Brush("Surface.Brush"), NotchWidth);

        foreach (var (time, hour) in HourMarks(area))
        {
            var x = area.X(time);

            if (x <= area.Plot.Left + NotchWidth || x >= area.Plot.Right - NotchWidth)
            {
                continue;
            }

            context.DrawLine(notch, new Point(x, area.Plot.Top), new Point(x, area.Plot.Bottom));

            var label = area.Text($"{hour:00}:00", quiet);

            context.DrawText(label, new Point(x - (label.Width / 2), area.Plot.Bottom + LabelTop));
        }
    }

    /// <summary>The 00, 06, 12 and 18 o'clock instants inside the ribbon, in local time.</summary>
    private IEnumerable<(DateTimeOffset Time, int Hour)> HourMarks(ChartArea area)
    {
        var start = TimeZoneInfo.ConvertTime(new DateTimeOffset((long)area.XMin, TimeSpan.Zero), TimeZone).Date;
        var end = new DateTimeOffset((long)area.XMax, TimeSpan.Zero);

        for (var day = start.AddDays(-1); day <= start.AddDays(2); day = day.AddDays(1))
        {
            foreach (var hour in hours)
            {
                var local = day.AddHours(hour);
                var time = new DateTimeOffset(local, TimeZone.GetUtcOffset(local));

                if (time.UtcTicks >= area.XMin && time <= end)
                {
                    yield return (time, hour);
                }
            }
        }
    }
}
