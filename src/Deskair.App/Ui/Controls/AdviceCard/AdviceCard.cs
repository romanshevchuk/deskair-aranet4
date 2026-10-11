using System.Windows;
using System.Windows.Controls;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-02) [M, haiku]
// Spec: docs/design/components.md "Hero card"; docs/design/design-tokens.md "Zone colours" (advice.*, icons); docs/design/accessibility.md
// Input : Zone, title, text.
// Output: Panel with advice.bg, advice.border, radius 12, an 18 x 18 line icon (check circle for Good, window for Elevated, warning triangle for High) and the title and text.
// Rules : Colours only from the Zone.*.Advice* tokens; the zone is also conveyed by the icon, not by colour alone.
// Edge  : A null zone shows a neutral Insight.Bg panel without an icon.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class AdviceCard : Control
{
    public static readonly DependencyProperty ZoneProperty = DependencyProperty.Register(
        nameof(Zone),
        typeof(Zone?),
        typeof(AdviceCard),
        new PropertyMetadata(null));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(AdviceCard),
        new PropertyMetadata(null));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(AdviceCard),
        new PropertyMetadata(null));

    static AdviceCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AdviceCard), new FrameworkPropertyMetadata(typeof(AdviceCard)));
    }

    public Zone? Zone
    {
        get => (Zone?)GetValue(ZoneProperty);
        set => SetValue(ZoneProperty, value);
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
