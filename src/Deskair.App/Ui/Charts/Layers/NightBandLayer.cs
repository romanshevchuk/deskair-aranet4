using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// The night hours as a soft band behind a time chart (design: History Day): a rounded rectangle in the grid colour over the span, reaching a little above the plot where the word
/// "Night" sits when the band is wide enough. Spans are clipped to the chart's time range.
/// </summary>
public sealed class NightBandLayer : IChartLayer
{
    private const double Radius = 6;
    private const double Rise = 14;
    private const double LabelInset = 8;
    private const double LabelBaseline = 3;
    private static readonly TimeSpan labelMinimum = TimeSpan.FromHours(5);

    public IReadOnlyList<ChartGap>? Spans { get; set; }

    /// <summary>The word on the band, already localized. None when null.</summary>
    public string? Label { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Spans is not { Count: > 0 } spans)
        {
            return;
        }

        var fill = area.Brush("Grid.Brush");
        var quiet = area.Brush("Text.Secondary.Brush");

        foreach (var span in spans)
        {
            var left = Math.Max(area.Plot.Left, area.X(span.Start));
            var right = Math.Min(area.Plot.Right, area.X(span.End));

            if (right <= left)
            {
                continue;
            }

            var top = area.Plot.Top - Rise;

            context.DrawRoundedRectangle(fill, null, new Rect(left, top, right - left, area.Plot.Bottom - top), Radius, Radius);

            if (!string.IsNullOrEmpty(Label) && span.End - span.Start >= labelMinimum)
            {
                var text = area.Text(Label, quiet);

                context.DrawText(text, new Point(left + LabelInset, area.Plot.Top - LabelBaseline - text.Height));
            }
        }
    }
}
