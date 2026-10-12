using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Deskair.App.Ui.Controls;
using Deskair.App.ViewModels.Shell;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;

namespace Deskair.Tests.Ui;

public class LiveControlsTests
{
    private static T Part<T>(Control control, string name)
        where T : class
        => (T)control.Template.FindName(name, control);

    private sealed class ClickableRoomChip : RoomChip
    {
        public void Press() => OnClick();
    }

    private sealed class CountingCommand : ICommand
    {
        public int Runs { get; private set; }

        public object? LastParameter { get; private set; }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            Runs++;
            LastParameter = parameter;
        }
    }

    [Theory]
    [InlineData(100, 19.0, BatteryLevel.Good)]
    [InlineData(50, 9.5, BatteryLevel.Good)]
    [InlineData(21, 3.99, BatteryLevel.Good)]
    [InlineData(20, 3.8, BatteryLevel.Low)]
    [InlineData(11, 2.09, BatteryLevel.Low)]
    [InlineData(10, 2.0, BatteryLevel.Critical)]
    [InlineData(1, 2.0, BatteryLevel.Critical)]
    [InlineData(0, 2.0, BatteryLevel.Critical)]
    public void Battery_fill_and_colour_follow_the_percent(int percent, double fill, BatteryLevel level)
    {
        UiHost.Run(() =>
        {
            var icon = UiHost.Prepare(new BatteryIcon { Percent = percent });

            Assert.Equal(fill, icon.FillWidth, 2);
            Assert.Equal(level, icon.Level);
        });
    }

    [Theory]
    [InlineData(-5, BatteryLevel.Critical, 2.0)]
    [InlineData(250, BatteryLevel.Good, 19.0)]
    public void Battery_percent_is_clamped_to_zero_to_one_hundred(int percent, BatteryLevel level, double fill)
    {
        UiHost.Run(() =>
        {
            var icon = UiHost.Prepare(new BatteryIcon { Percent = percent });

            Assert.Equal(level, icon.Level);
            Assert.Equal(fill, icon.FillWidth, 2);
        });
    }

    [Fact]
    public void Battery_with_an_unknown_percent_draws_no_fill()
    {
        UiHost.Run(() =>
        {
            var icon = UiHost.Prepare(new BatteryIcon { Percent = 40 });

            icon.Percent = null;

            Assert.Equal(BatteryLevel.Unknown, icon.Level);
            Assert.Equal(0, icon.FillWidth);
            Assert.Equal(Visibility.Collapsed, Part<FrameworkElement>(icon, "Fill").Visibility);
        });
    }

    [Fact]
    public void Advice_card_without_a_zone_has_no_icon()
    {
        UiHost.Run(() =>
        {
            var card = UiHost.Prepare(new AdviceCard { Title = "t", Text = "x" });

            Assert.Equal(Visibility.Collapsed, Part<FrameworkElement>(card, "Badge").Visibility);
        });
    }

    [Theory]
    [InlineData(Zone.Good, "GoodIcon")]
    [InlineData(Zone.Elevated, "ElevatedIcon")]
    [InlineData(Zone.High, "HighIcon")]
    public void Advice_card_shows_the_icon_of_its_zone(Zone zone, string icon)
    {
        UiHost.Run(() =>
        {
            var card = UiHost.Prepare(new AdviceCard { Zone = zone });

            Assert.Equal(Visibility.Visible, Part<FrameworkElement>(card, "Badge").Visibility);
            Assert.Equal(Visibility.Visible, Part<FrameworkElement>(card, icon).Visibility);

            foreach (var other in new[] { "GoodIcon", "ElevatedIcon", "HighIcon" }.Where(name => name != icon))
            {
                Assert.Equal(Visibility.Collapsed, Part<FrameworkElement>(card, other).Visibility);
            }
        });
    }

    [Fact]
    public void Metric_card_without_a_value_hides_the_unit_and_never_shows_zero()
    {
        UiHost.Run(() =>
        {
            var card = UiHost.Prepare(new MetricCard { Title = "CO₂", Unit = "ppm" });

            Assert.Equal(Visibility.Collapsed, Part<FrameworkElement>(card, "Unit").Visibility);
            Assert.Null(card.Value);
        });
    }

    [Fact]
    public void Metric_card_is_a_button_so_it_can_be_used_with_the_keyboard()
    {
        UiHost.Run(() =>
        {
            var card = UiHost.Prepare(new MetricCard());

            Assert.IsAssignableFrom<Button>(card);
            Assert.True(card.Focusable);
        });
    }

    [Fact]
    public void Banner_with_only_a_message_shows_no_buttons()
    {
        UiHost.Run(() =>
        {
            var banner = UiHost.Prepare(new Banner { Message = "m" });

            Assert.Equal(Visibility.Collapsed, Part<FrameworkElement>(banner, "Action").Visibility);
            Assert.Equal(Visibility.Collapsed, Part<FrameworkElement>(banner, "Dismiss").Visibility);
        });
    }

    [Fact]
    public void Banner_shows_the_buttons_it_has_commands_for()
    {
        UiHost.Run(() =>
        {
            var banner = UiHost.Prepare(new Banner { Message = "m", ActionText = "Retry", ActionCommand = new CountingCommand() });

            Assert.Equal(Visibility.Visible, Part<FrameworkElement>(banner, "Action").Visibility);
            Assert.Equal(Visibility.Collapsed, Part<FrameworkElement>(banner, "Dismiss").Visibility);
        });
    }

    [Fact]
    public void Room_chip_click_toggles_the_menu()
    {
        UiHost.Run(() =>
        {
            var chip = new ClickableRoomChip();

            chip.Press();
            Assert.True(chip.IsOpen);

            chip.Press();
            Assert.False(chip.IsOpen);
        });
    }

    [Fact]
    public void Room_chip_without_a_value_hides_the_value_text()
    {
        UiHost.Run(() =>
        {
            var chip = UiHost.Prepare(new RoomChip { RoomName = "Office" });

            Assert.Equal(Visibility.Collapsed, Part<FrameworkElement>(chip, "Value").Visibility);
        });
    }

    [Fact]
    public void Choosing_a_room_runs_the_command_with_the_room_and_closes_the_menu()
    {
        UiHost.Run(() =>
        {
            var command = new CountingCommand();
            var room = new RoomItem(new DeviceId("test", "A1"), "Office", "612 ppm", Zone.Good);
            var menu = UiHost.Prepare(new RoomMenu { IsOpen = true, RoomChosenCommand = command, ItemsSource = new[] { room } });
            var button = new Button { DataContext = room };

            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
            menu.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));

            Assert.Equal(1, command.Runs);
            Assert.Same(room, command.LastParameter);
            Assert.False(menu.IsOpen);
        });
    }

    [Fact]
    public void Room_menu_without_rooms_does_not_open()
    {
        UiHost.Run(() =>
        {
            var menu = UiHost.Prepare(new RoomMenu { IsOpen = true });
            var popup = Part<System.Windows.Controls.Primitives.Popup>(menu, "PART_Popup");

            Assert.False(popup.IsOpen);
        });
    }

    [Fact]
    public void Buttons_are_dimmed_when_disabled()
    {
        UiHost.Run(() =>
        {
            var primary = UiHost.Prepare(new PrimaryButton { IsEnabled = false });
            var secondary = UiHost.Prepare(new SecondaryButton { IsEnabled = false });

            Assert.Equal(0.5, Part<FrameworkElement>(primary, "Frame").Opacity);
            Assert.Equal(0.5, Part<FrameworkElement>(secondary, "Frame").Opacity);
        });
    }

    [Fact]
    public void Key_value_cell_swaps_the_order_when_the_value_comes_first()
    {
        UiHost.Run(() =>
        {
            var labelFirst = UiHost.Prepare(new KeyValueCell { Label = "l", Value = "v" });
            var valueFirst = UiHost.Prepare(new KeyValueCell { Label = "l", Value = "v", ValueFirst = true });

            Assert.Equal(0, Grid.GetRow(Part<FrameworkElement>(labelFirst, "Label")));
            Assert.Equal(1, Grid.GetRow(Part<FrameworkElement>(labelFirst, "Value")));
            Assert.Equal(0, Grid.GetRow(Part<FrameworkElement>(valueFirst, "Value")));
            Assert.Equal(1, Grid.GetRow(Part<FrameworkElement>(valueFirst, "Label")));
        });
    }
}
