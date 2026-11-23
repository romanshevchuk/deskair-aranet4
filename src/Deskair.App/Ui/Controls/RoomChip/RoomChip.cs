using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

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

    protected override void OnClick()
    {
        IsOpen = !IsOpen;
        base.OnClick();
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }
}
