using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Deskair.App.Ui.Motion;

/// <summary>Attached to a <see cref="TextBlock"/>: when its text changes, the new text fades in from a dim start (<c>Motion.Fade</c>). Nothing happens while animations are off.</summary>
public static class FadeOnChange
{
    /// <summary>How visible the new text is when the fade starts.</summary>
    public const double StartOpacity = 0.35;

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(FadeOnChange),
        new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not TextBlock block)
        {
            return;
        }

        var descriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));

        if (args.NewValue is true)
        {
            descriptor.AddValueChanged(block, OnTextChanged);
        }
        else
        {
            descriptor.RemoveValueChanged(block, OnTextChanged);
        }
    }

    private static void OnTextChanged(object? sender, EventArgs args)
    {
        if (sender is not TextBlock { IsLoaded: true } block)
        {
            return;
        }

        var duration = MotionPolicy.Of("Motion.Fade");

        if (duration.TimeSpan == TimeSpan.Zero)
        {
            return;
        }

        block.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(StartOpacity, 1, duration) { FillBehavior = FillBehavior.Stop });
    }
}
