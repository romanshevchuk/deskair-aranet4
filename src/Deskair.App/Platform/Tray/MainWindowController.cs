using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Deskair.App.ViewModels.Shell;

namespace Deskair.App.Platform.Tray;

/// <summary>
/// Owns the one main window for the tray: shows it on request, and (once the tray icon exists) turns closing it into hiding it, so closing the window
/// keeps the app running in the tray. Quit is the only way to end the app. Used from the UI thread.
/// </summary>
public sealed class MainWindowController : IMainWindowController
{
    private readonly ShellViewModel shell;
    private readonly Action shutdown;
    private Window? window;
    private bool quitting;

    public MainWindowController(ShellViewModel shell)
        : this(shell, () => Application.Current.Shutdown())
    {
    }

    /// <summary>For tests: another way to end the app, so the test application keeps running.</summary>
    public MainWindowController(ShellViewModel shell, Action shutdown)
    {
        this.shell = shell;
        this.shutdown = shutdown;
    }

    public void Attach(Window mainWindow)
    {
        window = mainWindow;
    }

    /// <summary>From now on closing the window only hides it. Call this once the tray icon is there to bring it back.</summary>
    public void KeepInTray()
    {
        if (window is not null)
        {
            window.Closing += OnClosing;
        }
    }

    public void Show(ShellSection? section = null)
    {
        if (window is null)
        {
            return;
        }

        if (section is { } target)
        {
            shell.Section = target;
        }

        if (!window.IsVisible)
        {
            window.Show();
        }

        Restore(window);
        window.Activate();

        // The flyout that asked for this closes while it runs; look again once that is over, in case the restore did not hold.
        window.Dispatcher.BeginInvoke(() => Restore(window), DispatcherPriority.Background);
    }

    public void Quit()
    {
        quitting = true;
        shutdown();
    }

    // WPF's WindowState can disagree with the real window (for example after Show desktop), and then setting it to Normal does nothing; ask Windows too.
    private static void Restore(Window target)
    {
        var handle = new WindowInteropHelper(target).Handle;
        var minimized = target.WindowState == WindowState.Minimized || (handle != 0 && NativeMethods.IsIconic(handle));

        if (!minimized)
        {
            return;
        }

        target.WindowState = WindowState.Normal;

        if (handle != 0 && NativeMethods.IsIconic(handle))
        {
            _ = NativeMethods.ShowWindow(handle, NativeMethods.SwRestore);
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (quitting)
        {
            return;
        }

        e.Cancel = true;
        window?.Hide();
    }
}
