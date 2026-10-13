using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Composition;
using Deskair.App.Localization;
using Deskair.Services.Messages;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Ui;

public class SettingsApplierTests
{
    [Fact]
    public void A_settings_change_switches_the_language_and_the_theme_at_once()
    {
        UiHost.Run(() =>
        {
            var messenger = new StrongReferenceMessenger();
            var language = new LanguageService(UiHost.Localizer, messenger, CultureInfo.GetCultureInfo("en-US"));
            var applier = new SettingsApplier(language, UiHost.Theme, messenger, new FakeUiDispatcher());
            using var stop = new CancellationTokenSource();
            applier.Start(stop.Token);

            try
            {
                messenger.Send(new SettingsChanged(AppSettings.Default with { Language = LanguageChoice.Ukrainian, Theme = ThemeChoice.Dark }));

                Assert.True(UiHost.Theme.IsDark);
                Assert.Equal("uk", UiHost.Localizer.Culture.TwoLetterISOLanguageName);

                messenger.Send(new SettingsChanged(AppSettings.Default with { Language = LanguageChoice.English, Theme = ThemeChoice.Light }));

                Assert.False(UiHost.Theme.IsDark);
                Assert.Equal("en", UiHost.Localizer.Culture.TwoLetterISOLanguageName);
            }
            finally
            {
                stop.Cancel();
                language.Apply(LanguageChoice.English);
            }
        });
    }

    [Fact]
    public void After_it_is_stopped_a_settings_change_does_nothing()
    {
        UiHost.Run(() =>
        {
            var messenger = new StrongReferenceMessenger();
            var language = new LanguageService(UiHost.Localizer, messenger, CultureInfo.GetCultureInfo("en-US"));
            var applier = new SettingsApplier(language, UiHost.Theme, messenger, new FakeUiDispatcher());
            using var stop = new CancellationTokenSource();
            applier.Start(stop.Token);
            stop.Cancel();

            messenger.Send(new SettingsChanged(AppSettings.Default with { Theme = ThemeChoice.Dark }));

            Assert.False(UiHost.Theme.IsDark);
        });
    }
}
