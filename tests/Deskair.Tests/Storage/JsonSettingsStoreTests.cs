using System.IO;
using Deskair.Services.Settings;
using Deskair.Storage.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Storage;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly TempKnownFolders folders = new();
    private readonly JsonSettingsStore store;

    public JsonSettingsStoreTests()
    {
        store = new JsonSettingsStore(folders);
    }

    private string SettingsPath => Path.Combine(folders.DataFolder, "settings.json");

    public void Dispose()
    {
        folders.Dispose();
    }

    [Fact]
    public async Task Load_returns_the_defaults_when_there_is_no_file()
    {
        var settings = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(AppSettings.Default, settings);
    }

    [Fact]
    public async Task Load_reads_back_what_Save_wrote()
    {
        var saved = new AppSettings(LanguageChoice.Ukrainian, ThemeChoice.Dark, false)
        {
            TemperatureUnit = Deskair.Domain.Metrics.TemperatureUnit.Fahrenheit,
            SelectedDevice = new Deskair.Domain.Readings.DeviceId("aranet4", "AA:02"),
            NotifyAtElevated = false,
            NotifyAtHigh = false,
            QuietHoursEnabled = true,
            QuietHoursStart = new TimeOnly(23, 30),
            QuietHoursEnd = new TimeOnly(6, 15),
            ShowValueOnTrayIcon = true,
            NightStart = new TimeOnly(23, 0),
            NightEnd = new TimeOnly(8, 30)
        };
        await store.SaveAsync(saved, CancellationToken.None);

        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(saved, loaded);
    }

    [Fact]
    public async Task Load_uses_the_default_for_a_setting_the_file_does_not_have_and_ignores_unknown_ones()
    {
        Directory.CreateDirectory(folders.DataFolder);
        await File.WriteAllTextAsync(SettingsPath, """{ "version": 1, "theme": "Dark", "somethingNew": 42 }""", TestContext.Current.CancellationToken);

        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(ThemeChoice.Dark, loaded.Theme);
        Assert.Equal(AppSettings.Default with { Theme = ThemeChoice.Dark }, loaded);
    }

    [Theory]
    [InlineData("{ this is not json")]
    [InlineData("")]
    [InlineData("""{ "theme": "NoSuchTheme" }""")]
    public async Task Load_returns_the_defaults_when_the_file_cannot_be_read(string content)
    {
        Directory.CreateDirectory(folders.DataFolder);
        await File.WriteAllTextAsync(SettingsPath, content, TestContext.Current.CancellationToken);

        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(AppSettings.Default, loaded);
    }

    [Fact]
    public async Task Save_stores_enums_as_text_so_the_file_stays_readable()
    {
        await store.SaveAsync(AppSettings.Default with { Theme = ThemeChoice.Dark }, CancellationToken.None);

        var text = await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken);

        Assert.Contains("\"theme\": \"Dark\"", text);
    }

    [Fact]
    public async Task Save_creates_the_data_folder_and_leaves_no_temporary_file()
    {
        await store.SaveAsync(AppSettings.Default, CancellationToken.None);

        Assert.Equal(new[] { SettingsPath }, Directory.GetFiles(folders.DataFolder));
    }

    [Fact]
    public async Task Save_replaces_the_previous_file()
    {
        await store.SaveAsync(AppSettings.Default with { AlertsEnabled = false }, CancellationToken.None);
        await store.SaveAsync(AppSettings.Default with { AlertsEnabled = true }, CancellationToken.None);

        Assert.True((await store.LoadAsync(CancellationToken.None)).AlertsEnabled);
    }

    [Fact]
    public async Task Save_that_fails_leaves_the_previous_file_untouched()
    {
        await store.SaveAsync(AppSettings.Default with { Theme = ThemeChoice.Dark }, CancellationToken.None);
        var before = await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken);

        // A directory where the temporary file should go makes writing it fail.
        Directory.CreateDirectory(SettingsPath + ".tmp");

        await Assert.ThrowsAnyAsync<Exception>(
            () => store.SaveAsync(AppSettings.Default with { Theme = ThemeChoice.Light }, CancellationToken.None));

        Assert.Equal(before, await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken));
    }
}
