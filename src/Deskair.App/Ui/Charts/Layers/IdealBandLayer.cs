using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>Non-CO₂ metrics: the plot filled with the metric colour at low opacity and the ideal range in a stronger tint, with an optional label.</summary>
public sealed class IdealBandLayer : IChartLayer
{
    private const double LabelInset = 6;

    public double? IdealMin { get; set; }

    public double? IdealMax { get; set; }

    public string FillBrushKey { get; set; } = "Metric.Temperature.Fill.Brush";

    public string IdealBrushKey { get; set; } = "Metric.Temperature.Ideal.Brush";

    /// <summary>The text drawn in the ideal band, for example "ideal 30–50". Already localized.</summary>
    public string? Label { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        context.DrawRectangle(area.Brush(FillBrushKey), null, area.Plot);

        if (IdealMin is not { } min || IdealMax is not { } max)
        {
            return;
        }

        var low = Math.Max(min, area.YMin);
        var high = Math.Min(max, area.YMax);

        if (high <= low)
        {
            return;
        }

        var band = new Rect(area.Plot.Left, area.Y(high), area.Plot.Width, area.Y(low) - area.Y(high));
        context.DrawRectangle(area.Brush(IdealBrushKey), null, band);

        if (!string.IsNullOrEmpty(Label))
        {
            var text = area.Text(Label, area.Brush("Text.Tertiary.Brush"));

            if (text.Height + (2 * LabelInset) <= band.Height)
            {
                context.DrawText(text, new Point(band.Right - text.Width - LabelInset, band.Top + LabelInset));
            }
        }
    }
}
