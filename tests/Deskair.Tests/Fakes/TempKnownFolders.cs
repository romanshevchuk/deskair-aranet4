using System.IO;
using Deskair.Services.Platform;

namespace Deskair.Tests.Fakes;

/// <summary>A data folder inside the system temp folder, deleted when the test ends. No test touches Documents.</summary>
public sealed class TempKnownFolders : IKnownFolders, IDisposable
{
    public TempKnownFolders()
    {
        DataFolder = Path.Combine(Path.GetTempPath(), "deskair-tests", Guid.NewGuid().ToString("N"));
        LogFolder = Path.Combine(DataFolder, "logs");
    }

    public string DataFolder { get; }

    public string LogFolder { get; }

    public void Dispose()
    {
        if (Directory.Exists(DataFolder))
        {
            Directory.Delete(DataFolder, recursive: true);
        }
    }
}
