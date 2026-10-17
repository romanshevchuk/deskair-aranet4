using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Deskair.App.Ui.Controls;
using Deskair.App.Ui.Motion;
using Deskair.App.Ui.Views.Settings;
using Deskair.App.Ui.Views.Shell;
using Deskair.Domain.Metrics;
using Deskair.Services.Settings;

namespace Deskair.Tests.Ui;

/// <summary>The segmented controls of the Settings screen show their selection the first time the screen opens (bug: the temperature unit had bold text but no highlight).</summary>
public class SettingsIndicatorTests
{
    private sealed class Model
    {
        public LanguageChoice Language { get; set; } = LanguageChoice.Ukrainian;

        public ThemeChoice Theme { get; set; } = ThemeChoice.System;

        public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Celsius;

        public bool ShowValueOnTrayIcon { get; set; }
    }

    [Fact]
    public void Every_segmented_control_has_a_visible_indicator_when_the_screen_first_opens()
    {
        UiHost.Run(() =>
        {
            MotionPolicy.Override = true;

            var window = new Window { Width = 900, Height = 600, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -4000 };

            try
            {
                window.Content = new AppearanceSettingsView { DataContext = new Model() };
                window.Show();
                Pump();

                var controls = Descendants<SegmentedControl>(window).ToList();

                Assert.Equal(3, controls.Count);
                Assert.All(controls, AssertIndicatorIsOnTheSelection);
            }
            finally
            {
                window.Close();
                MotionPolicy.Override = false;
            }
        });
    }

    [Fact]
    public void The_real_settings_screen_shows_the_selection_of_every_segmented_control()
    {
        UiHost.Run(() =>
        {
            var shell = SettingsRenderTests.BuildShell(ThemeChoice.Light, "en-US");
            var window = new MainWindow(shell);
            var content = (FrameworkElement)window.Content;

            content.DataContext = shell;
            window.Content = null;

            // The same path as the render tests: laid out inside a host, and examined before it is taken out again.
            var host = new Border { Width = 1200, Height = 700, Child = content, UseLayoutRounding = true };
            host.Measure(new Size(1200, 700));
            host.Arrange(new Rect(host.DesiredSize));
            host.UpdateLayout();

            var controls = Descendants<SegmentedControl>(content).ToList();

            Assert.Equal(3, controls.Count);
            Assert.All(controls, AssertIndicatorIsOnTheSelection);
        });
    }

    [Fact]
    public void The_indicator_comes_back_when_the_template_is_applied_again()
    {
        UiHost.Run(() =>
        {
            MotionPolicy.Override = true;

            var window = new Window { Width = 900, Height = 600, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -4000 };

            try
            {
                window.Content = new AppearanceSettingsView { DataContext = new Model() };
                window.Show();
                Pump();

                var control = Descendants<SegmentedControl>(window).Last();
                var template = control.Template;

                // A control gets a second template when its style arrives late or the theme changes the style.
                control.Template = null;
                control.Template = template;
                control.ApplyTemplate();
                Pump();

                AssertIndicatorIsOnTheSelection(control);
            }
            finally
            {
                window.Close();
                MotionPolicy.Override = false;
            }
        });
    }

    private static void AssertIndicatorIsOnTheSelection(SegmentedControl control)
    {
        var indicator = (Border)control.Template.FindName("PART_Indicator", control);
        var item = (FrameworkElement)control.ItemContainerGenerator.ContainerFromIndex(control.SelectedIndex);
        var host = (UIElement)indicator.Parent;
        var itemLeft = item.TranslatePoint(new Point(0, 0), host).X;
        var indicatorLeft = indicator.TranslatePoint(new Point(0, 0), host).X;

        Assert.True(
            indicator.Visibility == Visibility.Visible && Math.Abs(indicatorLeft - itemLeft) < 1 && Math.Abs(indicator.ActualWidth - item.ActualWidth) < 1,
            $"{control.SelectedValue}: indicator {indicator.Visibility} at {indicatorLeft:0.0} width {indicator.ActualWidth:0.0}; item at {itemLeft:0.0} width {item.ActualWidth:0.0}");
    }

    private static void Pump()
    {
        // Let layout, the loaded event and the animations of the first frames run.
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);

            if (child is T match)
            {
                yield return match;
            }

            foreach (var inner in Descendants<T>(child))
            {
                yield return inner;
            }
        }
    }
}
