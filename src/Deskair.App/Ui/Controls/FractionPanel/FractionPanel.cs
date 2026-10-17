using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

/// <summary>
/// Places each child so its middle is at a fraction of the panel's width (0 at the left edge, 1 at the right), kept inside the panel. Used for the flyout ribbon's labels, which sit at their true times.
/// Set <see cref="PositionProperty"/> on each child.
/// </summary>
public class FractionPanel : Panel
{
    public static readonly DependencyProperty PositionProperty = DependencyProperty.RegisterAttached(
        "Position",
        typeof(double),
        typeof(FractionPanel),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsParentArrange));

    public static double GetPosition(DependencyObject element) => (double)element.GetValue(PositionProperty);

    public static void SetPosition(DependencyObject element, double value) => element.SetValue(PositionProperty, value);

    /// <summary>The left edge of a child of this width, for a position in a panel of this width.</summary>
    public static double Left(double position, double width, double panelWidth)
        => Math.Clamp((position * panelWidth) - (width / 2), 0, Math.Max(0, panelWidth - width));

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = 0.0;

        foreach (UIElement child in InternalChildren)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (UIElement child in InternalChildren)
        {
            var width = child.DesiredSize.Width;

            child.Arrange(new Rect(Left(GetPosition(child), width, finalSize.Width), 0, width, finalSize.Height));
        }

        return finalSize;
    }
}
