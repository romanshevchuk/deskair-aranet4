using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>Labels for the typical-day chart: values on the y axis and the hours 0, 6, 12, 18 and 24 on the x axis, in Text.Tertiary.</summary>
public sealed class HourAxisLayer : IChartLayer
{
    private const double LabelGap = 8;
    private static readonly int[] hourLabels = [0, 6, 12, 18, 24];

    public CultureInfo? Culture { get; set; }

    public IReadOnlyList<double>? YTicks { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        var culture = Culture ?? CultureInfo.CurrentUICulture;
        var brush = area.Brush("Text.Tertiary.Brush");

        foreach (var tick in (YTicks ?? []).Where(tick => tick >= area.YMin && tick <= area.YMax))
        {
            var text = area.Text(tick.ToString("N0", culture), brush);
            context.DrawText(text, new Point(area.Plot.Left - LabelGap - text.Width, area.Y(tick) - (text.Height / 2)));
        }

        foreach (var hour in hourLabels)
        {
            var text = area.Text(hour.ToString("00", CultureInfo.InvariantCulture), brush);
            context.DrawText(text, new Point(area.X(hour) - (text.Width / 2), area.Plot.Bottom + LabelGap));
        }
    }
}
