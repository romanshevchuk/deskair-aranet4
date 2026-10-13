using Deskair.App.ViewModels.Shell;
using Deskair.Services.Alerts;
using Deskair.Services.Platform;
using Microsoft.Extensions.Logging;

namespace Deskair.App.Platform.Notifications;

/// <summary>Does what the user asked for on a toast: Snooze mutes alerts for an hour, pressing the toast opens the main window.</summary>
public sealed class ToastActionHandler
{
    private readonly SnoozeService snooze;
    private readonly IMainWindowController windows;
    private readonly IUiDispatcher dispatcher;
    private readonly ILogger<ToastActionHandler> logger;

    public ToastActionHandler(
        SnoozeService snooze,
        IMainWindowController windows,
        IUiDispatcher dispatcher,
        ILogger<ToastActionHandler> logger)
    {
        this.snooze = snooze;
        this.windows = windows;
        this.dispatcher = dispatcher;
        this.logger = logger;
    }

    /// <summary>Called with the arguments of a click, from any thread. Arguments that are not ours (the Windows dismiss button sends none) are ignored.</summary>
    public void Handle(string arguments)
    {
        switch (ToastArguments.Parse(arguments))
        {
            case ToastAction.Snooze:
                snooze.ForOneHour();

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Alerts snoozed for an hour from a toast.");
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
