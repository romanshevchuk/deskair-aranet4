using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Deskair.App.Ui.Controls;

namespace Deskair.Tests.Ui;

/// <summary>The header navigation: the current tab has the accent underline and semibold ink; the others are plain secondary text.</summary>
public class NavTabsStyleTests
{
    private static NavTabs Create(int selected)
    {
        var control = new NavTabs { Height = 52 };
        control.Items.Add(new ListBoxItem { Content = "Live" });
        control.Items.Add(new ListBoxItem { Content = "History" });
        control.Items.Add(new ListBoxItem { Content = "Settings" });
        UiHost.Prepare(control);
        control.SelectedIndex = selected;
        control.Measure(new Size(400, 52));
        control.Arrange(new Rect(0, 0, 400, 52));
        control.UpdateLayout();

        return control;
    }

    private static FrameworkElement Text(ListBoxItem item) => (FrameworkElement)System.Windows.Media.VisualTreeHelper.GetChild(System.Windows.Media.VisualTreeHelper.GetChild(item, 0), 0);

    private static Border Underline(NavTabs control) => (Border)control.Template.FindName("PART_Indicator", control);

    [Fact]
    public void The_current_tab_has_an_accent_underline_and_semibold_text()
    {
        UiHost.Run(() =>
        {
            var control = Create(1);
            var item = (ListBoxItem)control.Items[1];
            var underline = Underline(control);

            Assert.Equal(Visibility.Visible, underline.Visibility);
            Assert.Equal(18, underline.Width);
            Assert.Equal(3, underline.Height);
            Assert.Equal(((SolidColorBrush)Application.Current.FindResource("Accent.Brush")).Color, ((SolidColorBrush)underline.Background).Color);
            Assert.Equal(FontWeights.SemiBold, item.FontWeight);
        });
    }

    [Fact]
    public void The_underline_is_centred_under_the_current_tab_only()
    {
        UiHost.Run(() =>
        {
            var control = Create(1);

            var item = (ListBoxItem)control.Items[1];
            var underline = Underline(control);
            var underlineCentre = underline.TranslatePoint(new Point(underline.ActualWidth / 2, 0), control).X;
            var itemCentre = item.TranslatePoint(new Point(item.ActualWidth / 2, 0), control).X;

            Assert.Equal(itemCentre, underlineCentre, 0.5);
            Assert.NotEqual(FontWeights.SemiBold, ((ListBoxItem)control.Items[0]).FontWeight);
        });
    }

    [Fact]
    public void Tabs_touch_so_the_whole_row_is_clickable_and_their_texts_are_26_pixels_apart()
    {
        UiHost.Run(() =>
        {
            var control = Create(0);
            var first = (ListBoxItem)control.Items[0];
            var second = (ListBoxItem)control.Items[1];
            var itemGap = second.TranslatePoint(new Point(0, 0), control).X - first.TranslatePoint(new Point(first.ActualWidth, 0), control).X;
            var firstText = Text(first);
            var secondText = Text(second);
            var textGap = secondText.TranslatePoint(new Point(0, 0), control).X - firstText.TranslatePoint(new Point(firstText.ActualWidth, 0), control).X;

            // The spacing is padding inside each tab, so the click target is the text and its gaps, with no dead strip between tabs.
            Assert.Equal(0, itemGap, 0.5);
            Assert.Equal(26, textGap, 0.5);
            Assert.True(first.ActualWidth > firstText.ActualWidth + 25);
        });
    }
}
