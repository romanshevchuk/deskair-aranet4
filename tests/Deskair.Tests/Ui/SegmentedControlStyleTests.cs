using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Deskair.App.Ui.Controls;

namespace Deskair.Tests.Ui;

/// <summary>The segmented control must use its own item template, not WPF's default list item.</summary>
public class SegmentedControlStyleTests
{
    private static SegmentedControl Create(int selected)
    {
        var control = new SegmentedControl();
        control.Items.Add(new ListBoxItem { Content = "Live" });
        control.Items.Add(new ListBoxItem { Content = "History" });
        control.Items.Add(new ListBoxItem { Content = "Settings" });
        UiHost.Prepare(control);
        control.SelectedIndex = selected;
        control.Measure(new Size(400, 100));
        control.Arrange(new Rect(0, 0, control.DesiredSize.Width, control.DesiredSize.Height));
        control.UpdateLayout();

        return control;
    }

    private static Border Frame(ListBoxItem item) => (Border)VisualTreeHelper.GetChild(item, 0);

    private static Border Indicator(SegmentedControl control) => (Border)control.Template.FindName("PART_Indicator", control);

    [Fact]
    public void The_selected_segment_is_marked_by_a_filled_rounded_pill_with_a_border()
    {
        UiHost.Run(() =>
        {
            var control = Create(1);
            var item = (ListBoxItem)control.Items[1];
            var frame = Indicator(control);

            Assert.True(item.IsSelected);
            Assert.Equal(((SolidColorBrush)Application.Current.FindResource("Segment.Selected.Brush")).Color, ((SolidColorBrush)frame.Background).Color);
            Assert.Equal(((SolidColorBrush)Application.Current.FindResource("Border.Brush")).Color, ((SolidColorBrush)frame.BorderBrush).Color);
            Assert.Equal(new CornerRadius(8), frame.CornerRadius);
            Assert.Equal(FontWeights.SemiBold, item.FontWeight);
        });
    }

    [Fact]
    public void An_unselected_segment_has_no_fill_and_no_visible_border()
    {
        UiHost.Run(() =>
        {
            var item = (ListBoxItem)Create(1).Items[0];
            var frame = Frame(item);

            Assert.False(item.IsSelected);
            Assert.Equal(Brushes.Transparent.Color, ((SolidColorBrush)frame.Background).Color);
            Assert.Equal(Brushes.Transparent.Color, ((SolidColorBrush)frame.BorderBrush).Color);
        });
    }

    [Fact]
    public void The_selected_segment_is_white_in_the_light_theme_and_lighter_than_the_track_in_the_dark_one()
    {
        var light = UiHost.Run(Deskair.Services.Settings.ThemeChoice.Light, () => ((SolidColorBrush)Indicator(Create(0)).Background).Color);
        var dark = UiHost.Run(Deskair.Services.Settings.ThemeChoice.Dark, () => ((SolidColorBrush)Indicator(Create(0)).Background).Color);

        Assert.Equal(Colors.White, light);
        Assert.NotEqual(light, dark);
    }
}
