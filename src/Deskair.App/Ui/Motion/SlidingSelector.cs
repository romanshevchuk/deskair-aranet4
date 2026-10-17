using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Deskair.App.Ui.Motion;

/// <summary>
/// A list whose selected item is marked by one indicator that slides to the new item (150 ms, ease-out) instead of each item drawing its own highlight.
/// The template has an <c>PART_Indicator</c> inside the row of items. The first placement, and every placement while animations are off, is immediate.
/// </summary>
public abstract class SlidingSelector : ListBox
{
    private FrameworkElement? indicator;
    private TranslateTransform? shift;
    private bool placed;
    private Rect lastItem = Rect.Empty;

    protected SlidingSelector()
    {
        Unloaded += (_, _) => placed = false;
    }

    /// <summary>The width of the indicator when it does not cover the whole item (the nav underline); null to cover it.</summary>
    protected virtual double? FixedIndicatorWidth => null;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        indicator = GetTemplateChild("PART_Indicator") as FrameworkElement;
        shift = new TranslateTransform();
        placed = false;

        if (indicator is not null)
        {
            indicator.RenderTransform = shift;
        }
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);
        Place();
    }

    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        var size = base.ArrangeOverride(arrangeBounds);

        Place();

        return size;
    }

    private void Place()
    {
        if (indicator is null || shift is null || indicator.Parent is not FrameworkElement host)
        {
            return;
        }

        if (SelectedIndex < 0 || ItemContainerGenerator.ContainerFromIndex(SelectedIndex) is not FrameworkElement item || !item.IsArrangeValid)
        {
            indicator.Visibility = Visibility.Collapsed;
            placed = false;
            lastItem = Rect.Empty;
            return;
        }

        var bounds = item.TransformToVisual(host).TransformBounds(new Rect(0, 0, item.ActualWidth, item.ActualHeight));

        if (bounds == lastItem)
        {
            return;
        }

        lastItem = bounds;
        indicator.Visibility = Visibility.Visible;

        var (x, width) = SlideMath.Target(bounds, FixedIndicatorWidth);
        var duration = MotionPolicy.Of("Motion.Selection");

        if (!placed || duration.TimeSpan == TimeSpan.Zero)
        {
            shift.BeginAnimation(TranslateTransform.XProperty, null);
            indicator.BeginAnimation(WidthProperty, null);
            shift.X = x;
            indicator.Width = width;
            placed = true;
            return;
        }

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        shift.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(x, duration) { EasingFunction = ease });

        if (FixedIndicatorWidth is null)
        {
            indicator.BeginAnimation(WidthProperty, new DoubleAnimation(width, duration) { EasingFunction = ease });
        }
    }
}
