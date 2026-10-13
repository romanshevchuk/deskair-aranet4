using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

/// <summary>A setting: the label (and an optional description under it) on the left, the content (toggle, segmented control or button) on the right.</summary>
public class SettingsRow : HeaderedContentControl
{
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description),
        typeof(string),
        typeof(SettingsRow),
        new PropertyMetadata(null));

    static SettingsRow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SettingsRow), new FrameworkPropertyMetadata(typeof(SettingsRow)));
    }

    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
}
