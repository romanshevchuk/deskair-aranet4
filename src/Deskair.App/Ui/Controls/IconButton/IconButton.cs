using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

/// <summary>
/// A quiet 32 px button that shows one Segoe Fluent Icons glyph: no fill or border until hovered (History ‹ ›, export, the flyout's Settings).
/// Give it <c>AutomationProperties.Name</c> and a tooltip, because the glyph says nothing to Narrator.
/// </summary>
public class IconButton : Button
{
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph),
        typeof(string),
        typeof(IconButton),
        new PropertyMetadata(null));

    static IconButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(IconButton), new FrameworkPropertyMetadata(typeof(IconButton)));
    }

    /// <summary>The glyph, for example "" (chevron left).</summary>
    public string? Glyph
    {
        get => (string?)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }
}
