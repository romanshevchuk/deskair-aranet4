using CommunityToolkit.Mvvm.Messaging;
using Deskair.Services.Messages;
using Microsoft.Win32;

namespace Deskair.App.Platform.Power;

/// <summary>
/// Sends <see cref="SystemResumed"/> when Windows wakes from sleep, so the app connects to the sensor again and syncs. Windows can report a resume more than once
/// in a few seconds; only the first is announced.
/// </summary>
public sealed class PowerEvents : IDisposable
{
    private static readonly TimeSpan burstWindow = TimeSpan.FromSeconds(10);

    private readonly IMessenger messenger;
    private readonly TimeProvider timeProvider;
    private readonly Lock gate = new();
    private DateTimeOffset? lastResume;
    private bool subscribed;
    private bool disposed;

    public PowerEvents(IMessenger messenger, TimeProvider timeProvider)
    {
        this.messenger = messenger;
        this.timeProvider = timeProvider;
    }

    /// <summary>Starts listening to the system's power-mode event.</summary>
    public void Start()
    {
        lock (gate)
        {
            if (subscribed || disposed)
            {
                return;
            }

            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            subscribed = true;
        }
    }

    /// <summary>What to do for a power-mode change. Public so tests can drive it without putting the PC to sleep.</summary>
    public void Handle(PowerModes mode)
    {
        if (mode != PowerModes.Resume)
        {
            return;
        }

        lock (gate)
        {
            var now = timeProvider.GetUtcNow();

            if (disposed || (lastResume is { } previous && now - previous < burstWindow))
            {
                return;
            }

            lastResume = now;
        }

        messenger.Send(new SystemResumed());
    }

    public void Dispose()
    {
        lock (gate)
        {
            disposed = true;

            if (subscribed)
            {
                SystemEvents.PowerModeChanged -= OnPowerModeChanged;
                subscribed = false;
            }
        }
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        Handle(e.Mode);
    }
}
