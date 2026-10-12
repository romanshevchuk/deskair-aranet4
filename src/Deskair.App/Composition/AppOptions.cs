namespace Deskair.App.Composition;

/// <summary>
/// Command-line options. <c>--gallery</c> opens the control gallery instead of the main window.
/// <c>--simulated</c> runs the main window on a simulated sensor with its own data folder in the temp folder, so a developer can use the app without a sensor and without touching real data.
/// </summary>
public sealed record AppOptions(bool Gallery, bool Simulated = false)
{
    public static AppOptions Parse(IReadOnlyList<string> args)
    {
        var gallery = args.Contains("--gallery", StringComparer.OrdinalIgnoreCase);
        var simulated = args.Contains("--simulated", StringComparer.OrdinalIgnoreCase);

        return new AppOptions(gallery, simulated);
    }
}
