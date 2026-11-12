using System.Diagnostics;
using System.IO;
using Deskair.Services.Platform;
using Windows.Storage;

namespace Deskair.App.Platform.Files;

/// <summary>
/// Opens a folder in File Explorer. The folder is created first, so the button works before the first reading is stored.
/// The Store build's writes to %LOCALAPPDATA% land in the package's own copy of it, which File Explorer does not see; such a folder is opened at that copy.
/// </summary>
public sealed class ExplorerFolderOpener : IFolderOpener
{
    private readonly IPackageIdentity packageIdentity;

    public ExplorerFolderOpener(IPackageIdentity packageIdentity)
    {
        this.packageIdentity = packageIdentity;
    }

    public void Open(string folder)
    {
        Directory.CreateDirectory(folder);

        using var explorer = Process.Start(new ProcessStartInfo("explorer.exe", $"\"{VisibleFolder(folder)}\"") { UseShellExecute = true });
    }

    /// <summary>
    /// Where a packaged app's folder under <paramref name="localAppData"/> really is: under <c>LocalCache\Local</c> of the package (<paramref name="packageLocalCache"/>).
    /// Null for a folder outside <paramref name="localAppData"/>, which is not redirected.
    /// </summary>
    public static string? RedirectedFolder(string folder, string localAppData, string packageLocalCache)
    {
        var relative = Path.GetRelativePath(localAppData, folder);

        if (relative == "." || relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            return null;
        }

        return Path.Combine(packageLocalCache, "Local", relative);
    }

    // A folder that already existed before the app was installed is used in place, so the redirected copy counts only when it exists.
    private string VisibleFolder(string folder)
    {
        if (!packageIdentity.IsPackaged)
        {
            return folder;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var redirected = RedirectedFolder(folder, localAppData, ApplicationData.Current.LocalCacheFolder.Path);

        return redirected is not null && Directory.Exists(redirected) ? redirected : folder;
    }
}
