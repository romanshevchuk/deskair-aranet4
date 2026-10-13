using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>A light shaded rectangle, the full height of the plot, behind each weekend. Spans outside the visible range are skipped and the rest is clipped to the plot.</summary>
public sealed class WeekendShadingLayer : IChartLayer
{
    private const double Opacity = 0.6;

    public IReadOnlyList<ChartGap>? WeekendSpans { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (WeekendSpans is null)
        {
            return;
        }

        var brush = area.Brush("Segment.Bg.Brush");

        context.PushOpacity(Opacity);

        foreach (var span in WeekendSpans)
        {
            if (span.End.UtcTicks <= area.XMin || span.Start.UtcTicks >= area.XMax)
            {
                continue;
            }

            var left = Math.Max(area.X(span.Start), area.Plot.Left);
            var right = Math.Min(area.X(span.End), area.Plot.Right);

            if (right > left)
            {
                context.DrawRectangle(brush, null, new Rect(left, area.Plot.Top, right - left, area.Plot.Height));
            }
        }

        context.Pop();
    }
}
