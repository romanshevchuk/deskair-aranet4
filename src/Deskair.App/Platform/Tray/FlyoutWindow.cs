using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Deskair.App.Ui.Motion;
using Deskair.App.Ui.Views.Flyout;

namespace Deskair.App.Platform.Tray;

/// <summary>
/// The borderless, transparent window the flyout lives in. It stays out of the taskbar and above other windows, and goes away when it loses the focus
/// (a click anywhere else) or on Esc. It is created once and shown again each time; <see cref="Dismissed"/> tells the owner when it went away.
/// </summary>
public sealed class FlyoutWindow : Window
{
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

    /// <summary>A short fade when the flyout opens (<c>Motion.FlyoutOpen</c>); none while animations are off.</summary>
    private void FadeIn()
    {
        var duration = MotionPolicy.Of("Motion.FlyoutOpen");

        if (duration.TimeSpan > TimeSpan.Zero)
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration) { FillBehavior = FillBehavior.Stop });
        }
    }

    private void Dismiss()
    {
        if (IsVisible)
        {
            Hide();
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
