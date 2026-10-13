using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Views.History;

public partial class TypicalDayCard : UserControl
{
    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle),
        typeof(string),
        typeof(TypicalDayCard),
        new PropertyMetadata(null));

    public TypicalDayCard()
    {
        InitializeComponent();
    }

    /// <summary>The grey text after the title ("average by hour, last 7 days"). Already localized.</summary>
    public string? Subtitle
    {
        get => (string?)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }
}
