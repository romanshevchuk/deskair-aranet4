using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    public void Card_takes_the_zone_tint_and_border(Zone zone, string name)
    {
        UiHost.Run(() =>
        {
            var card = UiHost.Prepare(new Card { Zone = zone });

            Assert.Equal(Token($"Zone.{name}.CardBg.Brush"), BrushColor(card.Background));
            Assert.Equal(Token($"Zone.{name}.CardBorder.Brush"), BrushColor(card.BorderBrush));
            Assert.Equal(new Thickness(1.5), card.BorderThickness);
        });
    }

    [Fact]
    public void Card_without_a_zone_is_a_neutral_surface()
    {
        UiHost.Run(() =>
        {
            var card = UiHost.Prepare(new Card());

            Assert.Equal(Token("Surface.Brush"), BrushColor(card.Background));
            Assert.Equal(Token("Border.Brush"), BrushColor(card.BorderBrush));
            Assert.Equal(new Thickness(1), card.BorderThickness);
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

            Assert.Equal(Token("Toggle.On.Brush"), BrushColor(track.Background));
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

    [Fact]
    public void The_catalogue_lists_every_control_from_the_architecture_doc()
    {
        var expected = new[]
        {
            "Card", "Pill", "StatusDot", "SegmentedControl", "ToggleSwitch", "Gauge", "AdviceCard", "StatTriple",
            "MetricCard", "Banner", "EmptyStateCard", "RoomChip", "RoomMenu", "BatteryIcon", "CoverageChip",
            "ZoneShareBar", "InsightBox", "KeyValueCell", "SettingsCard", "SettingsRow", "PrimaryButton", "SecondaryButton"
        };

        var actual = typeof(Card).Assembly.GetTypes()
            .Where(type => type.Namespace == "Deskair.App.Ui.Controls")
            .Select(type => type.Name);

        Assert.Empty(expected.Except(actual));
    }
}
