using System.Diagnostics;
using System.IO;
using Deskair.Services.Platform;

namespace Deskair.App.Platform.Files;

/// <summary>Opens a folder in File Explorer. The folder is created first, so the button works before the first reading is stored.</summary>
public sealed class ExplorerFolderOpener : IFolderOpener
{
    public void Open(string folder)
    {
        Directory.CreateDirectory(folder);

        using var explorer = Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
    }
}
