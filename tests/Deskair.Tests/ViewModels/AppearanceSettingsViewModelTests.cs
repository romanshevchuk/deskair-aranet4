using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Settings;
using Deskair.Domain.Metrics;
using Deskair.Services.Messages;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

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
        viewModel = new AppearanceSettingsViewModel(settings);
    }

    [Fact]
    public async Task Load_shows_the_current_settings()
    {
        store.Stored = AppSettings.Default with { Language = LanguageChoice.Ukrainian, Theme = ThemeChoice.Dark, TemperatureUnit = Deskair.Domain.Metrics.TemperatureUnit.Fahrenheit };
        await settings.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.Load();

        Assert.Equal(LanguageChoice.Ukrainian, viewModel.Language);
        Assert.Equal(ThemeChoice.Dark, viewModel.Theme);
        Assert.Equal(Deskair.Domain.Metrics.TemperatureUnit.Fahrenheit, viewModel.TemperatureUnit);
    }

    [Fact]
    public async Task Apply_saves_and_announces_the_new_values()
    {
        SettingsChanged? received = null;
        messenger.Register<SettingsChanged>(this, (_, message) => received = message);
        viewModel.Language = LanguageChoice.English;
        viewModel.Theme = ThemeChoice.Light;
        viewModel.TemperatureUnit = TemperatureUnit.Fahrenheit;

        await viewModel.ApplyAsync(TestContext.Current.CancellationToken);

        var saved = Assert.Single(store.Saved);
        Assert.Equal(LanguageChoice.English, saved.Language);
        Assert.Equal(ThemeChoice.Light, saved.Theme);
        Assert.Equal(TemperatureUnit.Fahrenheit, saved.TemperatureUnit);
        Assert.Equal(saved, received?.Settings);
    }

    [Fact]
    public async Task Apply_keeps_the_other_settings()
    {
        store.Stored = AppSettings.Default with { AlertsEnabled = false };
        await settings.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.Load();
        viewModel.Theme = ThemeChoice.Dark;

        await viewModel.ApplyAsync(TestContext.Current.CancellationToken);

        Assert.False(Assert.Single(store.Saved).AlertsEnabled);
    }

    [Fact]
    public async Task Unchanged_values_do_nothing()
    {
        viewModel.Load();

        await viewModel.ApplyAsync(TestContext.Current.CancellationToken);

        Assert.Empty(store.Saved);
    }
}
