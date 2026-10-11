using System.Windows;
using System.Windows.Controls;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-07) [M, haiku]
// Spec: docs/design/components.md "Window chrome" (room chip), "Room menu"
// Input : Room name, value text, zone, open state.
// Output: A rounded chip with a StatusDot, room name and value; clicking toggles IsOpen.
// Rules : Uses StatusDot and Pill tokens; opens RoomMenu.
// Edge  : Long room names trim with an ellipsis and show a tooltip.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class RoomChip : Button
{
    public static readonly DependencyProperty RoomNameProperty = DependencyProperty.Register(
        nameof(RoomName),
        typeof(string),
        typeof(RoomChip),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register(
        nameof(ValueText),
        typeof(string),
        typeof(RoomChip),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ZoneProperty = DependencyProperty.Register(
        nameof(Zone),
        typeof(Zone?),
        typeof(RoomChip),
        new PropertyMetadata(null));

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen),
        typeof(bool),
        typeof(RoomChip),
        new PropertyMetadata(default(bool)));

    static RoomChip()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(RoomChip), new FrameworkPropertyMetadata(typeof(RoomChip)));
    }

    public string? RoomName
    {
        get => (string?)GetValue(RoomNameProperty);
        set => SetValue(RoomNameProperty, value);
    }

    public string? ValueText
    {
        get => (string?)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public Zone? Zone
    {
        get => (Zone?)GetValue(ZoneProperty);
        set => SetValue(ZoneProperty, value);
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }
}
