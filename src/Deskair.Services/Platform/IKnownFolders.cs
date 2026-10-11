namespace Deskair.Services.Platform;

public interface IKnownFolders
{
    /// <summary>Default data folder: Documents\Deskair.</summary>
    string DataFolder { get; }

    /// <summary>Log folder: %LOCALAPPDATA%\Deskair\logs.</summary>
    string LogFolder { get; }
}
