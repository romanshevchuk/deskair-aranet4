using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Settings;
using Deskair.Domain.Readings;
using Deskair.Services.Devices;
using Deskair.Services.Import;
using Deskair.Services.Settings;
using Deskair.Storage.Export;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.ViewModels;

public sealed class SettingsViewModelTests : IDisposable
{
    private readonly FakeSettingsStore settingsStore = new();
    private readonly FakeDeviceStore deviceStore = new();
    private readonly FakeStartupRegistration registration = new();
    private readonly TempKnownFolders folders = new();
    private readonly SettingsService settings;
    private readonly AppearanceSettingsViewModel appearance;
    private readonly AlertsSettingsViewModel alerts;
    private readonly DevicesSettingsViewModel devices;
    private readonly DataSettingsViewModel data;
    private readonly StartupSettingsViewModel startup;
    private readonly SettingsViewModel viewModel;

    public SettingsViewModelTests()
    {
        settings = new SettingsService(settingsStore, new StrongReferenceMessenger());
        appearance = new AppearanceSettingsViewModel(settings);
        alerts = new AlertsSettingsViewModel(settings);
        devices = new DevicesSettingsViewModel(new DeviceRegistry(deviceStore));
        var readings = new FakeReadingStore();
        data = new DataSettingsViewModel(
            new ImportService(readings, TimeZoneInfo.Utc),
            new ExportService(readings, new CsvExporter(), TimeZoneInfo.Utc),
            folders);
        startup = new StartupSettingsViewModel(registration, NullLogger<StartupSettingsViewModel>.Instance);
        viewModel = new SettingsViewModel(appearance, alerts, devices, data, startup);
    }

    public void Dispose()
    {
        folders.Dispose();
    }

    [Fact]
    public async Task Load_fills_every_section()
    {
        settingsStore.Stored = AppSettings.Default with { Theme = ThemeChoice.Dark, AlertsEnabled = false };
        await settings.LoadAsync(TestContext.Current.CancellationToken);
        deviceStore.Devices.Add(new DeviceInfo(new DeviceId("test", "A1"), "Office", null, [], null));
        registration.IsEnabled = true;

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ThemeChoice.Dark, appearance.Theme);
        Assert.False(alerts.AlertsEnabled);
        Assert.Single(devices.Items);
        Assert.Equal(folders.DataFolder, data.DataFolder);
        Assert.True(startup.StartWithWindows);
    }

    [Fact]
    public async Task One_failing_section_does_not_stop_the_others()
    {
        settingsStore.Stored = AppSettings.Default with { Theme = ThemeChoice.Dark };
        await settings.LoadAsync(TestContext.Current.CancellationToken);
        var failing = new DevicesSettingsViewModel(new DeviceRegistry(new ThrowingDeviceStore()));
        var model = new SettingsViewModel(appearance, alerts, failing, data, startup);
        registration.IsEnabled = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => model.LoadAsync(TestContext.Current.CancellationToken));

        Assert.Equal(ThemeChoice.Dark, appearance.Theme);
        Assert.Equal(folders.DataFolder, data.DataFolder);
        Assert.True(startup.StartWithWindows);
    }

    private sealed class ThrowingDeviceStore : IDeviceStore
    {
        public Task<IReadOnlyList<DeviceInfo>> ListAsync(CancellationToken cancellationToken)
            => throw new InvalidOperationException("devices.json");

        public Task SaveAsync(DeviceInfo device, CancellationToken cancellationToken)
            => throw new InvalidOperationException("devices.json");

        public Task DeleteAsync(DeviceId id, CancellationToken cancellationToken)
            => throw new InvalidOperationException("devices.json");
    }
}
