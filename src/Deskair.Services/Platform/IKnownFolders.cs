namespace Deskair.Services.Platform;

public interface IKnownFolders
{
    /// <summary>Default data folder: Documents\Deskair.</summary>
    string DataFolder { get; }

    /// <summary>Log folder: %LOCALAPPDATA%\Deskair\logs.</summary>
    string LogFolder { get; }

    /// <summary>
    /// What belongs to this PC rather than to the user's data, such as the window's place: %LOCALAPPDATA%\Deskair. Never synced with the data folder;
    /// the Store build's copy is removed on uninstall.
    /// </summary>
    string StateFolder { get; }
}
