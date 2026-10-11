using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.Services.Messages;
using Deskair.Services.Settings;

namespace Deskair.App.Localization;

/// <summary>Turns the language setting into a culture and applies it without a restart.</summary>
public sealed class LanguageService
{
    private readonly Localizer localizer;
    private readonly IMessenger messenger;
    private readonly CultureInfo systemCulture;

    public LanguageService(Localizer localizer, IMessenger messenger, CultureInfo systemCulture)
    {
        this.localizer = localizer;
        this.messenger = messenger;
        this.systemCulture = systemCulture;
    }

    /// <summary>System follows Windows: Ukrainian if Windows is Ukrainian, otherwise English.</summary>
    public static CultureInfo Resolve(LanguageChoice choice, CultureInfo system)
    {
        var ukrainian = CultureInfo.GetCultureInfo("uk-UA");
        var english = CultureInfo.GetCultureInfo("en-US");

        return choice switch
        {
            LanguageChoice.Ukrainian => ukrainian,
            LanguageChoice.English => english,
            _ => system.TwoLetterISOLanguageName == "uk" ? ukrainian : english
        };
    }

    public void Apply(LanguageChoice choice)
    {
        var culture = Resolve(choice, systemCulture);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        localizer.SetCulture(culture);
        messenger.Send(new LanguageChanged(culture.Name));
    }
}
