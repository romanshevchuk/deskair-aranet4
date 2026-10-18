using System.Runtime.InteropServices;
using Deskair.Services.Alerts;
using Deskair.Services.Platform;
using Microsoft.Extensions.Logging;
using Windows.Data.Xml.Dom;
using WindowsToasts = Windows.UI.Notifications;

namespace Deskair.App.Platform.Notifications;

/// <summary>
/// Shows an alert as a Windows toast (docs/plans/spike-s5-toasts.md). Windows decides whether to show it: Do Not Disturb, Focus assist and the user's notification settings
/// for Deskair all apply, and nothing here overrides them. A newer toast for the same room replaces the older one in the notification centre.
/// </summary>
public sealed class ToastNotifier : INotifier
{
    private const string Group = "alerts";
    private const int MaxTagLength = 64;

    private readonly ToastTextBuilder texts;
    private readonly ToastLogos logos;
    private readonly IPackageIdentity packageIdentity;
    private readonly ILogger<ToastNotifier> logger;

    public ToastNotifier(ToastTextBuilder texts, ToastLogos logos, IPackageIdentity packageIdentity, ILogger<ToastNotifier> logger)
    {
        this.packageIdentity = packageIdentity;
        this.texts = texts;
        this.logos = logos;
        this.logger = logger;
    }

    public async Task NotifyAsync(AlertNotification notification, CancellationToken cancellationToken)
    {
        var logo = await logos.UriForAsync(notification.Zone);
        var document = new XmlDocument();
        var tag = notification.Device.Address.Length > MaxTagLength ? notification.Device.Address[..MaxTagLength] : notification.Device.Address;

        document.LoadXml(ToastXmlBuilder.Build(texts.Build(notification), logo, tag));

        var toast = new WindowsToasts.ToastNotification(document) { Tag = tag, Group = Group };
        // A packaged app is known to Windows by its package, so it names no identity of its own.
        var notifier = packageIdentity.IsPackaged
            ? WindowsToasts.ToastNotificationManager.CreateToastNotifier()
            : WindowsToasts.ToastNotificationManager.CreateToastNotifier(ToastIdentity.AppUserModelId);

        LogIfDisabled(notifier);
        notifier.Show(toast);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Toast shown for {Room}: {Zone} at {Ppm} ppm.", notification.RoomName, notification.Zone, notification.Co2Ppm);
        }
    }

    /// <summary>The setting is unknown to Windows before the first toast (reading it fails then), which is not a problem.</summary>
    private void LogIfDisabled(WindowsToasts.ToastNotifier notifier)
    {
        try
        {
            if (notifier.Setting != WindowsToasts.NotificationSetting.Enabled && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Windows will not show Deskair's toasts: {Setting}.", notifier.Setting);
            }
        }
        catch (COMException)
        {
            // Not known yet.
        }
    }
}
