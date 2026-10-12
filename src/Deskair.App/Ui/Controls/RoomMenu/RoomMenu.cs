using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Deskair.App.Ui.Controls;

/// <summary>
/// A popup that lists rooms (items with a name, value text and zone). Choosing a room runs <see cref="RoomChosenCommand"/> with the room and closes the popup.
/// Esc and a click outside close it; the arrow keys move between rooms.
/// </summary>
public class RoomMenu : ItemsControl
{
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen),
        typeof(bool),
        typeof(RoomMenu),
        new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty RoomChosenCommandProperty = DependencyProperty.Register(
        nameof(RoomChosenCommand),
        typeof(ICommand),
        typeof(RoomMenu),
        new PropertyMetadata(null));

    private Popup? popup;

    static RoomMenu()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(RoomMenu), new FrameworkPropertyMetadata(typeof(RoomMenu)));
    }

    public RoomMenu()
    {
        AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnRoomClicked));
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public ICommand? RoomChosenCommand
    {
        get => (ICommand?)GetValue(RoomChosenCommandProperty);
        set => SetValue(RoomChosenCommandProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (popup is not null)
        {
            popup.PreviewKeyDown -= OnPopupKeyDown;
            popup.Opened -= OnPopupOpened;
        }

        popup = GetTemplateChild("PART_Popup") as Popup;

        if (popup is not null)
        {
            popup.PreviewKeyDown += OnPopupKeyDown;
            popup.Opened += OnPopupOpened;
        }
    }

    private void OnRoomClicked(object sender, RoutedEventArgs args)
    {
        if (args.OriginalSource is not FrameworkElement { DataContext: { } room })
        {
            return;
        }

        if (RoomChosenCommand?.CanExecute(room) == true)
        {
            RoomChosenCommand.Execute(room);
        }

        IsOpen = false;
    }

    private void OnPopupKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key == Key.Escape)
        {
            IsOpen = false;
            args.Handled = true;
        }
    }

    private void OnPopupOpened(object? sender, EventArgs args)
    {
        popup?.Child?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }
}
