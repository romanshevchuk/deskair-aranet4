using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// The comfortable range of Temperature or Humidity as a soft band across the Day chart (screens-and-states.md "History per metric"): the metric colour at 9 %, with its label
/// ("comfort 20–24 °C") at the right end just inside the band's top edge. The band is clipped to the plot; one entirely outside the axis draws nothing.
/// </summary>
public sealed class ComfortBandLayer : IChartLayer
{
    private const double FillOpacity = 0.09;
    private const double LabelInset = 6;
    private const double LabelGap = 3;

    public ChartBand? Band { get; set; }

    /// <summary>The key of the metric's brush ("Metric.Temperature.Brush").</summary>
    public string BrushKey { get; set; } = "Accent.Brush";

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Band is not { } band)
        {
            return;
        }

        var top = Math.Max(area.Plot.Top, area.Y(Math.Min(band.High, area.YMax)));
        var bottom = Math.Min(area.Plot.Bottom, area.Y(Math.Max(band.Low, area.YMin)));

        if (bottom <= top || band.High < area.YMin || band.Low > area.YMax)
        {
            return;
        }

        var fill = area.Brush(BrushKey).Clone();
        fill.Opacity = FillOpacity;
        fill.Freeze();

        context.DrawRectangle(fill, null, new Rect(area.Plot.Left, top, area.Plot.Width, bottom - top));

        if (string.IsNullOrEmpty(band.Label))
        {
            return;
        }

        var text = area.Text(band.Label, area.Brush("Text.Secondary.Brush"));

        if (bottom - top >= text.Height + (2 * LabelGap))
        {
            context.DrawText(text, new Point(area.Plot.Right - text.Width - LabelInset, top + LabelGap));
        }
    }
}
