namespace Deskair.Services.Platform;

/// <summary>Asks the user for a file. Both calls return null when the user cancels.</summary>
public interface IFileDialogs
{
    Task<string?> PickFileToOpenAsync(string title, FileTypeFilter filter, CancellationToken cancellationToken);

    Task<string?> PickFileToSaveAsync(string title, string suggestedName, FileTypeFilter filter, CancellationToken cancellationToken);

    Task<string?> PickFolderAsync(string title, CancellationToken cancellationToken);
}
