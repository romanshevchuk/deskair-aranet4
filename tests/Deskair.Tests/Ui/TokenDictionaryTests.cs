using System.Windows;
using System.Windows.Media;

namespace Deskair.Tests.Ui;

/// <summary>Checks that the theme dictionaries cover every token of docs/design/design-tokens.md in both themes.</summary>
public class TokenDictionaryTests
{
    private static readonly string[] themed = ["Colors", "Zones", "Metrics"];

    private static ResourceDictionary Load(string name, string theme)
    {
        var source = new Uri($"pack://application:,,,/Deskair.App;component/Ui/Theme/Tokens/{name}.{theme}.xaml", UriKind.Absolute);

        return new ResourceDictionary { Source = source };
    }

    private static HashSet<string> Keys(ResourceDictionary dictionary)
    {
        return dictionary.Keys.Cast<object>().Select(key => (string)key).ToHashSet();
    }

    private static Color ColorOf(string theme, string file, string key)
    {
        return (Color)Load(file, theme)[$"{key}.Color"];
    }

    [Theory]
    [InlineData("Colors")]
    [InlineData("Zones")]
    [InlineData("Metrics")]
    public void Light_and_dark_define_exactly_the_same_tokens(string file)
    {
        var (light, dark) = UiHost.Run(() => (Keys(Load(file, "Light")), Keys(Load(file, "Dark"))));

        Assert.Empty(light.Except(dark));
        Assert.Empty(dark.Except(light));
    }

    [Fact]
    public void Every_colour_has_a_matching_brush()
    {
        UiHost.Run(() =>
        {
            foreach (var file in themed)
            {
                foreach (var theme in new[] { "Light", "Dark" })
                {
                    var keys = Keys(Load(file, theme));

                    foreach (var key in keys.Where(key => key.EndsWith(".Color", StringComparison.Ordinal)))
                    {
                        Assert.Contains(key[..^".Color".Length] + ".Brush", keys);
                    }
                }
            }
        });
    }

    [Fact]
    public void Every_zone_has_the_nine_zone_tokens_plus_segment_line_and_band()
    {
        var keys = UiHost.Run(() => Keys(Load("Zones", "Light")));
        var parts = new[] { "CardBg", "CardBorder", "PillBg", "PillText", "Dot", "AdviceBg", "AdviceBorder", "Divider", "AdviceIcon", "Segment", "Line" };

        foreach (var zone in new[] { "Good", "Elevated", "High" })
        {
            foreach (var part in parts)
            {
                Assert.Contains($"Zone.{zone}.{part}.Brush", keys);
            }

            Assert.Contains($"Chart.Band.{zone}.Brush", keys);
        }
    }

    [Fact]
    public void Every_metric_has_a_colour_a_fill_and_an_ideal_range_token()
    {
        var keys = UiHost.Run(() => Keys(Load("Metrics", "Dark")));

        foreach (var metric in new[] { "Co2", "Temperature", "Humidity", "Pressure" })
        {
            Assert.Contains($"Metric.{metric}.Brush", keys);
            Assert.Contains($"Metric.{metric}.Fill.Brush", keys);
            Assert.Contains($"Metric.{metric}.Ideal.Brush", keys);
        }
    }

    [Theory]
    [InlineData("Light", "Colors", "Window.Bg", "#EEF1F3")]
    [InlineData("Dark", "Colors", "Window.Bg", "#12161A")]
    [InlineData("Light", "Colors", "Text.Primary", "#182026")]
    [InlineData("Dark", "Colors", "Surface", "#1B2026")]
    [InlineData("Light", "Zones", "Zone.High.PillText", "#8E241A")]
    [InlineData("Dark", "Zones", "Zone.Elevated.CardBorder", "#8A6A1F")]
    [InlineData("Light", "Zones", "Trend.Falling", "#1B7A53")]
    [InlineData("Dark", "Zones", "Trend.Rising", "#FF8A7E")]
    [InlineData("Light", "Metrics", "Metric.Humidity", "#2563C9")]
    [InlineData("Dark", "Metrics", "Metric.Pressure", "#A58DF0")]
    public void Values_match_the_design_token_table(string theme, string file, string key, string hex)
    {
        var color = UiHost.Run(() => ColorOf(theme, file, key));

        Assert.Equal((Color)ColorConverter.ConvertFromString(hex), color);
    }

    [Fact]
    public void Typography_spacing_and_radius_tokens_exist()
    {
        UiHost.Run(() =>
        {
            var resources = Application.Current.Resources;

            Assert.Equal(80.0, resources["Font.Size.Hero"]);
            Assert.Equal(12.0, resources["Font.Size.Caption"]);
            Assert.Equal(52.0, resources["Size.Header.Height"]);
            Assert.Equal(40.0, resources["Size.Footer.Height"]);
            Assert.Equal(new Thickness(24, 8, 24, 20), resources["Space.Page.Padding"]);
            Assert.Equal(new CornerRadius(16), resources["Radius.Card"]);
            Assert.Equal(new CornerRadius(10), resources["Radius.SegmentTrack"]);
            Assert.Equal(new CornerRadius(8), resources["Radius.Segment"]);
        });
    }

    [Fact]
    public void Switching_theme_changes_the_resources_the_controls_see()
    {
        var (light, dark) = UiHost.Run(() =>
        {
            UiHost.Theme.Apply(Deskair.Services.Settings.ThemeChoice.Light);
            var lightColor = ((SolidColorBrush)Application.Current.FindResource("Window.Bg.Brush")).Color;

            UiHost.Theme.Apply(Deskair.Services.Settings.ThemeChoice.Dark);
            var darkColor = ((SolidColorBrush)Application.Current.FindResource("Window.Bg.Brush")).Color;

            return (lightColor, darkColor);
        });

        Assert.NotEqual(light, dark);
    }
}
