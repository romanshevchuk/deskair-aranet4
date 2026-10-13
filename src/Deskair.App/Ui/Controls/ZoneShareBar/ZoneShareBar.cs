using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Deskair.App.Ui.Controls;

/// <summary>
/// A stacked bar of the share of time in each zone (fractions, together at most 1). The track shows through where the shares fall short of 1;
/// when they are all zero only the track shows. Fractions that add up to more than 1 are scaled down. The ends are rounded.
/// </summary>
public class ZoneShareBar : Control
{
    public static readonly DependencyProperty GoodProperty = DependencyProperty.Register(
        nameof(Good),
        typeof(double),
        typeof(ZoneShareBar),
        new PropertyMetadata(default(double), OnShareChanged));

    public static readonly DependencyProperty ElevatedProperty = DependencyProperty.Register(
        nameof(Elevated),
        typeof(double),
        typeof(ZoneShareBar),
        new PropertyMetadata(default(double), OnShareChanged));

    public static readonly DependencyProperty HighProperty = DependencyProperty.Register(
        nameof(High),
        typeof(double),
        typeof(ZoneShareBar),
        new PropertyMetadata(default(double), OnShareChanged));

    private ColumnDefinition? goodColumn;
    private ColumnDefinition? elevatedColumn;
    private ColumnDefinition? highColumn;
    private ColumnDefinition? restColumn;
    private FrameworkElement? track;

    static ZoneShareBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ZoneShareBar), new FrameworkPropertyMetadata(typeof(ZoneShareBar)));
    }

    public double Good
    {
        get => (double)GetValue(GoodProperty);
        set => SetValue(GoodProperty, value);
    }

    public double Elevated
    {
        get => (double)GetValue(ElevatedProperty);
        set => SetValue(ElevatedProperty, value);
    }

    public double High
    {
        get => (double)GetValue(HighProperty);
        set => SetValue(HighProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (track is not null)
        {
            track.SizeChanged -= OnTrackSizeChanged;
        }

        goodColumn = GetTemplateChild("PART_Good") as ColumnDefinition;
        elevatedColumn = GetTemplateChild("PART_Elevated") as ColumnDefinition;
        highColumn = GetTemplateChild("PART_High") as ColumnDefinition;
        restColumn = GetTemplateChild("PART_Rest") as ColumnDefinition;
        track = GetTemplateChild("PART_Track") as FrameworkElement;

        if (track is not null)
        {
            track.SizeChanged += OnTrackSizeChanged;
        }

        UpdateColumns();
    }

    private static void OnShareChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        => ((ZoneShareBar)sender).UpdateColumns();

    private static void OnTrackSizeChanged(object sender, SizeChangedEventArgs args)
    {
        // Rounded ends: a Border does not clip its children to its corners, so the bar is clipped by hand.
        if (sender is FrameworkElement element)
        {
            var radius = args.NewSize.Height / 2;
            element.Clip = new RectangleGeometry(new Rect(0, 0, args.NewSize.Width, args.NewSize.Height), radius, radius);
        }
    }

    private void UpdateColumns()
    {
        if (goodColumn is null || elevatedColumn is null || highColumn is null || restColumn is null)
        {
            return;
        }

        var good = Math.Max(0, Good);
        var elevated = Math.Max(0, Elevated);
        var high = Math.Max(0, High);
        var total = good + elevated + high;
        var scale = total > 1 ? 1 / total : 1;

        goodColumn.Width = new GridLength(good * scale, GridUnitType.Star);
        elevatedColumn.Width = new GridLength(elevated * scale, GridUnitType.Star);
        highColumn.Width = new GridLength(high * scale, GridUnitType.Star);
        restColumn.Width = new GridLength(Math.Max(0, 1 - (total * scale)), GridUnitType.Star);
    }
}
