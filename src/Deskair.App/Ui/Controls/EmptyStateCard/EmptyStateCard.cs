using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

public class EmptyStateCard : ContentControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(EmptyStateCard),
        new PropertyMetadata(null));

    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message),
        typeof(string),
        typeof(EmptyStateCard),
        new PropertyMetadata(null));

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon),
        typeof(object),
        typeof(EmptyStateCard),
        new PropertyMetadata(null));

    static EmptyStateCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(EmptyStateCard), new FrameworkPropertyMetadata(typeof(EmptyStateCard)));
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public object? Icon
    {
        get => (object?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}
