using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Deskair.App.Localization;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>Labels for the typical-day chart: values on the y axis at <see cref="YTicks"/> and a label every three hours under the middle of that hour's bar ("00h", "03h" ... "21h"), in Text.Tertiary.</summary>
public sealed class HourAxisLayer : IChartLayer
{
    private const double LabelGap = 8;
    private const int LabelEvery = 3;
    private const int Hours = 24;

    public CultureInfo? Culture { get; set; }

    public IReadOnlyList<double>? YTicks { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        var culture = Culture ?? CultureInfo.CurrentUICulture;
        var brush = area.Brush("Text.Tertiary.Brush");

        foreach (var tick in (YTicks ?? []).Where(tick => tick >= area.YMin && tick <= area.YMax))
        {
            var text = area.Text(tick.ToString(NumberFormat.WholeFormat, culture), brush);
            context.DrawText(text, new Point(area.Plot.Left - LabelGap - text.Width, area.Y(tick) - (text.Height / 2)));
        }

        for (var hour = 0; hour < Hours; hour += LabelEvery)
        {
            var text = area.Text($"{hour:00}h", brush);
            context.DrawText(text, new Point(area.X(hour + 0.5) - (text.Width / 2), area.Plot.Bottom + LabelGap));
        }
    }
}
