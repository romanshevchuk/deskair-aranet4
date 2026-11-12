using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Platform.Tray;
using Deskair.App.ViewModels.Shell;
using Deskair.Services.Devices;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Ui;

public class MainWindowControllerTests
{
    private static T Run<T>(Func<T> work) => UiHost.Run(ThemeChoice.Light, work);

    [Fact]
    public void Show_opens_a_hidden_window_and_goes_to_the_section()
    {
        var result = Run(() =>
        {
            var shell = new ShellViewModel();
            var controller = new MainWindowController(shell, () => { });
            var window = new Window { ShowActivated = false, Width = 100, Height = 100 };
            controller.Attach(window);

            controller.Show(ShellSection.History);
            var visible = window.IsVisible;
            window.Close();

            return (visible, shell.Section);
        });

        Assert.True(result.visible);
        Assert.Equal(ShellSection.History, result.Section);
    }

    [Fact]
    public void Show_without_a_section_keeps_the_section()
    {
        var section = Run(() =>
        {
            var shell = new ShellViewModel { Section = ShellSection.Settings };
            var controller = new MainWindowController(shell, () => { });
            var window = new Window { ShowActivated = false, Width = 100, Height = 100 };
            controller.Attach(window);

            controller.Show();
            window.Close();

            return shell.Section;
        });

        Assert.Equal(ShellSection.Settings, section);
    }

    [Fact]
    public void Closing_the_window_hides_it_once_the_tray_keeps_it()
    {
        var result = Run(() =>
        {
            var controller = new MainWindowController(new ShellViewModel(), () => { });
            var window = new Window { ShowActivated = false, Width = 100, Height = 100 };
            var closed = false;
            window.Closed += (_, _) => closed = true;
            controller.Attach(window);
            controller.KeepInTray();
            controller.Show();

            window.Close();
            var hidden = !window.IsVisible;
            controller.Show();
            var shownAgain = window.IsVisible;
            controller.Quit();
            window.Close();

            return (hidden, shownAgain, closed);
        });

        Assert.True(result.hidden);
        Assert.True(result.shownAgain);
        Assert.True(result.closed);
    }

    [Fact]
    public void Without_the_tray_closing_the_window_really_closes_it()
    {
        var closed = Run(() =>
        {
            var controller = new MainWindowController(new ShellViewModel(), () => { });
            var window = new Window { ShowActivated = false, Width = 100, Height = 100 };
            var wasClosed = false;
            window.Closed += (_, _) => wasClosed = true;
            controller.Attach(window);
            controller.Show();

            window.Close();

            return wasClosed;
        });

        Assert.True(closed);
    }

    [Fact]
    public void Quit_ends_the_app_and_lets_the_window_close()
    {
        var result = Run(() =>
        {
            var shutdowns = 0;
            var controller = new MainWindowController(new ShellViewModel(), () => shutdowns++);
            var window = new Window { ShowActivated = false, Width = 100, Height = 100 };
            var closed = false;
            window.Closed += (_, _) => closed = true;
            controller.Attach(window);
            controller.KeepInTray();
            controller.Show();

            controller.Quit();
            window.Close();

            return (shutdowns, closed);
        });

        Assert.Equal(1, result.shutdowns);
        Assert.True(result.closed);
    }

    [Fact]
    public void Show_before_a_window_is_attached_does_nothing()
    {
        var controller = new MainWindowController(new ShellViewModel(), () => { });

        Assert.Null(Record.Exception(() => controller.Show(ShellSection.Settings)));
    }

    [Fact]
    public void The_footer_is_told_when_the_window_is_shown_or_hidden()
    {
        var dispatcher = new FakeUiDispatcher();
        var postedAfterShow = Run(() =>
        {
            var footer = new FooterViewModel(
                new DeviceRegistry(new FakeDeviceStore()),
                new StrongReferenceMessenger(),
                dispatcher,
                new FakeClock(Samples.Utc(2026, 10, 12, 12)),
                new FakeLocalizer());
            var controller = new MainWindowController(new ShellViewModel { Footer = footer }, () => { });
            var window = new Window { ShowActivated = false, Width = 100, Height = 100 };
            controller.Attach(window);
            controller.KeepInTray();

            // Attached but not shown yet: nothing to refresh. Showing it refreshes once.
            var postedBeforeShow = dispatcher.Posted;
            controller.Show();
            var posted = (before: postedBeforeShow, shown: dispatcher.Posted);

            // Hidden to the tray and shown again: one more refresh.
            window.Close();
            controller.Show();
            var postedAgain = dispatcher.Posted;
            controller.Quit();
            window.Close();

            return (posted, postedAgain);
        });

        Assert.Equal(0, postedAfterShow.posted.before);
        Assert.Equal(1, postedAfterShow.posted.shown);
        Assert.Equal(2, postedAfterShow.postedAgain);
    }

    [Fact]
    public void Show_restores_a_window_that_Windows_minimized()
    {
        var restored = Run(() =>
        {
            var controller = new MainWindowController(new ShellViewModel(), () => { });
            var window = new Window { ShowActivated = false, Width = 100, Height = 100 };
            controller.Attach(window);
            controller.Show();
            var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;

            // Minimized by Windows itself, not through window.WindowState.
            _ = ShowWindow(handle, 6);
            var wasMinimized = IsIconic(handle);

            controller.Show();
            var stillMinimized = IsIconic(handle);
            window.Close();

            return (wasMinimized, stillMinimized);
        });

        Assert.True(restored.wasMinimized);
        Assert.False(restored.stillMinimized);
    }

    [Fact]
    public void Show_brings_back_a_maximized_window_maximized_after_it_was_minimized()
    {
        var states = Run(() =>
        {
            var controller = new MainWindowController(new ShellViewModel(), () => { });
            var window = new Window { ShowActivated = false, Width = 100, Height = 100 };
            controller.Attach(window);
            controller.Show();
            window.WindowState = WindowState.Maximized;
            var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;

            _ = ShowWindow(handle, 6);
            controller.Show();
            var afterWindowsMinimized = window.WindowState;

            window.WindowState = WindowState.Minimized;
            controller.Show();
            var afterWpfMinimized = window.WindowState;
            window.Close();

            return (afterWindowsMinimized, afterWpfMinimized);
        });

        Assert.Equal(WindowState.Maximized, states.afterWindowsMinimized);
        Assert.Equal(WindowState.Maximized, states.afterWpfMinimized);
    }

    [Fact]
    public void Show_keeps_the_place_of_a_window_that_was_minimized_or_hidden()
    {
        var places = Run(() =>
        {
            var controller = new MainWindowController(new ShellViewModel(), () => { });
            var window = new Window { ShowActivated = false, Width = 300, Height = 200, Left = 120, Top = 90 };
            controller.Attach(window);
            controller.KeepInTray();
            controller.Show();
            var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;

            _ = ShowWindow(handle, 6);
            controller.Show();
            var afterMinimized = (window.Left, window.Top);

            window.WindowState = WindowState.Minimized;
            window.Close();
            controller.Show();
            var afterHiddenMinimized = (window.Left, window.Top, window.WindowState);
            controller.Quit();
            window.Close();

            return (afterMinimized, afterHiddenMinimized);
        });

        Assert.Equal((120d, 90d), places.afterMinimized);
        Assert.Equal((120d, 90d, WindowState.Normal), places.afterHiddenMinimized);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(nint window, int command);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool IsIconic(nint window);
}
