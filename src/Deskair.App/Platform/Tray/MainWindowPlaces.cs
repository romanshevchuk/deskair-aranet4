using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Deskair.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Deskair.App.Platform.Tray;

/// <summary>
/// Keeps the main window's place between launches: puts the window where it was before it is first shown (also when the app starts in the tray
/// and the window appears only later), and stores the place each time the window is hidden or closed. Uses Windows' placement calls, which work in
/// physical pixels and move a window whose monitor is gone back onto a screen. With nothing stored the window opens in the centre of the screen.
/// </summary>
public sealed class MainWindowPlaces
{
    private static readonly TimeSpan saveTimeout = TimeSpan.FromSeconds(2);

    private readonly SettingsService settings;
    private readonly ILogger<MainWindowPlaces> logger;

    public MainWindowPlaces(SettingsService settings, ILogger<MainWindowPlaces> logger)
    {
        this.settings = settings;
        this.logger = logger;
    }

    /// <summary>Puts the window in its stored place and starts storing it. Call once, before the window is first shown.</summary>
    public void Attach(Window window)
    {
        Restore(window, settings.Current.MainWindowPlace);

        var shownOnce = false;
        window.IsVisibleChanged += (_, _) => shownOnce |= window.IsVisible;
        window.Closing += (_, e) => OnClosing(window, e, shownOnce);
    }

    /// <summary>Reads where the window is now; null when Windows cannot tell (no handle yet).</summary>
    public static WindowPlace? Read(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var placement = NewPlacement();

        if (handle == 0 || !NativeMethods.GetWindowPlacement(handle, ref placement))
        {
            return null;
        }

        var normal = placement.normalPosition;
        var maximized = placement.showCmd == NativeMethods.SwShowMaximized
            || (placement.showCmd == NativeMethods.SwShowMinimized && (placement.flags & NativeMethods.WpfRestoreToMaximized) != 0)
            || window.WindowState == WindowState.Maximized;

        return new WindowPlace(normal.left, normal.top, normal.right - normal.left, normal.bottom - normal.top, maximized);
    }

    /// <summary>Puts a window that is not shown yet in the given place; with no place, it opens in the centre of the screen.</summary>
    public static void Restore(Window window, WindowPlace? place)
    {
        if (place is null)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        var handle = new WindowInteropHelper(window).EnsureHandle();
        var placement = NewPlacement();
        placement.showCmd = NativeMethods.SwHide;
        placement.normalPosition = new NativeMethods.NativeRect
        {
            left = place.Left,
            top = place.Top,
            right = place.Left + place.Width,
            bottom = place.Top + place.Height
        };

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        _ = NativeMethods.SetWindowPlacement(handle, ref placement);

        if (place.Maximized)
        {
            window.WindowState = WindowState.Maximized;
        }
    }

    private static NativeMethods.NativeWindowPlacement NewPlacement() => new()
    {
        length = Marshal.SizeOf<NativeMethods.NativeWindowPlacement>()
    };

    // Closing runs both when the window hides to the tray and when the app quits or Windows signs out, while the window still has its handle.
    // A window never shown has no place of its own yet, so nothing is stored for it.
    private void OnClosing(Window window, CancelEventArgs e, bool shownOnce)
    {
        if (!shownOnce || Read(window) is not { } place)
        {
            return;
        }

        try
        {
            // Wait here: on quit the app ends right after Closing, before an awaited save could finish. The file is a few hundred bytes.
            Task.Run(() => settings.SaveMainWindowPlaceAsync(place, CancellationToken.None)).Wait(saveTimeout);
        }
        catch (AggregateException exception) when (exception.InnerException is IOException or UnauthorizedAccessException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception.InnerException, "Storing the window's place failed.");
            }
        }
    }
}
