using System.Windows;

namespace Deskair.App.Ui.Motion;

/// <summary>
/// Whether the interface may animate: the Windows "Animation effects" setting (<see cref="SystemParameters.ClientAreaAnimation"/>).
/// Tests and renders set <see cref="Override"/> so they see the final state of every control at once.
/// </summary>
public static class MotionPolicy
{
    /// <summary>When set, replaces the Windows setting.</summary>
    public static bool? Override { get; set; }

    public static bool Enabled => Override ?? SystemParameters.ClientAreaAnimation;

    /// <summary>A named duration from the theme (<c>Motion.*</c>), or zero while animations are off.</summary>
    public static Duration Of(string key) => Enabled && Application.Current?.TryFindResource(key) is Duration duration ? duration : new Duration(TimeSpan.Zero);
}
