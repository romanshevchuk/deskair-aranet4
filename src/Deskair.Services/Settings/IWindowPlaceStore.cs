namespace Deskair.Services.Settings;

/// <summary>Keeps the main window's place on this PC, apart from the settings in the data folder (D-0031).</summary>
public interface IWindowPlaceStore
{
    /// <summary>Returns null when nothing has been saved yet or the stored place cannot be used.</summary>
    Task<WindowPlace?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(WindowPlace place, CancellationToken cancellationToken);
}
