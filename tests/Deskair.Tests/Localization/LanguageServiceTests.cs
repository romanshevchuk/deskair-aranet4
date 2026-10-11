using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.Services.Messages;
using Deskair.Services.Settings;

namespace Deskair.Tests.Localization;

public class LanguageServiceTests
{
    private static readonly CultureInfo englishWindows = CultureInfo.GetCultureInfo("en-GB");

    [Theory]
    [InlineData(LanguageChoice.English, "uk-UA", "en")]
    [InlineData(LanguageChoice.Ukrainian, "en-GB", "uk")]
    [InlineData(LanguageChoice.System, "uk-UA", "uk")]
    [InlineData(LanguageChoice.System, "en-GB", "en")]
    [InlineData(LanguageChoice.System, "de-DE", "en")]
    public void Resolve_follows_the_choice_and_falls_back_to_English(LanguageChoice choice, string windows, string expected)
    {
        var culture = LanguageService.Resolve(choice, CultureInfo.GetCultureInfo(windows));

        Assert.Equal(expected, culture.TwoLetterISOLanguageName);
    }

    [Fact]
    public void Apply_switches_the_localizer_and_sends_LanguageChanged()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        var originalDefault = CultureInfo.DefaultThreadCurrentCulture;
        var originalDefaultUi = CultureInfo.DefaultThreadCurrentUICulture;

        try
        {
            var localizer = new Localizer();
            var messenger = new StrongReferenceMessenger();
            var received = new List<string>();
            messenger.Register<LanguageChanged>(this, (_, message) => received.Add(message.CultureName));
            var service = new LanguageService(localizer, messenger, englishWindows);

            service.Apply(LanguageChoice.Ukrainian);

            Assert.Equal("Наживо", localizer.Get("Nav_Live"));
            Assert.Equal(new[] { "uk-UA" }, received);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
            CultureInfo.DefaultThreadCurrentCulture = originalDefault;
            CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUi;
        }
    }

}
