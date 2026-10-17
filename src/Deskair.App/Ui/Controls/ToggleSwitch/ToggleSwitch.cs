using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using Deskair.App.Ui.Motion;

namespace Deskair.App.Ui.Controls;

/// <summary>An on/off switch (38 × 22 px). The optional content is its label. The knob slides while animations are on.</summary>
public class ToggleSwitch : ToggleButton
{
    static ToggleSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
    }

    protected override void OnChecked(RoutedEventArgs e)
    {
        base.OnChecked(e);
        SlideKnob("Space.Toggle.KnobOff", "Space.Toggle.KnobOn");
    }

    protected override void OnUnchecked(RoutedEventArgs e)
    {
        base.OnUnchecked(e);
        SlideKnob("Space.Toggle.KnobOn", "Space.Toggle.KnobOff");
    }

    /// <summary>The template already puts the knob at its new place; the animation only plays the way there, then lets go.</summary>
    private void SlideKnob(string fromKey, string toKey)
    {
        var duration = MotionPolicy.Of("Motion.Toggle");

        if (!IsLoaded || duration.TimeSpan == TimeSpan.Zero || GetTemplateChild("Knob") is not FrameworkElement knob || TryFindResource(fromKey) is not Thickness from || TryFindResource(toKey) is not Thickness to)
        {
            return;
        }

        knob.BeginAnimation(FrameworkElement.MarginProperty, new ThicknessAnimation(from, to, duration) { FillBehavior = FillBehavior.Stop });
    }
}
