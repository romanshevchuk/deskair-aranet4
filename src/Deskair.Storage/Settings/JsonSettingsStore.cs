using Deskair.Services.Platform;
using Deskair.Services.Settings;

namespace Deskair.Storage.Settings;

/// <summary>Reads and writes settings.json in the data folder.</summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly IKnownFolders folders;

    public JsonSettingsStore(IKnownFolders folders)
    {
        this.folders = folders;
    }

    // TODO(SET-02a) [S, haiku]
    // Spec: docs/engineering/architecture.md section 6 (SettingsService); docs/guides/add-a-setting.md.
    // Input : none.
    // Output: the saved AppSettings.
    // Rules : System.Text.Json, enums as strings; unknown properties are ignored.
    // Edge  : a missing or unreadable file returns AppSettings.Default.
    // Done  : add Storage/JsonSettingsStoreTests.Load_* using a temp folder and make them pass.
    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(SET-02a)");

    // TODO(SET-02b) [S, haiku]
    // Spec: docs/engineering/data-model.md "Safety".
    // Input : AppSettings.
    // Output: settings.json written.
    // Rules : write a temp file and rename over the old one.
    // Edge  : a failure leaves the previous file untouched.
    // Done  : add Storage/JsonSettingsStoreTests.Save_* and make them pass.
    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(SET-02b)");
}
