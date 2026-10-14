using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Deskair.App.Ui.Controls;

/// <summary>The StatTriple panel: equal columns side by side with a 1 px divider between them. Every column but the first starts <see cref="Gap"/> after its divider,
/// and every column but the last stops half a gap before the next one, so the values never touch a line (design-tokens.md "divider").</summary>
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

    private List<UIElement> Visible() => InternalChildren.Cast<UIElement>().Where(child => child.Visibility != Visibility.Collapsed).ToList();

    private (double Left, double Right) Insets(int index, int count) => (index > 0 ? Gap : 0, index < count - 1 ? Gap / 2 : 0);

    protected override Size MeasureOverride(Size availableSize)
    {
        var children = Visible();
        if (children.Count == 0)
        {
            return default;
        }

        var column = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width / children.Count;
        double widest = 0, tallest = 0;

        for (var index = 0; index < children.Count; index++)
        {
            var (left, right) = Insets(index, children.Count);
            children[index].Measure(new Size(Math.Max(0, column - left - right), availableSize.Height));
            widest = Math.Max(widest, children[index].DesiredSize.Width + left + right);
            tallest = Math.Max(tallest, children[index].DesiredSize.Height);
        }

        var width = double.IsInfinity(availableSize.Width) ? widest * children.Count : availableSize.Width;

        return new Size(width, tallest);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = Visible();
        var column = children.Count == 0 ? 0 : finalSize.Width / children.Count;

        for (var index = 0; index < children.Count; index++)
        {
            var (left, right) = Insets(index, children.Count);
            children[index].Arrange(new Rect((index * column) + left, 0, Math.Max(0, column - left - right), finalSize.Height));
        }

        return finalSize;
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
        var column = ActualWidth / count;

        for (var index = 1; index < count; index++)
        {
            // Half a pixel in, so a 1 px line covers one device pixel at 100 % instead of blurring over two.
            var x = Math.Round(index * column) + 0.5;
            dc.DrawLine(pen, new Point(x, 0), new Point(x, ActualHeight));
        }
    }
}
