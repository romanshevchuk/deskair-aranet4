using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// The History grid (D-0018, design-tokens.md "History grid"): the night bracket and hour labels, one row of squares per day in the heat scale,
/// each row's label, average, zone bar and best / worst tag, folded runs of empty days, the typical row with its worst three hours outlined,
/// and a ring on the square the mouse or the keyboard is on.
/// </summary>
public sealed class HourGridLayer : IChartLayer
{
    private static readonly string[] heatKeys = ["Heat.G1.Brush", "Heat.G2.Brush", "Heat.E1.Brush", "Heat.E2.Brush", "Heat.H1.Brush", "Heat.H2.Brush"];
    private const double AverageRight = 46;
    private const double BarLeft = 56;
    private const double BarWidth = 60;
    private const double TagLeft = 124;
    private const double NightLine = 18;

    public HourGridLayout? Layout { get; set; }

    /// <summary>The square with the ring, if any.</summary>
    public (int Row, int Column)? Focused { get; set; }

    /// <summary>The ring is the blue focus ring for the keyboard and the ink accent for the mouse.</summary>
    public bool KeyboardFocus { get; set; }

    /// <summary>The heat step (0 to 5) of a value, from the model's step limits.</summary>
    public static int Step(double value, IReadOnlyList<(double Value, string Text)> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        var step = 0;
        while (step < steps.Count && value >= steps[step].Value)
        {
            step++;
        }

        return Math.Min(step, heatKeys.Length - 1);
    }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Layout is not { } layout || layout.GridWidth <= 0)
        {
            return;
        }

        var text = layout.RowHeight < 16 ? area with { FontSize = Math.Min(area.FontSize, 11) } : area;

        DrawTop(context, area, layout);

        for (var index = 0; index < layout.Model.Rows.Count; index++)
        {
            var row = layout.Model.Rows[index];

            if (row.IsCollapsed)
            {
                DrawCollapsed(context, area, layout, index, row);
                continue;
            }

            if (!layout.IsCompact || row.LabelAlways)
            {
                DrawLabel(context, text, layout, index, row);
            }

            DrawCells(context, area, layout, index, row);
            DrawEnd(context, text, layout, index, row);
        }

        DrawTypical(context, area, text, layout);
        DrawRing(context, area, layout);
    }

    private static void DrawTop(DrawingContext context, ChartArea area, HourGridLayout layout)
    {
        var model = layout.Model;

        if (model.Night is { } night)
        {
            var pen = new Pen(area.Brush("Text.Tertiary.Brush"), 2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            var spans = night.Start < night.End ? new[] { (night.Start, night.End) } : [(0, night.End), (night.Start, model.Columns)];

            context.PushOpacity(0.55);
            foreach (var (from, to) in spans.Where(span => span.Item2 > span.Item1))
            {
                context.DrawLine(pen, new Point(layout.GridLeft + (from * layout.CellWidth) + 2, NightLine), new Point(layout.GridLeft + (to * layout.CellWidth) - 2, NightLine));
            }

            context.Pop();

            if (model.NightText is { } label)
            {
                var first = spans.First(span => span.Item2 > span.Item1).Item1;
                var formatted = area.Text(label, area.Brush("Text.Secondary.Brush"));
                context.DrawText(formatted, new Point(layout.GridLeft + (first * layout.CellWidth) + 2, NightLine - 4 - formatted.Height));
            }
        }

        foreach (var (column, label) in model.ColumnLabels)
        {
            var formatted = area.Text(label, area.Brush("Text.Tertiary.Brush"));
            context.DrawText(formatted, new Point(layout.GridLeft + (column * layout.CellWidth) + 1, layout.Top - 6 - formatted.Height));
        }
    }

    private static void DrawLabel(DrawingContext context, ChartArea text, HourGridLayout layout, int index, HourGridRow row)
    {
        var formatted = text.Text(row.Label, text.Brush(row.Emphasis ? "Text.Primary.Brush" : "Text.Secondary.Brush"), bold: row.Emphasis);
        formatted.MaxTextWidth = HourGridLayout.LabelWidth - 6;
        formatted.MaxLineCount = 1;
        formatted.Trimming = TextTrimming.CharacterEllipsis;

        context.DrawText(formatted, new Point(0, layout.RowTop(index) + ((layout.RowHeight - formatted.Height) / 2)));
    }

    private static void DrawCells(DrawingContext context, ChartArea area, HourGridLayout layout, int index, HourGridRow row)
    {
        var radius = Math.Min(4, layout.RowHeight / 4);
        var outline = new Pen(area.Brush("Heat.None.Line.Brush"), 1);

        for (var column = 0; column < row.Cells.Count && column < layout.Model.Columns; column++)
        {
            if (row.Cells[column] is not { } cell)
            {
                continue;
            }

            var rect = layout.Cell(index, column);

            if (cell.Value is { } value)
            {
                context.DrawRoundedRectangle(area.Brush(heatKeys[Step(value, layout.Model.Steps)]), null, rect, radius, radius);
            }
            else
            {
                rect.Inflate(-0.5, -0.5);
                context.DrawRoundedRectangle(null, outline, rect, radius, radius);
            }
        }
    }

    private static void DrawEnd(DrawingContext context, ChartArea text, HourGridLayout layout, int index, HourGridRow row)
    {
        var middle = layout.RowTop(index) + (layout.RowHeight / 2);

        if (row.AverageText is { } average && !layout.IsCompact)
        {
            var formatted = text.Text(average, text.Brush("Text.Primary.Brush"));
            context.DrawText(formatted, new Point(layout.GridRight + AverageRight - formatted.Width, middle - (formatted.Height / 2)));
        }

        if (row.Share is { } share)
        {
            var height = Math.Clamp(layout.RowHeight - 6, 3, 6);
            var x = layout.GridRight + BarLeft;

            foreach (var (part, zone) in new[] { (share.Good, "Good"), (share.Elevated, "Elevated"), (share.High, "High") })
            {
                var width = BarWidth * part;
                if (width >= 1)
                {
                    context.DrawRoundedRectangle(text.Brush($"Zone.{zone}.Segment.Brush"), null, new Rect(x, middle - (height / 2), Math.Max(0.5, width - 1.5), height), Math.Min(2, height / 2), Math.Min(2, height / 2));
                }

                x += width;
            }
        }

        if (row.Tag != HourGridTag.None)
        {
            var label = row.Tag == HourGridTag.Best ? layout.Model.BestText : layout.Model.WorstTagText;
            var brush = text.Brush(row.Tag == HourGridTag.Best ? "Zone.Good.PillText.Brush" : "Zone.High.PillText.Brush");
            var formatted = text.Text(label, brush, bold: true);
            context.DrawText(formatted, new Point(layout.GridRight + TagLeft, middle - (formatted.Height / 2)));
        }
    }

    private static void DrawCollapsed(DrawingContext context, ChartArea area, HourGridLayout layout, int index, HourGridRow row)
    {
        var top = layout.RowTop(index);
        var rect = new Rect(layout.GridLeft + 1.5, top + 5.5, Math.Max(0, layout.GridWidth - 3), HourGridLayout.CollapsedHeight - 11);
        var formatted = area.Text(row.CollapsedText!, area.Brush("Text.Secondary.Brush"));

        context.DrawRoundedRectangle(null, new Pen(area.Brush("Heat.None.Line.Brush"), 1), rect, 5, 5);
        context.DrawText(formatted, new Point(layout.GridLeft + ((layout.GridWidth - formatted.Width) / 2), rect.Top + ((rect.Height - formatted.Height) / 2)));
    }

    private static void DrawTypical(DrawingContext context, ChartArea area, ChartArea text, HourGridLayout layout)
    {
        if (layout.Model.Typical is not { } typical || layout.TypicalTop is not { } top)
        {
            return;
        }

        var separator = area.Snap(layout.RowsBottom + (HourGridLayout.TypicalGap / 2));
        context.DrawLine(new Pen(area.Brush("Divider.Brush"), 1 / area.PixelsPerDip), new Point(0, separator), new Point(layout.Size.Width, separator));

        DrawLabel(context, text, layout, -1, typical);
        DrawCells(context, area, layout, -1, typical);

        if (layout.Model.WorstStart is not { } worst)
        {
            return;
        }

        var first = layout.Cell(-1, worst);
        var last = layout.Cell(-1, Math.Min(worst + 2, layout.Model.Columns - 1));
        var outline = new Rect(first.Left - 2, top - 1, last.Right - first.Left + 4, layout.RowHeight + 2);
        var radius = Math.Min(4, layout.RowHeight / 4) + 2;

        context.DrawRoundedRectangle(null, new Pen(area.Brush("Accent.Brush"), 1.5), outline, radius, radius);

        if (layout.Model.WorstText is { } label)
        {
            var formatted = text.Text(label, text.Brush("Text.Secondary.Brush"));
            context.DrawText(formatted, new Point(layout.GridRight + 14, top + ((layout.RowHeight - formatted.Height) / 2)));
        }
    }

    private void DrawRing(DrawingContext context, ChartArea area, HourGridLayout layout)
    {
        if (Focused is not { } focused)
        {
            return;
        }

        var rect = layout.Cell(focused.Row, focused.Column);
        rect.Inflate(1.5, 1.5);
        var radius = Math.Min(4, layout.RowHeight / 4) + 1.5;

        context.DrawRoundedRectangle(null, new Pen(area.Brush(KeyboardFocus ? "Focus.Ring.Brush" : "Accent.Brush"), 2), rect, radius, radius);
    }
}
