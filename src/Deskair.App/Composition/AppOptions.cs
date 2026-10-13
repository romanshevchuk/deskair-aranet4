namespace Deskair.App.Composition;

/// <summary>
/// Command-line options. <c>--gallery</c> opens the control gallery instead of the main window.
/// <c>--simulated</c> runs the main window on a simulated sensor with its own data folder in the temp folder, so a developer can use the app without a sensor and without touching real data.
/// <c>--sensor &lt;address&gt;</c> runs the real app on that one sensor, also with its own data folder in the temp folder.
/// <c>--first-run</c> runs the real app, with real Bluetooth, on an empty data folder in the temp folder that is wiped at every start, to try the welcome screen.
/// <c>--spike-s1</c> runs the sensor history spike instead of the app; <c>--compare &lt;file&gt;</c> adds a phone export to compare against.
/// <c>--spike-s5</c> runs the toast spike instead of the app (modes: <c>--leave</c>, <c>--cleanup</c>; Windows adds <c>-Embedding</c> when a toast click starts the app).
/// <c>--spike-s3</c> runs the title bar spike instead of the app (<c>--variant native|chrome|custom</c>).
/// <c>-ToastActivated</c> (and <c>-Embedding</c>, which Windows adds) means Windows started the app because a toast button was pressed: it starts in the tray without showing the window.
/// </summary>
public sealed record AppOptions(bool Gallery, bool Simulated = false, bool SpikeS1 = false, string? CompareFile = null, string? Sensor = null, bool FirstRun = false, bool SpikeS5 = false, bool StartedByToast = false, bool SpikeS3 = false)
{
    public static AppOptions Parse(IReadOnlyList<string> args)
    {
        var gallery = args.Contains("--gallery", StringComparer.OrdinalIgnoreCase);
        var simulated = args.Contains("--simulated", StringComparer.OrdinalIgnoreCase);
        var spikeS1 = args.Contains("--spike-s1", StringComparer.OrdinalIgnoreCase);
        var firstRun = args.Contains("--first-run", StringComparer.OrdinalIgnoreCase);
        var spikeS5 = args.Contains("--spike-s5", StringComparer.OrdinalIgnoreCase);
        var spikeS3 = args.Contains("--spike-s3", StringComparer.OrdinalIgnoreCase);
        var startedByToast = args.Contains("-Embedding", StringComparer.OrdinalIgnoreCase) || args.Contains("-ToastActivated", StringComparer.OrdinalIgnoreCase);

        return new AppOptions(gallery, simulated, spikeS1, ValueOf(args, "--compare"), ValueOf(args, "--sensor"), firstRun, spikeS5, startedByToast, spikeS3);
    }

    private static string? ValueOf(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
