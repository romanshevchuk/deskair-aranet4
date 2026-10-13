using Deskair.Services.Platform;

namespace Deskair.Tests.Fakes;

/// <summary>Answers every dialog with a fixed path (null means the user cancelled) and remembers what it was asked.</summary>
public sealed class FakeFileDialogs : IFileDialogs
{
    public string? PathToOpen { get; set; }

    public string? PathToSave { get; set; }

    public string? FolderToPick { get; set; }

    public int OpenRequests { get; private set; }

    public string? SuggestedName { get; private set; }

    public FileTypeFilter? LastFilter { get; private set; }

    public Task<string?> PickFileToOpenAsync(string title, FileTypeFilter filter, CancellationToken cancellationToken)
    {
        OpenRequests++;
        LastFilter = filter;

        return Task.FromResult(PathToOpen);
    }

    public Task<string?> PickFileToSaveAsync(string title, string suggestedName, FileTypeFilter filter, CancellationToken cancellationToken)
    {
        SuggestedName = suggestedName;
        LastFilter = filter;

        return Task.FromResult(PathToSave);
    }

    public Task<string?> PickFolderAsync(string title, CancellationToken cancellationToken)
    {
        return Task.FromResult(FolderToPick);
    }
}
