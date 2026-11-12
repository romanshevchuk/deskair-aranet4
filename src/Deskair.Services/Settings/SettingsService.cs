using CommunityToolkit.Mvvm.Messaging;
using Deskair.Services.Messages;

namespace Deskair.Services.Settings;

/// <summary>Typed settings with change notifications.</summary>
public sealed class SettingsService
{
    private readonly ISettingsStore store;
    private readonly IMessenger messenger;

    public SettingsService(ISettingsStore store, IMessenger messenger)
    {
        this.store = store;
        this.messenger = messenger;
        Current = AppSettings.Default;
    }

    public AppSettings Current { get; private set; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        Current = await store.LoadAsync(cancellationToken);
    }

    public async Task UpdateAsync(Func<AppSettings, AppSettings> change, CancellationToken cancellationToken)
    {
        var updated = change(Current);

        if (updated == Current)
        {
            return;
        }

        await store.SaveAsync(updated, cancellationToken);
        Current = updated;
        messenger.Send(new SettingsChanged(updated));
    }

    /// <summary>
    /// Stores where the main window is. No <see cref="SettingsChanged"/> is sent: nothing follows the window's place, and the message would make
    /// the open screens reload each time the window hides.
    /// </summary>
    public async Task SaveMainWindowPlaceAsync(WindowPlace place, CancellationToken cancellationToken)
    {
        var updated = Current with { MainWindowPlace = place };

        if (updated == Current)
        {
            return;
        }

        await store.SaveAsync(updated, cancellationToken);
        Current = updated;
    }
}
