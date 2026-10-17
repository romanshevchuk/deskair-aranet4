using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Deskair.App.Ui.Motion;
using Deskair.App.Ui.Views.Flyout;

namespace Deskair.App.Platform.Tray;

/// <summary>
/// The borderless, transparent window the flyout lives in. It stays out of the taskbar and above other windows, and goes away when it loses the focus
/// (a click anywhere else) or on Esc. A new one is made for each opening; <see cref="Dismissed"/> tells the owner when it went away.
/// </summary>
public sealed class FlyoutWindow : Window
{
    private bool closing;

    public FlyoutWindow(FlyoutView view)
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        Content = view;
        Title = "Deskair";

        Deactivated += (_, _) => Dismiss();
        PreviewKeyDown += OnKeyDown;
        IsVisibleChanged += (_, args) =>
        {
            if (args.NewValue is false)
            {
                Dismissed?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                FadeIn();
            }
        };
    }

    /// <summary>Raised whenever the window stops being visible.</summary>
    public event EventHandler? Dismissed;

    /// <summary>The room around the card that the shadow needs. The card, not the window, is what has to fit the work area.</summary>
    public static Thickness ShadowMargin => (Thickness)Application.Current.FindResource("Space.Menu.Shadow");

    /// <summary>Call before <see cref="Window.Show"/>: while animations are on, the window starts see-through so its first frame is never the finished card (that showed as a blink); otherwise it starts opaque.</summary>
    public void PrepareToShow()
    {
        BeginAnimation(OpacityProperty, null);
        Opacity = MotionPolicy.Of("Motion.FlyoutOpen").TimeSpan > TimeSpan.Zero ? 0 : 1;
    }

    /// <summary>A short fade when the flyout opens (<c>Motion.FlyoutOpen</c>); none while animations are off.</summary>
    private void FadeIn()
    {
        var duration = MotionPolicy.Of("Motion.FlyoutOpen");

        if (duration.TimeSpan <= TimeSpan.Zero)
        {
            return;
        }

        var fade = new DoubleAnimation(0, 1, duration);
        fade.Completed += (_, _) =>
        {
            BeginAnimation(OpacityProperty, null);
            Opacity = 1;
        };
        BeginAnimation(OpacityProperty, fade);
    }
    /// <summary>Losing the focus while the window is already closing must not close it a second time.</summary>
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        closing = true;
        base.OnClosing(e);
    }


    private void Dismiss()
    {
        if (IsVisible && !closing)
        {
            Close();
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Dismiss();
        }
    }
}
