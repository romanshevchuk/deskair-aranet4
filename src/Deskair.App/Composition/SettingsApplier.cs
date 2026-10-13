using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.Ui.Theme;
using Deskair.Services.Messages;
using Deskair.Services.Platform;
using Deskair.Services.Settings;

namespace Deskair.App.Composition;

/// <summary>Makes the app follow a settings change at once: the language and the theme are applied on the UI thread. The values and units refresh through their own view models.</summary>
public sealed class SettingsApplier
{
    private readonly LanguageService language;
    private readonly ThemeService theme;
    private readonly IMessenger messenger;
    private readonly IUiDispatcher dispatcher;

    public SettingsApplier(LanguageService language, ThemeService theme, IMessenger messenger, IUiDispatcher dispatcher)
    {
        this.language = language;
        this.theme = theme;
        this.messenger = messenger;
        this.dispatcher = dispatcher;
    }

    /// <summary>Starts following <see cref="SettingsChanged"/> until cancelled.</summary>
    public void Start(CancellationToken cancellationToken)
    {
        messenger.UnregisterAll(this);
        messenger.Register<SettingsApplier, SettingsChanged>(this, static (self, message) => self.Apply(message.Settings));
        cancellationToken.Register(() => messenger.UnregisterAll(this));
    }

    private void Apply(AppSettings settings)
    {
        dispatcher.Post(() =>
        {
            language.Apply(settings.Language);
            theme.Apply(settings.Theme);
        });
    }
}
