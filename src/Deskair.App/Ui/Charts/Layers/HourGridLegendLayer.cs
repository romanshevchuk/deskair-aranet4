using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// The legend under the History grid: the six heat steps with the zone names above them and the limits below ("800", "1000" …), the
/// "No data" outline, and "Each square is one hour" on the right. A semantic scale always shows its legend (D-0018).
/// </summary>
public sealed class HourGridLegendLayer : IChartLayer
{
    private static readonly string[] heatKeys = ["Heat.G1.Brush", "Heat.G2.Brush", "Heat.E1.Brush", "Heat.E2.Brush", "Heat.H1.Brush", "Heat.H2.Brush"];
    private const double Swatch = 46;
    private const double SwatchHeight = 8;
    private const double SwatchGap = 2;

    public HourGridLayout? Layout { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Layout is not { } layout || layout.GridWidth <= 0)
        {
            return;
        }

        var model = layout.Model;
        var top = layout.LegendTop;
        var left = layout.GridLeft;
        var step = Swatch + SwatchGap;
        var small = area with { FontSize = Math.Min(area.FontSize, 11) };

        if (model.Steps.Count == heatKeys.Length - 1)
        {
            for (var index = 0; index < heatKeys.Length; index++)
            {
                context.DrawRoundedRectangle(area.Brush(heatKeys[index]), null, new Rect(left + (index * step), top, Swatch, SwatchHeight), 2, 2);
            }

            foreach (var (name, pair) in new[] { (model.GoodText, 0), (model.ElevatedText, 1), (model.HighText, 2) })
            {
                var formatted = area.Text(name, area.Brush("Text.Secondary.Brush"));
                context.DrawText(formatted, new Point(left + (pair * 2 * step) + Swatch - (formatted.Width / 2), top - 3 - formatted.Height));
            }

            for (var index = 0; index < model.Steps.Count; index++)
            {
                var formatted = small.Text(model.Steps[index].Text, area.Brush("Text.Tertiary.Brush"));
                context.DrawText(formatted, new Point(left + ((index + 1) * step) - 1 - (formatted.Width / 2), top + SwatchHeight + 2));
            }

            left += (heatKeys.Length * step) + 20;
        }

        context.DrawRoundedRectangle(null, new Pen(area.Brush("Heat.None.Line.Brush"), 1), new Rect(left + 0.5, top + 0.5, 14, SwatchHeight - 1), 2, 2);
        var none = area.Text(model.NoDataText, area.Brush("Text.Secondary.Brush"));
        context.DrawText(none, new Point(left + 20, top + (SwatchHeight / 2) - (none.Height / 2)));

        var unit = area.Text(model.UnitText, area.Brush("Text.Tertiary.Brush"));
        context.DrawText(unit, new Point(layout.Size.Width - unit.Width, top + (SwatchHeight / 2) - (unit.Height / 2)));
    }
}
