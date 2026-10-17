using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// A stretch of time shaded behind a line: the metric colour at 14 %, with its label inside the top edge when it fits. Clipped to the plot; one outside the time range draws nothing.
/// </summary>
public sealed class HighlightLayer : IChartLayer
{
    private const double FillOpacity = 0.14;
    private const double LabelGap = 4;

    public ChartHighlight? Highlight { get; set; }

    /// <summary>The key of the metric's brush ("Metric.Pressure.Brush").</summary>
    public string BrushKey { get; set; } = "Accent.Brush";

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Highlight is not { } highlight)
        {
            return;
        }

        var left = Math.Max(area.Plot.Left, area.X(highlight.Start));
        var right = Math.Min(area.Plot.Right, area.X(highlight.End));

        if (right <= left)
        {
            return;
        }

        var fill = area.Brush(BrushKey).Clone();
        fill.Opacity = FillOpacity;
        fill.Freeze();

        context.DrawRectangle(fill, null, new Rect(left, area.Plot.Top, right - left, area.Plot.Height));

        if (string.IsNullOrEmpty(highlight.Label))
        {
            return;
        }

        var text = area.Text(highlight.Label, area.Brush("Text.Primary.Brush"), bold: true);
        var x = Math.Clamp(left + ((right - left - text.Width) / 2), area.Plot.Left, Math.Max(area.Plot.Left, area.Plot.Right - text.Width));

        context.DrawText(text, new Point(x, area.Plot.Top + LabelGap));
    }
}
