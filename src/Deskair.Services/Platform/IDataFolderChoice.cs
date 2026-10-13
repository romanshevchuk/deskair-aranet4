namespace Deskair.Services.Platform;

/// <summary>
/// The data folder the user picked in Settings. It is kept outside the data folder (settings live inside it) and is used from the next start;
/// the files in the old folder are not moved.
/// </summary>
public interface IDataFolderChoice
{
    /// <summary>The picked folder, or null when the user never picked one.</summary>
    string? Chosen { get; }

    void Choose(string folder);
}
