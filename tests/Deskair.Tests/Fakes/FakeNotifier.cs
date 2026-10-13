using Deskair.Services.Alerts;

namespace Deskair.Tests.Fakes;

/// <summary>A notifier that remembers every alert it was asked to show. Safe to read while a background feed adds to it.</summary>
public sealed class FakeNotifier : INotifier
{
    public ThreadSafeLog<AlertNotification> Sent { get; } = new();

    public Task NotifyAsync(AlertNotification notification, CancellationToken cancellationToken)
    {
        Sent.Add(notification);

        return Task.CompletedTask;
    }
}
