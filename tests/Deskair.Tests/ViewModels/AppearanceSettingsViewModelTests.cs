using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Settings;
using Deskair.Domain.Metrics;
using Deskair.Services.Messages;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.ViewModels;

public class AppearanceSettingsViewModelTests
{
    private readonly FakeSettingsStore store = new();
    private readonly StrongReferenceMessenger messenger = new();
    private readonly SettingsService settings;
    private readonly AppearanceSettingsViewModel viewModel;

    public AppearanceSettingsViewModelTests()
    {
        settings = new SettingsService(store, messenger);
        viewModel = new AppearanceSettingsViewModel(settings, messenger, new FakeUiDispatcher(), NullLogger<AppearanceSettingsViewModel>.Instance);
    }

    [Fact]
    public async Task Load_shows_the_current_settings()
    {
        store.Stored = AppSettings.Default with { Language = LanguageChoice.Ukrainian, Theme = ThemeChoice.Dark, TemperatureUnit = Deskair.Domain.Metrics.TemperatureUnit.Fahrenheit, ShowValueOnTrayIcon = true };
        await settings.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.Load();

        Assert.Equal(LanguageChoice.Ukrainian, viewModel.Language);
        Assert.Equal(ThemeChoice.Dark, viewModel.Theme);
        Assert.Equal(Deskair.Domain.Metrics.TemperatureUnit.Fahrenheit, viewModel.TemperatureUnit);
        Assert.True(viewModel.ShowValueOnTrayIcon);
    }

    [Fact]
    public async Task A_change_on_the_screen_is_saved_and_announced_at_once()
    {
        await settings.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.Load();
        SettingsChanged? received = null;
        messenger.Register<SettingsChanged>(this, (_, message) => received = message);
        viewModel.Language = LanguageChoice.English;
        viewModel.Theme = ThemeChoice.Light;
        viewModel.TemperatureUnit = TemperatureUnit.Fahrenheit;
        viewModel.ShowValueOnTrayIcon = true;

        var saved = store.Stored;
        Assert.Equal(LanguageChoice.English, saved.Language);
        Assert.Equal(ThemeChoice.Light, saved.Theme);
        Assert.Equal(TemperatureUnit.Fahrenheit, saved.TemperatureUnit);
        Assert.True(saved.ShowValueOnTrayIcon);
        Assert.Equal(saved, received?.Settings);
    }

    [Fact]
    public async Task A_change_keeps_the_other_settings()
    {
        store.Stored = AppSettings.Default with { AlertsEnabled = false };
        await settings.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.Load();
        viewModel.Theme = ThemeChoice.Dark;

        Assert.False(Assert.Single(store.Saved).AlertsEnabled);
    }

    [Fact]
    public void Unchanged_values_do_nothing()
    {
        viewModel.Load();

        Assert.Empty(store.Saved);
    }

    [Fact]
    public async Task A_change_made_elsewhere_such_as_the_tray_menu_shows_on_the_screen()
    {
        viewModel.Load();

        await settings.UpdateAsync(current => current with { ShowValueOnTrayIcon = true }, TestContext.Current.CancellationToken);

        Assert.True(viewModel.ShowValueOnTrayIcon);
    }

    [Fact]
    public async Task Changing_the_theme_does_not_undo_a_tray_menu_change_made_since_the_screen_loaded()
    {
        viewModel.Load();
        await settings.UpdateAsync(current => current with { ShowValueOnTrayIcon = true }, TestContext.Current.CancellationToken);

        viewModel.Theme = ThemeChoice.Dark;

        Assert.True(store.Stored.ShowValueOnTrayIcon);
        Assert.Equal(ThemeChoice.Dark, store.Stored.Theme);
    }
}
