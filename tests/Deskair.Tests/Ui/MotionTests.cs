using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Deskair.App.Ui.Controls;
using Deskair.App.Ui.Motion;

namespace Deskair.Tests.Ui;

/// <summary>Motion: the policy that switches animation off, where the selection indicator goes, and that it lands on the chosen item.</summary>
public class MotionTests
{
    [Fact]
    public void A_segment_indicator_covers_the_item_and_an_underline_is_centred_with_its_own_width()
    {
        var item = new Rect(40, 0, 80, 30);

        Assert.Equal((40.0, 80.0), SlideMath.Target(item));
        Assert.Equal((71.0, 18.0), SlideMath.Target(item, 18));
    }

    [Fact]
    public void The_override_replaces_the_windows_setting_and_zeroes_every_duration()
    {
        UiHost.Run(() =>
        {
            Assert.False(MotionPolicy.Enabled);
            Assert.Equal(TimeSpan.Zero, MotionPolicy.Of("Motion.Selection").TimeSpan);

            MotionPolicy.Override = true;

            try
            {
                Assert.True(MotionPolicy.Enabled);
                Assert.Equal(TimeSpan.FromMilliseconds(150), MotionPolicy.Of("Motion.Selection").TimeSpan);
                Assert.Equal(TimeSpan.FromMilliseconds(400), MotionPolicy.Of("Motion.Glide").TimeSpan);
            }
            finally
            {
                MotionPolicy.Override = false;
            }
        });
    }

    [Fact]
    public void The_indicator_sits_on_the_selected_segment_and_moves_with_the_selection_at_once_when_animations_are_off()
    {
        UiHost.Run(() =>
        {
            var control = new SegmentedControl();
            control.Items.Add(new ListBoxItem { Content = "Day" });
            control.Items.Add(new ListBoxItem { Content = "Week" });
            control.Items.Add(new ListBoxItem { Content = "Month" });
            UiHost.Prepare(control);
            control.SelectedIndex = 0;
            Layout(control);

            var indicator = (Border)control.Template.FindName("PART_Indicator", control);
            var item = (ListBoxItem)control.Items[0];

            Assert.Equal(Visibility.Visible, indicator.Visibility);
            Assert.Equal(item.ActualWidth, indicator.Width, 0.5);

            control.SelectedIndex = 2;
            Layout(control);
            item = (ListBoxItem)control.Items[2];

            var expected = item.TranslatePoint(new Point(0, 0), (UIElement)indicator.Parent).X;

            Assert.Equal(expected, ((TranslateTransform)indicator.RenderTransform).X, 0.5);
            Assert.Equal(item.ActualWidth, indicator.Width, 0.5);
        });
    }

    [Fact]
    public void Without_a_selection_there_is_no_indicator()
    {
        UiHost.Run(() =>
        {
            var control = new SegmentedControl();
            control.Items.Add(new ListBoxItem { Content = "Day" });
            UiHost.Prepare(control);
            Layout(control);

            Assert.Equal(Visibility.Collapsed, ((Border)control.Template.FindName("PART_Indicator", control)).Visibility);
        });
    }

    [Fact]
    public void Changing_text_does_not_dim_it_while_animations_are_off()
    {
        UiHost.Run(() =>
        {
            var block = new TextBlock { Text = "800" };
            FadeOnChange.SetIsEnabled(block, true);

            block.Text = "950";

            Assert.Equal(1, block.Opacity);
            Assert.False(block.HasAnimatedProperties);
        });
    }

    private static void Layout(FrameworkElement control)
    {
        control.Measure(new Size(400, 100));
        control.Arrange(new Rect(0, 0, control.DesiredSize.Width, control.DesiredSize.Height));
        control.UpdateLayout();
    }
}
