using System.Windows;
using Deskair.App.Platform.Tray;
using Deskair.App.ViewModels.Shell;
using Deskair.Services.Settings;

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
        new MainWindowController(new ShellViewModel(), () => { }).Show(ShellSection.Settings);
    }
}
