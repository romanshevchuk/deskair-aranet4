using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

/// <summary>A 26 x 12 battery with a fill proportional to <see cref="Percent"/>. Unknown draws an empty outline.</summary>
public class BatteryIcon : Control
{
    public const double FillMaximum = 19;
    public const double FillMinimum = 2;

    public static readonly DependencyProperty PercentProperty = DependencyProperty.Register(
        nameof(Percent),
        typeof(int?),
        typeof(BatteryIcon),
        new PropertyMetadata(null, OnPercentChanged));

    private static readonly DependencyPropertyKey fillWidthKey = DependencyProperty.RegisterReadOnly(
        nameof(FillWidth),
        typeof(double),
        typeof(BatteryIcon),
        new PropertyMetadata(0.0));

    private static readonly DependencyPropertyKey levelKey = DependencyProperty.RegisterReadOnly(
        nameof(Level),
        typeof(BatteryLevel),
        typeof(BatteryIcon),
        new PropertyMetadata(BatteryLevel.Unknown));

    public static readonly DependencyProperty FillWidthProperty = fillWidthKey.DependencyProperty;

    public static readonly DependencyProperty LevelProperty = levelKey.DependencyProperty;

    static BatteryIcon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BatteryIcon), new FrameworkPropertyMetadata(typeof(BatteryIcon)));
    }

    public int? Percent
    {
        get => (int?)GetValue(PercentProperty);
        set => SetValue(PercentProperty, value);
    }

    public double FillWidth => (double)GetValue(FillWidthProperty);

    public BatteryLevel Level => (BatteryLevel)GetValue(LevelProperty);

    private static void OnPercentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var icon = (BatteryIcon)sender;

        if (args.NewValue is not int percent)
        {
            icon.SetValue(fillWidthKey, 0.0);
            icon.SetValue(levelKey, BatteryLevel.Unknown);
            return;
        }

        var clamped = Math.Clamp(percent, 0, 100);

        icon.SetValue(fillWidthKey, Math.Max(FillMinimum, FillMaximum * clamped / 100));
        icon.SetValue(levelKey, clamped <= 10 ? BatteryLevel.Critical : clamped <= 20 ? BatteryLevel.Low : BatteryLevel.Good);
    }
}
