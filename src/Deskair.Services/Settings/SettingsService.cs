using CommunityToolkit.Mvvm.Messaging;

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

    // TODO(SET-01a) [S, haiku]
    // Spec: docs/engineering/architecture.md section 6 (SettingsService).
    // Input : none.
    // Output: loads settings from ISettingsStore into Current.
    // Rules : does not send SettingsChanged (startup only).
    // Edge  : a store that returns defaults leaves Current at the defaults.
    // Done  : add Services/SettingsServiceTests.Load_* and make them pass.
    public Task LoadAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(SET-01a)");

    // TODO(SET-01b) [S, haiku]
    // Spec: docs/engineering/architecture.md section 6 (SettingsService); docs/guides/add-a-setting.md.
    // Input : a function that returns the changed settings.
    // Output: saves through ISettingsStore, updates Current, sends SettingsChanged.
    // Rules : save first; send the message only after a successful save.
    // Edge  : if the change produces an equal record, do nothing.
    // Done  : add Services/SettingsServiceTests.Update_* and make them pass.
    public Task UpdateAsync(Func<AppSettings, AppSettings> change, CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(SET-01b)");
}
