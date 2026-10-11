namespace Deskair.Services.Alerts;

public interface INotifier
{
    Task NotifyAsync(AlertNotification notification, CancellationToken cancellationToken);
}
