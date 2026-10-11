using System.IO;
using Deskair.Services.Platform;

namespace Deskair.App.Platform;

public sealed class KnownFolders : IKnownFolders
{
    public string DataFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Deskair");

    public string LogFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Deskair",
        "logs");
}
