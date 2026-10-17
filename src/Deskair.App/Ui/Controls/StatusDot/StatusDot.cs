using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Deskair.App.Ui.Motion;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Controls;

/// <summary>A small dot in the zone colour. Without a zone it is neutral grey (for example "no data"). With <see cref="Pulses"/> set, it pulses slowly in the High zone while animations are on.</summary>
public class StatusDot : Control
{
    /// <summary>The lowest opacity of the pulse.</summary>
    public const double PulseLow = 0.4;

    public static readonly DependencyProperty ZoneProperty = DependencyProperty.Register(
        nameof(Zone),
        typeof(Zone?),
        typeof(StatusDot),
        new PropertyMetadata(null, (sender, _) => ((StatusDot)sender).UpdatePulse()));

    public static readonly DependencyProperty PulsesProperty = DependencyProperty.Register(
        nameof(Pulses),
        typeof(bool),
        typeof(StatusDot),
        new PropertyMetadata(false, (sender, _) => ((StatusDot)sender).UpdatePulse()));

    static StatusDot()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(StatusDot), new FrameworkPropertyMetadata(typeof(StatusDot)));
    }

    public StatusDot()
    {
        Loaded += (_, _) => UpdatePulse();
        Unloaded += (_, _) => BeginAnimation(OpacityProperty, null);
    }

    public Zone? Zone
    {
        get => (Zone?)GetValue(ZoneProperty);
        set => SetValue(ZoneProperty, value);
    }

    /// <summary>True on the dot in the hero pill, the one place that pulses.</summary>
    public bool Pulses
    {
        get => (bool)GetValue(PulsesProperty);
        set => SetValue(PulsesProperty, value);
    }

    private void UpdatePulse()
    {
        BeginAnimation(OpacityProperty, null);

        var duration = MotionPolicy.Of("Motion.Pulse");

        if (!IsLoaded || !Pulses || Zone != Domain.Zones.Zone.High || duration.TimeSpan == TimeSpan.Zero)
        {
            return;
        }

        BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(1, PulseLow, new Duration(duration.TimeSpan / 2)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
    }
}
