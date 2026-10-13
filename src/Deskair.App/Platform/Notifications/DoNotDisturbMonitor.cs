using CommunityToolkit.Mvvm.Messaging;
using Deskair.Services.Alerts;
using Deskair.Services.Messages;

namespace Deskair.App.Platform.Notifications;

/// <summary>
/// Whether Windows Do Not Disturb is on. <see cref="IsActive"/> asks Windows each time, so an alert never decides on an old answer; a periodic look announces changes as
/// <see cref="DoNotDisturbChanged"/> for the tray and Settings ("Alerts paused by Windows"). Windows holds native toasts itself while it is on; this only lets the app say so.
/// A state that cannot be read counts as not active (questions.md Q13).
/// </summary>
public sealed class DoNotDisturbMonitor : IDoNotDisturb, IDisposable
{
    /// <summary>How often the state is looked at to find changes.</summary>
    public static readonly TimeSpan PollEvery = TimeSpan.FromSeconds(3);

    private readonly IDoNotDisturbReader reader;
    private readonly IMessenger messenger;
    private readonly TimeProvider timeProvider;
    private readonly Lock gate = new();
    private ITimer? timer;
    private bool last;

    public DoNotDisturbMonitor(IDoNotDisturbReader reader, IMessenger messenger, TimeProvider timeProvider)
    {
        this.reader = reader;
        this.messenger = messenger;
        this.timeProvider = timeProvider;
    }

    public bool IsActive => reader.Read() ?? false;

    /// <summary>Starts looking for changes.</summary>
    public void Start()
    {
        lock (gate)
        {
            if (timer is not null)
            {
                return;
            }

            last = IsActive;
            timer = timeProvider.CreateTimer(_ => Look(), null, PollEvery, PollEvery);
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            timer?.Dispose();
            timer = null;
        }
    }

    private void Look()
    {
        bool now;

        lock (gate)
        {
            if (timer is null)
            {
                return;
            }

            now = IsActive;

            if (now == last)
            {
                return;
            }

            last = now;
        }

        messenger.Send(new DoNotDisturbChanged(now));
    }
}
