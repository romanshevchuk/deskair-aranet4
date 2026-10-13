namespace Deskair.App.Platform.Tray;

/// <summary>
/// Makes a click on the tray icon close an open flyout instead of reopening it. Clicking the icon first takes the focus away from the flyout, which hides it,
/// and only then reports the click; so a click that arrives just after the flyout hid is the one that meant "close".
/// </summary>
public sealed class ToggleGuard
{
    private readonly TimeProvider timeProvider;
    private readonly TimeSpan window;
    private DateTimeOffset? hiddenAt;

    public ToggleGuard(TimeProvider timeProvider, TimeSpan window)
    {
        this.timeProvider = timeProvider;
        this.window = window;
    }

    /// <summary>Call when the flyout was hidden for any reason.</summary>
    public void NotifyHidden()
    {
        hiddenAt = timeProvider.GetUtcNow();
    }

    /// <summary>True when a click now should open the flyout; false when it only closed it a moment ago.</summary>
    public bool ShouldShow()
    {
        return hiddenAt is not { } hidden || timeProvider.GetUtcNow() - hidden > window;
    }
}
