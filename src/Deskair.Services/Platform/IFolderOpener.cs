namespace Deskair.Services.Platform;

/// <summary>Shows a folder in the system file browser.</summary>
public interface IFolderOpener
{
    void Open(string folder);
}
