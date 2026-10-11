namespace Deskair.App.Composition;

/// <summary>Command-line options. <c>--gallery</c> opens the control gallery instead of the main window.</summary>
public sealed record AppOptions(bool Gallery)
{
    public static AppOptions Parse(IReadOnlyList<string> args)
    {
        var gallery = args.Contains("--gallery", StringComparer.OrdinalIgnoreCase);

        return new AppOptions(gallery);
    }
}
