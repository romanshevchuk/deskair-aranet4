using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// The CO₂ zone limits on the Live chart as labelled lines (design-tokens.md "Live chart"): 1 px, dashed 4 on 4 off, in the zone dot colour at 60 %,
/// with the label ("Elevated ↑", the number is on the axis) right-aligned just above in the zone's pill text colour (below, on the top edge). A limit outside the visible range draws nothing; one on its edge is drawn.
/// </summary>
public sealed class ThresholdLayer : IChartLayer
{
    private const double LineOpacity = 0.6;
    private const double LabelGap = 3;

    public double ElevatedFrom { get; set; } = 1000.0;

    public double HighAbove { get; set; } = 1400.0;

    /// <summary>Already localized; no label when null.</summary>
    public string? ElevatedLabel { get; set; }

    /// <summary>Already localized; no label when null.</summary>
    public string? HighLabel { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        DrawLimit(context, area, ElevatedFrom, "Elevated", ElevatedLabel);
        DrawLimit(context, area, HighAbove, "High", HighLabel);
    }

    private static void DrawLimit(DrawingContext context, ChartArea area, double limit, string zone, string? label)
    {
        // A limit on the edge of the axis still counts: "Elevated ↑" on the bottom edge says the whole chart is above it.
        if (limit < area.YMin || limit > area.YMax)
        {
            return;
        }

        var brush = area.Brush($"Zone.{zone}.Dot.Brush").Clone();
        brush.Opacity = LineOpacity;
        brush.Freeze();

        var pen = new Pen(brush, 1 / area.PixelsPerDip) { DashStyle = new DashStyle([4, 4], 0) };
        var y = area.Snap(area.Y(limit));

        context.DrawLine(pen, new Point(area.Plot.Left, y), new Point(area.Plot.Right, y));

        if (string.IsNullOrEmpty(label))
        {
            return;
        }

        var text = area.Text(label, area.Brush($"Zone.{zone}.PillText.Brush"), bold: true);

        var above = y - LabelGap - text.Height;

        // On the top edge there is no room above the line inside the plot, so the label goes under it.
        context.DrawText(text, new Point(area.Plot.Right - text.Width, above >= area.Plot.Top ? above : y + LabelGap));
    }
}
