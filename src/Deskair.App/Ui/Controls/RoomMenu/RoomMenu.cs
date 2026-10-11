using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-08) [M, haiku]
// Spec: docs/design/components.md "Room menu"; docs/design/accessibility.md (Esc closes popups)
// Input : Rooms (items with name, value and zone) and IsOpen.
// Output: A Popup below the chip listing rooms with value and a StatusDot; choosing a room closes it.
// Rules : Esc and click-away close it; arrow keys move between rooms.
// Edge  : One room still shows the menu; no rooms shows nothing.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class RoomMenu : ItemsControl
{
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen),
        typeof(bool),
        typeof(RoomMenu),
        new PropertyMetadata(default(bool)));

    static RoomMenu()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(RoomMenu), new FrameworkPropertyMetadata(typeof(RoomMenu)));
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }
}
