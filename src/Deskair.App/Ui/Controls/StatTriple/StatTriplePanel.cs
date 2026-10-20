using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Deskair.App.Ui.Controls;

/// <summary>The StatTriple panel: equal columns side by side with a 1 px divider between them. Every column but the first starts <see cref="Gap"/> after its divider,
/// and every column but the last stops a quarter of a gap before the next one, so the values never touch a line (design-tokens.md "divider").</summary>
public class StatTriplePanel : Panel
{
    public static readonly DependencyProperty DividerBrushProperty = DependencyProperty.Register(
        nameof(DividerBrush),
        typeof(Brush),
        typeof(StatTriplePanel),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
        nameof(Gap),
        typeof(double),
        typeof(StatTriplePanel),
        new FrameworkPropertyMetadata(16.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush? DividerBrush
    {
        get => (Brush?)GetValue(DividerBrushProperty);
        set => SetValue(DividerBrushProperty, value);
    }

    public double Gap
    {
        get => (double)GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    private double[] columns = [];

    private List<UIElement> Visible() => InternalChildren.Cast<UIElement>().Where(child => child.Visibility != Visibility.Collapsed).ToList();

    private (double Left, double Right) Insets(int index, int count) => (index > 0 ? Gap : 0, index < count - 1 ? Gap / 4 : 0);

    /// <summary>What a cell must have to keep its value on one line: for a <see cref="KeyValueCell"/> the value alone (its caption may wrap), otherwise the whole child.</summary>
    private static double RequiredWidth(UIElement child)
    {
        return FindCell(child) is { } cell ? cell.MeasureValueWidth() : child.DesiredSize.Width;
    }

    private static KeyValueCell? FindCell(DependencyObject element)
    {
        if (element is KeyValueCell cell)
        {
            return cell;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
        {
            if (FindCell(VisualTreeHelper.GetChild(element, index)) is { } found)
            {
                return found;
            }
        }

        return null;
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var children = Visible();
        if (children.Count == 0)
        {
            return default;
        }

        var desired = new double[children.Count];

        for (var index = 0; index < children.Count; index++)
        {
            var (left, right) = Insets(index, children.Count);
            children[index].Measure(new Size(double.PositiveInfinity, availableSize.Height));
            desired[index] = Math.Ceiling(RequiredWidth(children[index])) + 1 + left + right;
        }

        if (double.IsInfinity(availableSize.Width))
        {
            return new Size(desired.Max() * children.Count, children.Max(child => child.DesiredSize.Height));
        }

        var previous = columns;
        columns = Allocate(availableSize.Width, desired);

        // The dividers are drawn from the columns; a new value can move them while the panel keeps its size, and then WPF would not draw them again.
        if (!previous.SequenceEqual(columns))
        {
            InvalidateVisual();
        }

        double tallest = 0;

        for (var index = 0; index < children.Count; index++)
        {
            var (left, right) = Insets(index, children.Count);
            children[index].Measure(new Size(Math.Max(0, columns[index] - left - right), availableSize.Height));
            tallest = Math.Max(tallest, children[index].DesiredSize.Height);
        }

        return new Size(availableSize.Width, tallest);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = Visible();
        var widths = columns.Length == children.Count ? columns : Allocate(finalSize.Width, new double[children.Count]);
        var x = 0.0;

        for (var index = 0; index < children.Count; index++)
        {
            var (left, right) = Insets(index, children.Count);
            children[index].Arrange(new Rect(x + left, 0, Math.Max(0, widths[index] - left - right), finalSize.Height));
            x += widths[index];
        }

        return finalSize;
    }

    /// <summary>
    /// The width of each column: equal, unless a value needs more than its equal share (a long "1522 ppm" at 20 px), in which case it gets what it needs and the others
    /// share the rest equally. When the values cannot all fit, the columns are equal and the text wraps.
    /// </summary>
    public static double[] Allocate(double width, IReadOnlyList<double> desired)
    {
        var count = desired.Count;
        var result = new double[count];
        var fixedColumn = new bool[count];
        var remaining = width;
        var free = count;
        var changed = true;

        while (changed && free > 0)
        {
            changed = false;
            var share = remaining / free;

            for (var index = 0; index < count; index++)
            {
                if (!fixedColumn[index] && desired[index] > share + 0.01)
                {
                    fixedColumn[index] = true;
                    result[index] = desired[index];
                    remaining -= desired[index];
                    free--;
                    changed = true;
                    break;
                }
            }
        }

        if (remaining < 0 || (free == 0 && remaining > 0.01))
        {
            return [.. Enumerable.Repeat(count == 0 ? 0 : width / count, count)];
        }

        for (var index = 0; index < count; index++)
        {
            if (!fixedColumn[index])
            {
                result[index] = remaining / free;
            }
        }

        return result;
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var count = Visible().Count;
        if (DividerBrush is null || count < 2)
        {
            return;
        }

        var pen = new Pen(DividerBrush, 1);
        pen.Freeze();
        var widths = columns.Length == count ? columns : [.. Enumerable.Repeat(ActualWidth / count, count)];
        var edge = 0.0;

        for (var index = 1; index < count; index++)
        {
            edge += widths[index - 1];

            // Half a pixel in, so a 1 px line covers one device pixel at 100 % instead of blurring over two.
            var x = Math.Round(edge) + 0.5;
            dc.DrawLine(pen, new Point(x, 0), new Point(x, ActualHeight));
        }
    }
}
