using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>A vertical line, a dot at the hovered value and the readout text. Nothing is drawn without a hover or outside the plot.</summary>
public sealed class CrosshairLayer : IChartLayer
{
    private const double DotRadius = 4;
    private const double Offset = 10;
    private const double PaddingX = 8;
    private const double PaddingY = 4;
    private const double Corner = 6;

    public ChartHover? Hover { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Hover is not { } hover || !area.ContainsX(hover.X))
        {
            return;
        }

        var line = new Pen(area.Brush("Text.Strong2.Brush"), 1 / area.PixelsPerDip);
        var x = area.Snap(hover.X);

        context.PushOpacity(0.5);
        context.DrawLine(line, new Point(x, area.Plot.Top), new Point(x, area.Plot.Bottom));
        context.Pop();

        if (hover.Y is { } y)
        {
            context.DrawEllipse(area.Brush("Surface.Brush"), new Pen(area.Brush("Text.Strong2.Brush"), 2), new Point(hover.X, y), DotRadius, DotRadius);
        }

        DrawReadout(context, area, hover);
    }

    private static void DrawReadout(DrawingContext context, ChartArea area, ChartHover hover)
    {
        if (string.IsNullOrEmpty(hover.Text))
        {
            return;
        }

        var text = area.Text(hover.Text, area.Brush("Text.Primary.Brush"), bold: true);
        var width = text.Width + (2 * PaddingX);
        var height = text.Height + (2 * PaddingY);
        var left = hover.X + Offset + width <= area.Plot.Right ? hover.X + Offset : hover.X - Offset - width;
        var box = new Rect(left, area.Plot.Top + Corner, width, height);

        context.DrawRoundedRectangle(area.Brush("Surface.Brush"), new Pen(area.Brush("Border.Brush"), 1), box, Corner, Corner);
        context.DrawText(text, new Point(box.Left + PaddingX, box.Top + PaddingY));
    }
}
