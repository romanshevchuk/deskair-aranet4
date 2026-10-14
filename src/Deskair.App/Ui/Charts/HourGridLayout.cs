using System.Windows;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts;

/// <summary>
/// Where every part of the History grid goes (design-tokens.md "History grid"): an 84 px label column, the squares, a 160 px end column;
/// the night bracket and hour labels on top, the legend at the bottom. Rows share the height left, between 9 and 44 px each; a folded run of
/// days without readings is one 28 px row; the typical row follows a 16 px gap. Row index -1 is the typical row.
/// </summary>
public sealed class HourGridLayout
{
    public const double LabelWidth = 84;
    public const double EndWidth = 160;
    public const double CollapsedHeight = 28;
    public const double TypicalGap = 16;
    public const double LegendBand = 50;
    private const double TopWithNight = 44;
    private const double TopPlain = 28;
    private const double MinRow = 9;
    private const double MaxRow = 44;

    private readonly List<double> rowTops = [];

    public HourGridLayout(HourGridModel model, Size size)
    {
        ArgumentNullException.ThrowIfNull(model);

        Model = model;
        Size = size;
        GridWidth = Math.Max(0, size.Width - LabelWidth - EndWidth);
        CellWidth = GridWidth / Math.Max(1, model.Columns);
        Top = model.Night is null ? TopPlain : TopWithNight;

        var collapsed = model.Rows.Count(row => row.IsCollapsed);
        var regular = model.Rows.Count - collapsed + (model.Typical is null ? 0 : 1);
        var spare = size.Height - Top - LegendBand - (model.Typical is null ? 0 : TypicalGap) - (collapsed * CollapsedHeight);

        RowHeight = Math.Clamp(regular == 0 ? MaxRow : spare / regular, MinRow, MaxRow);

        var y = Top;
        foreach (var row in model.Rows)
        {
            rowTops.Add(y);
            y += row.IsCollapsed ? CollapsedHeight : RowHeight;
        }

        RowsBottom = y;
        TypicalTop = model.Typical is null ? null : y + TypicalGap;
    }

    public HourGridModel Model { get; }

    public Size Size { get; }

    public double GridLeft { get; } = LabelWidth;

    public double GridWidth { get; }

    public double GridRight => GridLeft + GridWidth;

    public double CellWidth { get; }

    public double RowHeight { get; }

    /// <summary>Where the first row starts; the hour labels and the night bracket sit above it.</summary>
    public double Top { get; }

    public double RowsBottom { get; }

    public double? TypicalTop { get; }

    public double LegendTop => Size.Height - 26;

    /// <summary>Rows under 14 px show labels only on Mondays and Today, and no averages (screens-and-states.md "Grid state").</summary>
    public bool IsCompact => RowHeight < 14;

    public double RowTop(int row) => row < 0 ? TypicalTop ?? 0 : rowTops[row];

    public HourGridRow? RowAt(int row) => row < 0 ? Model.Typical : row < Model.Rows.Count ? Model.Rows[row] : null;

    /// <summary>A square with 1 px of space on each side, so neighbours are 2 px apart.</summary>
    public Rect Cell(int row, int column) => new(GridLeft + (column * CellWidth) + 1, RowTop(row) + 1, Math.Max(0, CellWidth - 2), Math.Max(0, RowHeight - 2));

    /// <summary>The square under a point, or null over anything else (labels, the end column, a folded row, a square that is not drawn).</summary>
    public (int Row, int Column)? HitTest(Point point)
    {
        if (point.X < GridLeft || point.X >= GridRight || CellWidth <= 0)
        {
            return null;
        }

        var column = (int)((point.X - GridLeft) / CellWidth);
        var row = RowIndexAt(point.Y);

        return row is { } index && RowAt(index) is { IsCollapsed: false } found && column < found.Cells.Count && found.Cells[column] is not null
            ? (index, column)
            : null;
    }

    private int? RowIndexAt(double y)
    {
        if (TypicalTop is { } typical && y >= typical && y < typical + RowHeight)
        {
            return -1;
        }

        for (var index = 0; index < rowTops.Count; index++)
        {
            var height = Model.Rows[index].IsCollapsed ? CollapsedHeight : RowHeight;
            if (y >= rowTops[index] && y < rowTops[index] + height)
            {
                return index;
            }
        }

        return null;
    }
}
