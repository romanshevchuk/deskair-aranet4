namespace Deskair.App.Composition;

/// <summary>
/// Command-line options. <c>--gallery</c> opens the control gallery instead of the main window.
/// <c>--simulated</c> runs the main window on a simulated sensor with its own data folder in the temp folder, so a developer can use the app without a sensor and without touching real data.
/// <c>--sensor &lt;address&gt;</c> runs the real app on that one sensor, also with its own data folder in the temp folder, until the first-run screen can pick a sensor.
/// <c>--spike-s1</c> runs the sensor history spike instead of the app; <c>--compare &lt;file&gt;</c> adds a phone export to compare against.
/// </summary>
public sealed record AppOptions(bool Gallery, bool Simulated = false, bool SpikeS1 = false, string? CompareFile = null, string? Sensor = null)
{
    public static AppOptions Parse(IReadOnlyList<string> args)
    {
        var gallery = args.Contains("--gallery", StringComparer.OrdinalIgnoreCase);
        var simulated = args.Contains("--simulated", StringComparer.OrdinalIgnoreCase);
        var spikeS1 = args.Contains("--spike-s1", StringComparer.OrdinalIgnoreCase);

        return new AppOptions(gallery, simulated, spikeS1, ValueOf(args, "--compare"), ValueOf(args, "--sensor"));
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
