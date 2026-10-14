using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Deskair.App.Localization;

namespace Deskair.App.Ui.Controls;

/// <summary>
/// The CO₂ scale: three zone segments with widths proportional to the scale, ticks at the two zone limits and a round marker at Value.
/// The template is a Grid with proportional columns; this class only sets the column widths and places the tick labels.
/// </summary>
public class Gauge : Control
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double?),
        typeof(Gauge),
        new PropertyMetadata(null, OnScaleChanged));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum),
        typeof(double),
        typeof(Gauge),
        new PropertyMetadata(400.0, OnScaleChanged));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(double),
        typeof(Gauge),
        new PropertyMetadata(2000.0, OnScaleChanged));

    public static readonly DependencyProperty ElevatedFromProperty = DependencyProperty.Register(
        nameof(ElevatedFrom),
        typeof(double),
        typeof(Gauge),
        new PropertyMetadata(1000.0, OnScaleChanged));

    public static readonly DependencyProperty HighAboveProperty = DependencyProperty.Register(
        nameof(HighAbove),
        typeof(double),
        typeof(Gauge),
        new PropertyMetadata(1400.0, OnScaleChanged));

    private ColumnDefinition? goodColumn;
    private ColumnDefinition? elevatedColumn;
    private ColumnDefinition? highColumn;
    private Canvas? marker;
    private FrameworkElement? markerDot;
    private Canvas? ticks;
    private TextBlock? elevatedTick;
    private TextBlock? highTick;

    static Gauge()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Gauge), new FrameworkPropertyMetadata(typeof(Gauge)));
    }

    public double? Value
    {
        get => (double?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double ElevatedFrom
    {
        get => (double)GetValue(ElevatedFromProperty);
        set => SetValue(ElevatedFromProperty, value);
    }

    public double HighAbove
    {
        get => (double)GetValue(HighAboveProperty);
        set => SetValue(HighAboveProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (ticks is not null)
        {
            ticks.SizeChanged -= OnTicksResized;
        }

        if (marker is not null)
        {
            marker.SizeChanged -= OnTicksResized;
        }

        goodColumn = GetTemplateChild("PART_Good") as ColumnDefinition;
        elevatedColumn = GetTemplateChild("PART_Elevated") as ColumnDefinition;
        highColumn = GetTemplateChild("PART_High") as ColumnDefinition;
        marker = GetTemplateChild("PART_Marker") as Canvas;
        markerDot = GetTemplateChild("PART_MarkerDot") as FrameworkElement;
        elevatedTick = GetTemplateChild("PART_ElevatedTick") as TextBlock;
        highTick = GetTemplateChild("PART_HighTick") as TextBlock;
        ticks = GetTemplateChild("PART_Ticks") as Canvas;

        if (ticks is not null)
        {
            ticks.SizeChanged += OnTicksResized;
        }

        if (marker is not null)
        {
            marker.SizeChanged += OnTicksResized;
        }

        UpdateParts();
    }

    private static void OnScaleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((Gauge)sender).UpdateParts();
    }

    private double Fraction(double value)
    {
        var span = Maximum - Minimum;

        return span <= 0 ? 0 : Math.Clamp((value - Minimum) / span, 0, 1);
    }

    private void UpdateParts()
    {
        var elevated = Fraction(ElevatedFrom);
        var high = Fraction(HighAbove);

        SetWidth(goodColumn, elevated);
        SetWidth(elevatedColumn, Math.Max(0, high - elevated));
        SetWidth(highColumn, 1 - high);

        SetText(elevatedTick, ElevatedFrom);
        SetText(highTick, HighAbove);
        PlaceTicks();
        PlaceMarker();
    }

    private static void SetWidth(ColumnDefinition? column, double fraction)
    {
        if (column is not null)
        {
            column.Width = new GridLength(fraction, GridUnitType.Star);
        }
    }

    private static void SetText(TextBlock? block, double number)
    {
        if (block is not null)
        {
            block.Text = number.ToString(NumberFormat.WholeFormat, CultureInfo.CurrentUICulture);
        }
    }

    private void OnTicksResized(object sender, SizeChangedEventArgs args)
    {
        PlaceTicks();
        PlaceMarker();
    }

    private void PlaceMarker()
    {
        if (marker is null || markerDot is null || marker.ActualWidth <= 0)
        {
            return;
        }

        var fraction = Value is { } value ? Fraction(value) : 0;
        Canvas.SetLeft(markerDot, (fraction * marker.ActualWidth) - (markerDot.Width / 2));
    }

    private void PlaceTicks()
    {
        if (ticks is null || ticks.ActualWidth <= 0)
        {
            return;
        }

        Place(elevatedTick, Fraction(ElevatedFrom));
        Place(highTick, Fraction(HighAbove));
    }

    private void Place(TextBlock? block, double fraction)
    {
        if (block is null || ticks is null)
        {
            return;
        }

        block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(block, (fraction * ticks.ActualWidth) - (block.DesiredSize.Width / 2));
    }
}
