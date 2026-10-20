namespace Deskair.App.Composition;

/// <summary>
/// Command-line options. <c>--gallery</c> opens the control gallery instead of the main window.
/// <c>--simulated</c> runs the main window on a simulated sensor with its own data folder in the temp folder, so a developer can use the app without a sensor and without touching real data.
/// <c>--sensor &lt;address&gt;</c> runs the real app on that one sensor, also with its own data folder in the temp folder.
/// <c>--first-run</c> runs the real app, with real Bluetooth, on an empty data folder in the temp folder that is wiped at every start, to try the welcome screen.
/// <c>--test-toast</c> shows one High CO₂ toast right after the start, to try the toast buttons without waiting for a real alert.
/// <c>-ToastActivated</c> (and <c>-Embedding</c>, which Windows adds) means Windows started the app because a toast button was pressed: it starts in the tray without showing the window.
/// <c>--background</c> is what the Start with Windows entry passes: the app starts in the tray without showing the window. A packaged build cannot pass it, so <c>Program</c> asks Windows instead.
/// </summary>
public sealed record AppOptions(
    bool Gallery,
    bool Simulated = false,
    string? Sensor = null,
    bool FirstRun = false,
    bool StartedByToast = false,
    bool TestToast = false,
    bool StartedInBackground = false)
{
    /// <summary>The argument the Run key entry adds.</summary>
    public const string BackgroundArgument = "--background";

    public static AppOptions Parse(IReadOnlyList<string> args)
    {
        var gallery = args.Contains("--gallery", StringComparer.OrdinalIgnoreCase);
        var simulated = args.Contains("--simulated", StringComparer.OrdinalIgnoreCase);
        var firstRun = args.Contains("--first-run", StringComparer.OrdinalIgnoreCase);
        var testToast = args.Contains("--test-toast", StringComparer.OrdinalIgnoreCase);
        var startedByToast = args.Contains("-Embedding", StringComparer.OrdinalIgnoreCase) || args.Contains("-ToastActivated", StringComparer.OrdinalIgnoreCase);

        var background = args.Contains(BackgroundArgument, StringComparer.OrdinalIgnoreCase);

        return new AppOptions(gallery, simulated, ValueOf(args, "--sensor"), firstRun, startedByToast, testToast, background);
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
