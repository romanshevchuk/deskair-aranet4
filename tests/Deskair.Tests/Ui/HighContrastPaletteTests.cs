using System.Windows;
using System.Windows.Media;
using Deskair.App.Ui.Theme;

namespace Deskair.Tests.Ui;

/// <summary>High Contrast (accessibility.md): system colours for surfaces, text and outlines; hue-only colours become the text colour at stepped opacities.</summary>
public class HighContrastPaletteTests
{
    private static readonly HighContrastColors colors = new(
        Color.FromRgb(0, 0, 0),
        Color.FromRgb(255, 255, 255),
        Color.FromRgb(26, 235, 255),
        Color.FromRgb(0, 0, 0),
        Color.FromRgb(63, 242, 63),
        Color.FromRgb(255, 255, 0));

    private static ResourceDictionary Tokens(string name)
        => new() { Source = new Uri($"pack://application:,,,/Deskair.App;component/Ui/Theme/Tokens/{name}.Light.xaml", UriKind.Absolute) };

    private static ResourceDictionary Overlay()
        => HighContrastPalette.Create([Tokens("Colors"), Tokens("Zones"), Tokens("Metrics")], colors);

    [Fact]
    public void Surfaces_are_the_window_colour_and_text_and_outlines_the_text_colour()
    {
        UiHost.Run(() =>
        {
            var overlay = Overlay();

            Assert.Equal(colors.Window, overlay["Surface.Color"]);
            Assert.Equal(colors.Window, overlay["Window.Bg.Color"]);
            Assert.Equal(colors.WindowText, overlay["Text.Primary.Color"]);
            Assert.Equal(colors.WindowText, overlay["Text.Secondary.Color"]);
            Assert.Equal(colors.WindowText, overlay["Card.Border.Color"]);
            Assert.Equal(colors.Highlight, overlay["Accent.Color"]);
            Assert.Equal(colors.HighlightText, overlay["Accent.On.Color"]);
            Assert.Equal(colors.HotTrack, overlay["Focus.Ring.Color"]);
        });
    }

    [Fact]
    public void Every_colour_it_replaces_has_its_brush_replaced_too()
    {
        UiHost.Run(() =>
        {
            var overlay = Overlay();
            var brush = Assert.IsType<SolidColorBrush>(overlay["Surface.Brush"]);

            Assert.Equal(colors.Window, brush.Color);
            Assert.True(brush.IsFrozen);
        });
    }

    [Fact]
    public void The_co2_heat_steps_get_darker_in_six_even_steps_of_the_text_colour()
    {
        UiHost.Run(() =>
        {
            var overlay = Overlay();
            string[] steps = ["G1", "G2", "E1", "E2", "H1", "H2"];
            var alphas = steps.Select(step => ((Color)overlay[$"Heat.{step}.Color"]).A).ToList();

            Assert.Equal(alphas.Order(), alphas);
            Assert.Equal(6, alphas.Distinct().Count());
            Assert.Equal(255, alphas[^1]);
            Assert.All(steps, step => Assert.Equal(colors.WindowText, Opaque((Color)overlay[$"Heat.{step}.Color"])));
        });
    }

    [Fact]
    public void The_comfort_steps_are_stepped_on_both_sides_and_the_zones_differ_by_opacity()
    {
        UiHost.Run(() =>
        {
            var overlay = Overlay();
            byte Alpha(string key) => ((Color)overlay[$"{key}.Color"]).A;

            Assert.True(Alpha("Heat.Cool2") > Alpha("Heat.Cool1"));
            Assert.True(Alpha("Heat.Cool1") > Alpha("Heat.Comfort"));
            Assert.Equal(Alpha("Heat.Cool2"), Alpha("Heat.Warm2"));
            Assert.Equal(Alpha("Heat.Dry1"), Alpha("Heat.Humid1"));
            Assert.True(Alpha("Zone.Good.Segment") < Alpha("Zone.Elevated.Segment"));
            Assert.True(Alpha("Zone.Elevated.Segment") < Alpha("Zone.High.Segment"));
        });
    }

    [Fact]
    public void Glows_are_invisible_and_the_advice_gets_an_outline()
    {
        UiHost.Run(() =>
        {
            var overlay = Overlay();

            Assert.Equal(0, ((Color)overlay["Zone.High.Glow.Color"]).A);
            Assert.Equal(0, ((Color)overlay["Glow.Offline.Color"]).A);
            Assert.Equal(colors.WindowText, overlay["Zone.Elevated.AdviceBorder.Color"]);
            Assert.Equal(colors.Window, overlay["Zone.Elevated.AdviceBg.Color"]);
        });
    }

    [Fact]
    public void Tray_colours_are_left_alone()
    {
        UiHost.Run(() =>
        {
            var overlay = Overlay();

            Assert.False(overlay.Contains("Tray.Offline.Color"));
            Assert.False(overlay.Contains("Tray.Value.OnAmber.Color"));
        });
    }


    [Fact]
    public void A_colour_token_the_palette_leaves_alone_is_a_decision_not_an_oversight()
    {
        // Add a new token to the palette (HighContrastPalette.Map) or, if it must keep its colour, to this list with the reason.
        // Tray.: the tray icon is drawn by Windows. Caption.CloseHover: not used by any view; Windows draws the caption buttons (UseAeroCaptionButtons).
        string[] keptOnPurpose = ["Tray.", "Caption.CloseHover."];

        var untouched = UiHost.Run(() =>
        {
            var overlay = Overlay();

            return new[] { Tokens("Colors"), Tokens("Zones"), Tokens("Metrics") }
                .SelectMany(source => source.Keys.OfType<string>())
                .Where(key => key.EndsWith(".Color", StringComparison.Ordinal) && !overlay.Contains(key))
                .Where(key => !keptOnPurpose.Any(prefix => key.StartsWith(prefix, StringComparison.Ordinal)))
                .Order()
                .ToList();
        });

        Assert.Empty(untouched);
    }

    private static Color Opaque(Color color) => Color.FromRgb(color.R, color.G, color.B);
}
