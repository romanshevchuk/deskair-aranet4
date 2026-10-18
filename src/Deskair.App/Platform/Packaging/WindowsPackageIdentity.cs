using System.Runtime.InteropServices;
using Deskair.Services.Platform;

namespace Deskair.App.Platform.Packaging;

/// <summary>Asks Windows whether this process has package identity. Answered once; it cannot change while the program runs.</summary>
public sealed class WindowsPackageIdentity : IPackageIdentity
{
    private const int NoPackage = 15700;

    private readonly Lazy<bool> isPackaged = new(Detect);

    public bool IsPackaged => isPackaged.Value;

    private static bool Detect()
    {
        uint length = 0;

        return GetCurrentPackageFullName(ref length, null) != NoPackage;
    }

    [DllImport("kernel32.dll")]
    private static extern int GetCurrentPackageFullName(ref uint length, char[]? name);
}
