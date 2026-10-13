using Deskair.App.Localization;
using Deskair.Domain.Metrics;
using Deskair.Services.Alerts;

namespace Deskair.App.Platform.Notifications;

/// <summary>Puts an alert into words: "CO₂ is elevated · Office", "1,068 ppm. Open a window for about 10 minutes." (a short body of its own for each zone, so it does not repeat the title) and the two button labels, in the selected language.</summary>
public sealed class ToastTextBuilder
{
    private readonly ILocalizer localizer;
    private readonly MetricFormatter formatter;

    public ToastTextBuilder(ILocalizer localizer, MetricFormatter formatter)
    {
        this.localizer = localizer;
        this.formatter = formatter;
    }

    public ToastText Build(AlertNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var zone = notification.Zone;
        var value = $"{formatter.Value(Metric.Co2, notification.Co2Ppm)} {formatter.Unit(Metric.Co2)}";

        return new ToastText(
            localizer.Format($"Toast_{zone}_Title", ("room", notification.RoomName)),
            localizer.Format($"Toast_{zone}_Body", ("value", value)),
            localizer.Get("Toast_Snooze"),
            localizer.Get("Toast_Dismiss"));
    }
}
