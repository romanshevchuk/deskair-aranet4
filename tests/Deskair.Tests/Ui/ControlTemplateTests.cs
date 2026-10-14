using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Deskair.App.Ui.Controls;
using Deskair.Domain.Zones;
using Deskair.Services.Settings;

namespace Deskair.Tests.Ui;

public class ControlTemplateTests
{
    private static Color BrushColor(Brush brush) => ((SolidColorBrush)brush).Color;

    private static Color Token(string key) => BrushColor((Brush)Application.Current.FindResource(key));

    public static TheoryData<Zone, string> ZoneCards => new()
    {
        { Zone.Good, "Good" },
        { Zone.Elevated, "Elevated" },
        { Zone.High, "High" }
    };

    [Theory]
    [MemberData(nameof(ZoneCards))]
    public void A_zone_card_is_a_neutral_surface_with_the_zone_glow(Zone zone, string name)
    {
        UiHost.Run(() =>
        {
            var card = UiHost.Prepare(new Card { Zone = zone });
            var glow = (Border)card.Template.FindName("Glow", card);

            // D-0017: no zone fill or zone border; the zone shows as a glow between the surface and the content.
            Assert.Equal(Token("Surface.Brush"), BrushColor(card.Background));
            Assert.Equal(Token("Card.Border.Brush"), BrushColor(card.BorderBrush));
            Assert.Equal(new Thickness(1), card.BorderThickness);
            Assert.Equal(Visibility.Visible, glow.Visibility);
            Assert.Same(Application.Current.FindResource($"Zone.{name}.Glow.Brush"), glow.Background);
        });
    }

    [Fact]
    public void A_card_without_a_zone_has_no_glow()
    {
        UiHost.Run(() =>
        {
            var card = UiHost.Prepare(new Card());
            var glow = (Border)card.Template.FindName("Glow", card);

            Assert.Equal(Visibility.Collapsed, glow.Visibility);
        });
    }

    [Fact]
    public void Card_without_a_zone_is_a_neutral_surface()
    {
        UiHost.Run(() =>
        {
            var card = UiHost.Prepare(new Card());

            Assert.Equal(Token("Surface.Brush"), BrushColor(card.Background));
            Assert.Equal(Token("Card.Border.Brush"), BrushColor(card.BorderBrush));
            Assert.Equal(new Thickness(1), card.BorderThickness);
        });
    }


    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);

            yield return child;

            foreach (var inner in Descendants(child))
            {
                yield return inner;
            }
        }
    }

    [Fact]
    public void Card_has_a_soft_shadow_behind_its_content_and_not_on_it()
    {
        UiHost.Run(() =>
        {
            var card = UiHost.Prepare(new Card { Content = new TextBlock { Text = "inside" } });

            var shadowed = Descendants(card).OfType<Border>().Where(border => border.Effect is DropShadowEffect).ToList();

            // One border behind the content carries the shadow, so what changes inside the card is never blurred again.
            var shadow = Assert.Single(shadowed);
            Assert.Null(shadow.Child);
            Assert.DoesNotContain(Descendants(card).OfType<TextBlock>(), text => text.Effect is not null);
        });
    }

    [Theory]
    [InlineData(Zone.Good)]
    [InlineData(Zone.Elevated)]
    [InlineData(Zone.High)]
    public void A_zone_card_casts_the_same_neutral_shadow_as_any_card(Zone zone)
    {
        UiHost.Run(ThemeChoice.Light, () =>
        {
            var card = UiHost.Prepare(new Card { Zone = zone });

            var shadow = Descendants(card).OfType<Border>().First(border => border.Effect is DropShadowEffect).Effect;

            Assert.Same(Application.Current.FindResource("Shadow.Card"), shadow);
        });
    }
    [Fact]
    public void Card_follows_the_theme()
    {
        var light = UiHost.Run(ThemeChoice.Light, () => BrushColor(UiHost.Prepare(new Card()).Background));
        var dark = UiHost.Run(ThemeChoice.Dark, () => BrushColor(UiHost.Prepare(new Card()).Background));

        Assert.NotEqual(light, dark);
    }

    [Theory]
    [MemberData(nameof(ZoneCards))]
    public void Pill_uses_the_zone_pill_colours(Zone zone, string name)
    {
        UiHost.Run(() =>
        {
            var pill = UiHost.Prepare(new Pill { Zone = zone });

            Assert.Equal(Token($"Zone.{name}.PillBg.Brush"), BrushColor(pill.Background));
            Assert.Equal(Token($"Zone.{name}.PillText.Brush"), BrushColor(pill.Foreground));
        });
    }

    [Theory]
    [MemberData(nameof(ZoneCards))]
    public void StatusDot_fills_with_the_zone_dot_colour(Zone zone, string name)
    {
        UiHost.Run(() =>
        {
            var dot = UiHost.Prepare(new StatusDot { Zone = zone });
            dot.ApplyTemplate();

            var ellipse = (System.Windows.Shapes.Ellipse)dot.Template.FindName("Dot", dot);

            Assert.Equal(Token($"Zone.{name}.Dot.Brush"), BrushColor(ellipse.Fill));
        });
    }

    [Fact]
    public void StatusDot_without_a_zone_is_neutral()
    {
        UiHost.Run(() =>
        {
            var dot = UiHost.Prepare(new StatusDot());
            dot.ApplyTemplate();

            var ellipse = (System.Windows.Shapes.Ellipse)dot.Template.FindName("Dot", dot);

            Assert.Equal(Token("Text.Tertiary.Brush"), BrushColor(ellipse.Fill));
        });
    }

    [Fact]
    public void ToggleSwitch_track_follows_the_checked_state()
    {
        UiHost.Run(() =>
        {
            var toggle = UiHost.Prepare(new ToggleSwitch());
            toggle.ApplyTemplate();
            var track = (Border)toggle.Template.FindName("Track", toggle);

            Assert.Equal(Token("Toggle.Off.Brush"), BrushColor(track.Background));

            toggle.IsChecked = true;

            // D-0017: on is the ink accent, never green (green means Good air), with the knob in the colour that sits on the accent.
            var knob = (System.Windows.Shapes.Ellipse)toggle.Template.FindName("Knob", toggle);
            Assert.Equal(Token("Accent.Brush"), BrushColor(track.Background));
            Assert.Equal(Token("Accent.On.Brush"), BrushColor(knob.Fill));
        });
    }

    [Fact]
    public void ToggleSwitch_track_is_38_by_22()
    {
        UiHost.Run(() =>
        {
            var toggle = UiHost.Prepare(new ToggleSwitch());
            toggle.ApplyTemplate();
            var track = (Border)toggle.Template.FindName("Track", toggle);

            Assert.Equal(38, track.Width);
            Assert.Equal(22, track.Height);
        });
    }

    [Fact]
    public void ToggleSwitch_hides_the_label_gap_without_content()
    {
        UiHost.Run(() =>
        {
            var toggle = UiHost.Prepare(new ToggleSwitch());
            toggle.ApplyTemplate();
            var label = (ContentPresenter)toggle.Template.FindName("Label", toggle);

            Assert.Equal(Visibility.Collapsed, label.Visibility);

            toggle.Content = "Alerts";

            Assert.Equal(Visibility.Visible, label.Visibility);
        });
    }

    [Fact]
    public void SegmentedControl_selects_one_item_and_has_a_template()
    {
        UiHost.Run(() =>
        {
            var segmented = UiHost.Prepare(new SegmentedControl());
            segmented.Items.Add(new ListBoxItem { Content = "Day" });
            segmented.Items.Add(new ListBoxItem { Content = "Week" });
            segmented.ApplyTemplate();

            segmented.SelectedIndex = 1;

            Assert.NotNull(segmented.Template);
            Assert.Equal("Week", ((ListBoxItem)segmented.SelectedItem).Content);
            Assert.Equal(SelectionMode.Single, segmented.SelectionMode);
        });
    }

    [Fact]
    public void Every_control_in_the_catalogue_has_a_default_template()
    {
        var failures = UiHost.Run(() =>
        {
            var problems = new List<string>();
            var controlTypes = typeof(Card).Assembly.GetTypes()
                .Where(type => type.Namespace == "Deskair.App.Ui.Controls" && type.IsSubclassOf(typeof(Control)) && !type.IsAbstract);

            foreach (var type in controlTypes)
            {
                var control = UiHost.Prepare((Control)Activator.CreateInstance(type)!);
                control.ApplyTemplate();

                if (control.Template is null)
                {
                    problems.Add(type.Name);
                }
            }

            return problems;
        });

        Assert.Empty(failures);
    }
}
