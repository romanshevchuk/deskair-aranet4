using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// Non-CO₂ metrics: the ideal range as a quiet reference. The plot stays on the card surface; the range is the Good zone colour at 9 %,
/// with 1 px dashed limits at 35 % and an optional label top-left inside the band.
/// </summary>
public sealed class IdealBandLayer : IChartLayer
{
    private const double BandOpacity = 0.09;
    private const double LimitOpacity = 0.35;
    private const double LabelInset = 6;
    private const string BrushKey = "Zone.Good.Dot.Brush";

    public double? IdealMin { get; set; }

    public double? IdealMax { get; set; }

    /// <summary>The text drawn in the ideal band, for example "ideal 30–50". Already localized.</summary>
    public string? Label { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
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
        context.DrawRectangle(Faded(area, BandOpacity), null, band);

        DrawLimits(context, area, min, max);
        DrawLabel(context, area, band);
    }

    private static void DrawLimits(DrawingContext context, ChartArea area, double min, double max)
    {
        var pen = new Pen(Faded(area, LimitOpacity), 1 / area.PixelsPerDip) { DashStyle = new DashStyle([4, 4], 0) };

        foreach (var limit in new[] { min, max })
        {
            // A limit outside the visible range has nothing to draw.
            if (limit < area.YMin || limit > area.YMax)
            {
                continue;
            }

            var y = area.Snap(area.Y(limit));
            context.DrawLine(pen, new Point(area.Plot.Left, y), new Point(area.Plot.Right, y));
        }
    }

    private void DrawLabel(DrawingContext context, ChartArea area, Rect band)
    {
        if (string.IsNullOrEmpty(Label))
        {
            return;
        }

        var text = area.Text(Label, area.Brush("Text.Secondary.Brush"));

        if (text.Height + (2 * LabelInset) <= band.Height)
        {
            context.DrawText(text, new Point(band.Left + LabelInset, band.Top + LabelInset));
        }
    }

    private static Brush Faded(ChartArea area, double opacity)
    {
        var brush = area.Brush(BrushKey).Clone();
        brush.Opacity = opacity;
        brush.Freeze();

        return brush;
    }
}
