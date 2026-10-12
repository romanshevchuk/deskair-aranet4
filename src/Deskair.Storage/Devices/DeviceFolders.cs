using Deskair.Domain.Readings;
using Deskair.Services.Platform;

namespace Deskair.Storage.Devices;

/// <summary>Where a device lives on disk: <c>devices/&lt;providerId&gt;-&lt;address&gt;/</c> (docs/plans/questions.md Q9).</summary>
internal static class DeviceFolders
{
    private const string RootName = "devices";

    public static string Root(IKnownFolders folders) => Path.Combine(folders.DataFolder, RootName);

    public static string For(IKnownFolders folders, DeviceId id) => Path.Combine(Root(folders), NameOf(id));

    private static string NameOf(DeviceId id) => Safe(id.ProviderId) + "-" + Safe(id.Address);

    private static string Safe(string text)
    {
        var invalid = Path.GetInvalidFileNameChars();

        return string.Concat(text.Select(character => invalid.Contains(character) ? '_' : character));
    }
}
