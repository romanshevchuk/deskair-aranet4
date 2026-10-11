using System.Windows;
using System.Windows.Controls;
using Deskair.App.Localization;
using Deskair.App.Ui.Theme;
using Deskair.Services.Settings;

namespace Deskair.App.Ui.Views.Gallery;

public partial class GalleryWindow : Window
{
    private readonly ThemeService theme;
    private readonly LanguageService language;

    public GalleryWindow(ThemeService theme, LanguageService language)
    {
        this.theme = theme;
        this.language = language;
        InitializeComponent();
    }

    private void OnThemeSelected(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: ListBoxItem { Tag: ThemeChoice choice } })
        {
            theme.Apply(choice);
        }
    }

    private void OnLanguageSelected(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: ListBoxItem { Tag: LanguageChoice choice } })
        {
            language.Apply(choice);
        }
    }
}
