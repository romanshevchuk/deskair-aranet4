namespace Deskair.Services.Settings;

/// <summary>
/// Where the main window was the last time it was hidden or the app quit, so it opens there again. The rectangle is the window's normal (not maximized)
/// place in Windows' own placement coordinates, in physical pixels; <see cref="Maximized"/> says whether it was maximized over that place.
/// </summary>
public sealed record WindowPlace(int Left, int Top, int Width, int Height, bool Maximized);
