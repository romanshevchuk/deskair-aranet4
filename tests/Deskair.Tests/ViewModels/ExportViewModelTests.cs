using System.IO;
using Deskair.App.ViewModels.Data;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Import;
using Deskair.Storage.Export;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public sealed class ExportViewModelTests : IDisposable
{
    private static readonly DeviceId aranet = new("aranet4", "AA:01");
    private static readonly DateTimeOffset noon = Samples.Utc(2026, 10, 9, 12);

    private readonly TempKnownFolders folders = new();
    private readonly FakeReadingStore store = new();
    private readonly FakeDeviceStore deviceStore = new();
    private readonly FakeFileDialogs dialogs = new();
    private readonly ListLogger<ExportViewModel> logger = new();
    private readonly DeviceRegistry registry;
    private readonly string target;

    public ExportViewModelTests()
    {
        registry = new DeviceRegistry(deviceStore);
        Directory.CreateDirectory(folders.DataFolder);
        target = Path.Combine(folders.DataFolder, "out.csv");
        dialogs.PathToSave = target;
    }

    public void Dispose()
    {
        folders.Dispose();
    }

    private async Task<ExportViewModel> CreateAsync(string roomName = "Office", bool withDevice = true)
    {
        if (withDevice)
        {
            deviceStore.Devices.Add(new DeviceInfo(aranet, roomName, null, [], null));
        }

        await registry.LoadAsync(TestContext.Current.CancellationToken);

        return new ExportViewModel(
            new ExportService(store, new CsvExporter(), TimeZoneInfo.Utc),
            registry,
            dialogs,
            new FakeLocalizer(),
            new LocalCalendar(TimeZoneInfo.Utc),
            new FakeClock(noon),
            logger);
    }

    [Fact]
    public async Task Exporting_writes_every_reading_of_the_selected_device_to_the_chosen_file()
    {
        store.Readings.AddRange([Samples.Co2(noon, 800), Samples.Co2(noon.AddMinutes(2), 900)]);
        var viewModel = await CreateAsync();

        await viewModel.ExportCommand.ExecuteAsync(null);

        var lines = await File.ReadAllLinesAsync(target, TestContext.Current.CancellationToken);
        Assert.Equal(3, lines.Length);
        Assert.Equal((aranet, DateTimeOffset.MinValue, DateTimeOffset.MaxValue), store.Requested);
        Assert.Equal("Export_Done(file=out.csv)", viewModel.StatusText);
    }

    [Fact]
    public async Task The_suggested_name_has_the_room_and_the_date()
    {
        var viewModel = await CreateAsync("Living room");

        await viewModel.ExportCommand.ExecuteAsync(null);

        Assert.Equal("Deskair Living room 2026-10-09.csv", dialogs.SuggestedName);
        Assert.Equal([".csv"], dialogs.LastFilter?.Extensions);
    }

    [Fact]
    public async Task Characters_a_file_name_cannot_hold_are_left_out_of_the_suggested_name()
    {
        var viewModel = await CreateAsync("Kids: room/2");

        await viewModel.ExportCommand.ExecuteAsync(null);

        Assert.Equal("Deskair Kids room2 2026-10-09.csv", dialogs.SuggestedName);
    }

    [Fact]
    public async Task A_cancelled_dialog_writes_nothing_and_says_nothing()
    {
        dialogs.PathToSave = null;
        var viewModel = await CreateAsync();

        await viewModel.ExportCommand.ExecuteAsync(null);

        Assert.False(File.Exists(target));
        Assert.False(viewModel.HasStatus);
        Assert.Null(store.Requested);
    }

    [Fact]
    public async Task Without_a_sensor_it_asks_for_one_and_opens_no_dialog()
    {
        var viewModel = await CreateAsync(withDevice: false);

        await viewModel.ExportCommand.ExecuteAsync(null);

        Assert.Equal("Data_NoSensor", viewModel.StatusText);
        Assert.Null(dialogs.SuggestedName);
    }

    [Fact]
    public async Task A_file_that_cannot_be_written_reports_a_failure_and_logs_it()
    {
        dialogs.PathToSave = Path.Combine(folders.DataFolder, "missing-folder", "out.csv");
        var viewModel = await CreateAsync();

        await viewModel.ExportCommand.ExecuteAsync(null);

        Assert.Equal("Export_Failed", viewModel.StatusText);
        Assert.Contains(logger.Entries, entry => entry.Exception is DirectoryNotFoundException);
    }
}
