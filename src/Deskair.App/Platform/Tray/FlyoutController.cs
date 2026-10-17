using System.IO;
using System.Windows;
using System.Windows.Interop;
using Deskair.App.Ui.Views.Flyout;
using Deskair.App.ViewModels.Flyout;
using Deskair.Domain.Readings;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Queries;
using Microsoft.Extensions.Logging;

namespace Deskair.App.Platform.Tray;

/// <summary>
/// Opens and closes the tray flyout. A click on the tray icon opens it next to the icon with fresh numbers, or closes it when it is open.
/// The position comes from where the click happened: that point is on the taskbar, so <see cref="FlyoutPositioner"/> finds the edge from it.
/// Used from the UI thread.
/// </summary>
public sealed class FlyoutController
{
    private static readonly TimeSpan ribbonLength = TimeSpan.FromHours(24);
    private static readonly TimeSpan toggleWindow = TimeSpan.FromMilliseconds(400);
    private const double IconSize = 24;

    private readonly FlyoutViewModel viewModel;
    private readonly ReadingQueryService query;
    private readonly DeviceRegistry devices;
    private readonly ConnectionTracker connection;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<FlyoutController> logger;
    private readonly ToggleGuard guard;
    private FlyoutWindow? window;
    private ScreenPoint? anchor;

    public FlyoutController(
        FlyoutViewModel viewModel,
        ReadingQueryService query,
        DeviceRegistry devices,
        ConnectionTracker connection,
        TimeProvider timeProvider,
        ILogger<FlyoutController> logger)
    {
        this.viewModel = viewModel;
        this.query = query;
        this.devices = devices;
        this.connection = connection;
        this.timeProvider = timeProvider;
        this.logger = logger;
        guard = new ToggleGuard(timeProvider, toggleWindow);
    }

    /// <summary>Opens the flyout, or closes it when it is open. A failure is logged and the flyout stays closed.</summary>
    public async Task ToggleAsync()
    {
        try
        {
            if (window is { IsVisible: true })
            {
                window.Close();
                return;
            }

            if (!guard.ShouldShow())
            {
                return;
            }

            anchor = ScreenPoint.AtCursor();

            await RefreshAsync();

            window ??= CreateWindow();
            Place(window, anchor);
            window.PrepareToShow();
            window.Show();
            window.Activate();
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(exception, "Opening the flyout failed.");
            }
        }
    }

    private FlyoutWindow CreateWindow()
    {
        var created = new FlyoutWindow(new FlyoutView { DataContext = viewModel });
        created.Dismissed += (_, _) => guard.NotifyHidden();

        // A new window for every opening: a hidden transparent window shows its last picture for a frame when it is shown again, which looked like a blink.
        created.Closed += (_, _) =>
        {
            if (ReferenceEquals(window, created))
            {
                window = null;
            }
        };

        // The first time, the content has no real size until the window is on screen, so the position worked out before showing is too low.
        // Place it again as soon as the window knows its size; moving without resizing does not raise this event again.
        created.SizeChanged += (_, _) =>
        {
            if (created.IsVisible)
            {
                Place(created, anchor);
            }
        };

        return created;
    }

    private async Task RefreshAsync()
    {
        var now = timeProvider.GetUtcNow();
        var selected = devices.Selected;
        var room = selected is { } id ? devices.Info(id)?.RoomName : null;

        if (selected is not { } device || connection.Current is ConnectionState.FirstRun)
        {
            viewModel.Update(null, [], now, room);
            return;
        }

        try
        {
            var recent = await query.GetRecentAsync(device, ribbonLength, CancellationToken.None);

            // Not connected: no current reading, but the last of the day is shown faded.
            Reading? current = connection.Current is ConnectionState.NotConnected || recent.Count == 0 ? null : recent[^1];

            viewModel.Update(current, recent, now, room);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Reading the last 24 hours for the flyout failed.");
            }

            viewModel.Update(null, [], now, room);
        }
    }

    /// <summary>Moves the (hidden) window so the card sits next to the taskbar edge nearest the click. Works in pixels at the end, because that is what Windows places by on any monitor scale.</summary>
    private static void Place(FlyoutWindow flyout, ScreenPoint? anchor)
    {
        var content = (FrameworkElement)flyout.Content;
        content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var shadow = FlyoutWindow.ShadowMargin;
        var card = new Size(content.DesiredSize.Width - shadow.Left - shadow.Right, content.DesiredSize.Height - shadow.Top - shadow.Bottom);
        var point = anchor ?? FallbackAnchor();
        var position = FlyoutPositioner.Position(point.IconRectangle(IconSize), card, point.WorkArea);
        var handle = new WindowInteropHelper(flyout).EnsureHandle();

        NativeMethods.SetWindowPos(
            handle,
            0,
            (int)Math.Round((position.X - shadow.Left) * point.Scale),
            (int)Math.Round((position.Y - shadow.Top) * point.Scale),
            0,
            0,
            NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate);
    }

    /// <summary>When Windows gives no cursor position: the bottom right of the main monitor, where the tray usually is.</summary>
    private static ScreenPoint FallbackAnchor()
    {
        var work = SystemParameters.WorkArea;

        return new ScreenPoint(new Point(work.Right - (IconSize / 2), work.Bottom + (IconSize / 2)), work, 1);
    }
}
