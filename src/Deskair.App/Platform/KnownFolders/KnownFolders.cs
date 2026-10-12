using System.IO;
using Deskair.App.Composition;
using Deskair.Services.Platform;

namespace Deskair.App.Platform;

public sealed class KnownFolders : IKnownFolders
{
    public KnownFolders(AppOptions options)
    {
        // The simulated app never writes to Documents.
        DataFolder = options.Simulated
            ? Path.Combine(Path.GetTempPath(), "Deskair-simulated")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Deskair");
    }

    public string DataFolder { get; }

    public string LogFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Deskair",
        "logs");
}
