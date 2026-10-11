namespace Deskair.Services.Settings;

public interface ISettingsStore
{
    /// <summary>Returns <see cref="AppSettings.Default"/> when nothing has been saved yet.</summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}
