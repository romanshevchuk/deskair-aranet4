using CommunityToolkit.Mvvm.Messaging;
using Deskair.Services.Messages;

namespace Deskair.Services.Storage;

/// <summary>Remembers whether the saved readings could be read the last time they were asked for, and announces a change (Q27). Used from any thread.</summary>
public sealed class HistoryReadMonitor
{
    private readonly IMessenger messenger;
    private readonly Lock gate = new();
    private bool unreadable;

    public HistoryReadMonitor(IMessenger messenger)
    {
        this.messenger = messenger;
    }

    public bool IsUnreadable
    {
        get
        {
            lock (gate)
            {
                return unreadable;
            }
        }
    }

    public void ReportFailure() => Set(true);

    public void ReportSuccess() => Set(false);

    private void Set(bool value)
    {
        lock (gate)
        {
            if (unreadable == value)
            {
                return;
            }

            unreadable = value;
        }

        messenger.Send(new HistoryReadabilityChanged(value));
    }
}
