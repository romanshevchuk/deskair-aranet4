using Deskair.App.ViewModels.Shell;
using Deskair.Services.Alerts;
using Deskair.Services.Platform;
using Microsoft.Extensions.Logging;

namespace Deskair.App.Platform.Notifications;

/// <summary>Does what the user asked for on a toast: Snooze mutes alerts for an hour, pressing the toast opens the main window.</summary>
public sealed class ToastActionHandler
{
    /// <summary>The length of "Snooze 1 h".</summary>
    public static readonly TimeSpan SnoozeFor = TimeSpan.FromHours(1);

    private readonly AlertEngine engine;
    private readonly IMainWindowController windows;
    private readonly TimeProvider timeProvider;
    private readonly IUiDispatcher dispatcher;
    private readonly ILogger<ToastActionHandler> logger;

    public ToastActionHandler(
        AlertEngine engine,
        IMainWindowController windows,
        TimeProvider timeProvider,
        IUiDispatcher dispatcher,
        ILogger<ToastActionHandler> logger)
    {
        this.engine = engine;
        this.windows = windows;
        this.timeProvider = timeProvider;
        this.dispatcher = dispatcher;
        this.logger = logger;
    }

    /// <summary>Called with the arguments of a click, from any thread. Arguments that are not ours (the Windows dismiss button sends none) are ignored.</summary>
    public void Handle(string arguments)
    {
        switch (ToastArguments.Parse(arguments))
        {
            case ToastAction.Snooze:
                engine.SnoozeUntil(timeProvider.GetUtcNow() + SnoozeFor);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Alerts snoozed for {Duration} from a toast.", SnoozeFor);
                }

                break;
            case ToastAction.Open:
                dispatcher.Post(() => windows.Show());
                break;
            default:
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Ignored toast arguments '{Arguments}'.", arguments);
                }

                break;
        }
    }
}
