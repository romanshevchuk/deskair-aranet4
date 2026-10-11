using System.Globalization;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Deskair.Tests.Ui;

public class LocalizationMarkupTests
{
    private const string Xaml = """
        <TextBlock xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                   xmlns:loc="clr-namespace:Deskair.App.Ui.Markup;assembly=Deskair.App"
                   Text="{loc:T Nav_Live}" />
        """;

    [Fact]
    public void T_shows_the_text_for_the_current_language()
    {
        UiHost.Run(() =>
        {
            var block = (TextBlock)XamlReader.Parse(Xaml);

            Assert.Equal("Live", block.Text);
        });
    }

    [Fact]
    public void T_refreshes_when_the_language_changes_without_a_restart()
    {
        UiHost.Run(() =>
        {
            var block = (TextBlock)XamlReader.Parse(Xaml);

            UiHost.Localizer.SetCulture(CultureInfo.GetCultureInfo("uk-UA"));

            Assert.Equal("Наживо", block.Text);

            UiHost.Localizer.SetCulture(CultureInfo.GetCultureInfo("en-US"));

            Assert.Equal("Live", block.Text);
        });
    }

    [Fact]
    public void T_shows_the_key_in_brackets_when_it_is_missing()
    {
        UiHost.Run(() =>
        {
            var block = (TextBlock)XamlReader.Parse(Xaml.Replace("Nav_Live", "No_Such_Key"));

            Assert.Equal("[No_Such_Key]", block.Text);
        });
    }
}
