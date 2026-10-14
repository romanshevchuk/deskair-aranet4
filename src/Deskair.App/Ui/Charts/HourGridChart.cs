using System.Windows;
using System.Windows.Input;
using Deskair.App.Ui.Charts.Layers;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts;

/// <summary>
/// The History grid for Week and Month (D-0018): rows of days, a square per hour in the heat scale. The mouse and the keyboard point at one square
/// at a time and its text goes to <see cref="ChartBase.Hover"/> for the readout: Tab enters on the latest hour, arrow keys move, Home and End go to
/// the ends of the row (accessibility.md "Keyboard"). Everything shown, the texts included, comes from <see cref="Model"/>.
/// </summary>
public sealed class HourGridChart : ChartBase
{
    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
        nameof(Model),
        typeof(HourGridModel),
        typeof(HourGridChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnModelChanged));

    private (int Row, int Column)? focused;

    public HourGridChart()
    {
        Focusable = true;
        FocusVisualStyle = null;
    }

    public HourGridModel? Model
    {
        get => (HourGridModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    protected override Thickness PlotMargin => default;

    private HourGridLayout? CurrentLayout => Model is { } model ? new HourGridLayout(model, RenderSize) : null;

    protected override IReadOnlyList<IChartLayer> CreateLayers()
    {
        if (CurrentLayout is not { } layout)
        {
            return [];
        }

        return
        [
            new HourGridLayer { Layout = layout, Focused = focused, KeyboardFocus = IsKeyboardFocused },
            new HourGridLegendLayer { Layout = layout }
        ];
    }

    protected override ChartHover? HitTest(Point position, ChartArea area)
    {
        var cell = CurrentLayout?.HitTest(position);
        Point(cell);

        return HoverFor(cell);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        if (!IsKeyboardFocused)
        {
            Point(null);
        }
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        Move(focused ?? Latest());
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        Move(null);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (focused is not { } at || CurrentLayout is not { } layout)
        {
            return;
        }

        (int Row, int Column)? next = e.Key switch
        {
            Key.Left => Step(layout, at, 0, -1),
            Key.Right => Step(layout, at, 0, 1),
            Key.Up => Step(layout, at, -1, 0),
            Key.Down => Step(layout, at, 1, 0),
            Key.Home => Edge(layout, at.Row, fromStart: true),
            Key.End => Edge(layout, at.Row, fromStart: false),
            _ => null
        };

        if (next is { } target)
        {
            Move(target);
            e.Handled = true;
        }
    }

    private static void OnModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var chart = (HourGridChart)sender;
        chart.focused = null;

        if (chart.IsKeyboardFocused)
        {
            chart.Move(chart.Latest());
        }
    }

    private void Point((int Row, int Column)? cell)
    {
        if (focused != cell)
        {
            focused = cell;
            InvalidateVisual();
        }
    }

    private void Move((int Row, int Column)? cell)
    {
        Point(cell);
        SetHover(HoverFor(cell));
    }

    private ChartHover? HoverFor((int Row, int Column)? cell)
    {
        if (cell is not { } at || CurrentLayout is not { } layout || layout.RowAt(at.Row)?.Cells[at.Column] is not { } square)
        {
            return null;
        }

        var rect = layout.Cell(at.Row, at.Column);

        return new ChartHover(default, square.Value, rect.Left + (rect.Width / 2), rect.Top + (rect.Height / 2), square.Text);
    }

    /// <summary>The last hour shown in the last day row: now, on the latest period.</summary>
    private (int Row, int Column)? Latest()
    {
        if (CurrentLayout is not { } layout)
        {
            return null;
        }

        for (var row = layout.Model.Rows.Count - 1; row >= 0; row--)
        {
            if (Edge(layout, row, fromStart: false) is { } cell)
            {
                return cell;
            }
        }

        return null;
    }

    private static (int Row, int Column)? Edge(HourGridLayout layout, int row, bool fromStart)
    {
        if (layout.RowAt(row) is not { IsCollapsed: false } found)
        {
            return null;
        }

        var columns = Enumerable.Range(0, found.Cells.Count).Where(column => found.Cells[column] is not null);
        var pick = fromStart ? columns.Cast<int?>().FirstOrDefault() : columns.Cast<int?>().LastOrDefault();

        return pick is { } column ? (row, column) : null;
    }

    /// <summary>The next square in a direction, skipping folded rows; up from the first row stays, down from the last day row goes to the typical row.</summary>
    private static (int Row, int Column)? Step(HourGridLayout layout, (int Row, int Column) at, int rows, int columns)
    {
        if (columns != 0)
        {
            var column = at.Column + columns;
            return layout.RowAt(at.Row) is { } row && column >= 0 && column < row.Cells.Count && row.Cells[column] is not null ? (at.Row, column) : null;
        }

        // Order of rows top to bottom: the day rows, then the typical row (index -1).
        var order = Enumerable.Range(0, layout.Model.Rows.Count).Where(index => !layout.Model.Rows[index].IsCollapsed).ToList();
        if (layout.Model.Typical is not null)
        {
            order.Add(-1);
        }

        for (var position = order.IndexOf(at.Row) + rows; position >= 0 && position < order.Count; position += rows)
        {
            var row = layout.RowAt(order[position])!;
            if (at.Column < row.Cells.Count && row.Cells[at.Column] is not null)
            {
                return (order[position], at.Column);
            }
        }

        return null;
    }
}
