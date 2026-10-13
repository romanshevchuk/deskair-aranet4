using System.IO;
using Deskair.App.Composition;
using Deskair.Services.Platform;

namespace Deskair.App.Platform;

public sealed class KnownFolders : IKnownFolders
{
    /// <summary>The temp folder the <c>--first-run</c> option uses. It is wiped at every start (see <see cref="ResetFirstRunFolder"/>).</summary>
    public static readonly string FirstRunFolder = Path.Combine(Path.GetTempPath(), "Deskair-first-run");

    public KnownFolders(AppOptions options, IDataFolderChoice choice)
    {
        // The simulated app and the developer runs with their own data never write to Documents, and ignore the folder the user picked.
        DataFolder = options.Simulated
            ? Path.Combine(Path.GetTempPath(), "Deskair-simulated")
            : options.Sensor is not null
                ? Path.Combine(Path.GetTempPath(), "Deskair-sensor-test")
                : options.FirstRun
                    ? FirstRunFolder
                    : choice.Chosen ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Deskair");
    }

    public string DataFolder { get; }

    public string LogFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Deskair",
        "logs");

    /// <summary>Deletes the <c>--first-run</c> data folder so the next run starts as a first run. It only ever touches that one folder.</summary>
    public static void ResetFirstRunFolder()
    {
        if (Directory.Exists(FirstRunFolder))
        {
            Directory.Delete(FirstRunFolder, recursive: true);
        }
    }
}
