using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Data;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Import;
using Deskair.Services.Messages;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public sealed class ImportViewModelTests : IDisposable
{
    private static readonly DeviceId aranet = new("aranet4", "AA:01");
    private static readonly DateTimeOffset noon = Samples.Utc(2026, 10, 9, 12);

    private readonly TempKnownFolders folders = new();
    private readonly FakeReadingStore store = new();
    private readonly FakeDeviceStore deviceStore = new();
    private readonly FakeFileDialogs dialogs = new();
    private readonly StrongReferenceMessenger messenger = new();
    private readonly ListLogger<ImportViewModel> logger = new();
    private readonly DeviceRegistry registry;
    private readonly string file;

    public ImportViewModelTests()
    {
        registry = new DeviceRegistry(deviceStore);
        Directory.CreateDirectory(folders.DataFolder);
        file = Path.Combine(folders.DataFolder, "export.csv");
        File.WriteAllText(file, "placeholder");
        dialogs.PathToOpen = file;
    }

    public void Dispose()
    {
        folders.Dispose();
    }

    private static Reading At(int minutes) => Samples.Co2(noon.AddMinutes(minutes), 800);

    private async Task<ImportViewModel> CreateAsync(FakeReadingImporter importer, bool withDevice = true, string providerId = "aranet4")
    {
        if (withDevice)
        {
            deviceStore.Devices.Add(new DeviceInfo(aranet, "Office", null, [], null));
        }

        await registry.LoadAsync(TestContext.Current.CancellationToken);

        return new ImportViewModel(
            new ImportService(store, TimeZoneInfo.Utc),
            new ReadingImporters([new FakeSensorProvider(providerId, new FakeSensor(), importer)]),
            registry,
            dialogs,
            new FakeLocalizer(),
            new LocalCalendar(TimeZoneInfo.Utc),
            messenger,
            logger);
    }

    [Fact]
    public async Task Choosing_a_file_shows_a_preview_and_writes_nothing()
    {
        store.Readings.Add(At(0));
        var viewModel = await CreateAsync(new FakeReadingImporter(At(0), At(2), At(4)));

        await viewModel.ChooseFileCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasPreview);
        Assert.StartsWith("Import_Preview[2]", viewModel.PreviewText);
        Assert.Empty(store.Merges);
    }


    [Fact]
    public async Task A_file_with_nothing_new_says_so_and_offers_no_import()
    {
        store.Readings.Add(At(0));
        var viewModel = await CreateAsync(new FakeReadingImporter(At(0)));

        await viewModel.ChooseFileCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasPreview);
        Assert.Equal("Import_NothingNew", viewModel.StatusText);
    }
    [Fact]
    public async Task The_dialog_offers_the_importers_file_types()
    {
        var viewModel = await CreateAsync(new FakeReadingImporter(At(0)));

        await viewModel.ChooseFileCommand.ExecuteAsync(null);

        Assert.Equal([".csv"], dialogs.LastFilter?.Extensions);
        Assert.Equal("Import_Fake_Name", dialogs.LastFilter?.Description);
    }

    [Fact]
    public async Task A_cancelled_dialog_does_nothing()
    {
        dialogs.PathToOpen = null;
        var viewModel = await CreateAsync(new FakeReadingImporter(At(0)));

        await viewModel.ChooseFileCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasPreview);
        Assert.False(viewModel.HasStatus);
    }

    [Fact]
    public async Task Confirming_imports_for_the_selected_device_and_tells_the_app()
    {
        var sent = new List<ReadingsImported>();
        messenger.Register<ReadingsImported>(this, (_, message) => sent.Add(message));
        var viewModel = await CreateAsync(new FakeReadingImporter(At(0), At(2)));
        await viewModel.ChooseFileCommand.ExecuteAsync(null);

        await viewModel.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal([new ReadingsImported(aranet, 2)], sent);
        Assert.Equal(2, store.Readings.Count);
        Assert.Equal("Import_Done[2]", viewModel.StatusText);
        Assert.False(viewModel.HasPreview);
    }

    [Fact]
    public async Task Cancelling_the_preview_imports_nothing()
    {
        var viewModel = await CreateAsync(new FakeReadingImporter(At(0), At(2)));
        await viewModel.ChooseFileCommand.ExecuteAsync(null);

        viewModel.CancelCommand.Execute(null);

        Assert.False(viewModel.HasPreview);
        Assert.Empty(store.Merges);
        Assert.False(viewModel.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public async Task A_file_without_readings_says_so_and_offers_no_import()
    {
        var viewModel = await CreateAsync(new FakeReadingImporter());

        await viewModel.ChooseFileCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasPreview);
        Assert.Equal("Import_Empty", viewModel.StatusText);
    }

    [Fact]
    public async Task A_file_that_cannot_be_read_reports_a_failure_and_logs_it()
    {
        var viewModel = await CreateAsync(new FakeReadingImporter { Invalid = true });

        await viewModel.ChooseFileCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasPreview);
        Assert.Equal("Import_Failed", viewModel.StatusText);
        Assert.Contains(logger.Entries, entry => entry.Exception is FormatException);
    }

    [Fact]
    public async Task A_file_that_vanishes_before_confirming_reports_a_failure()
    {
        var viewModel = await CreateAsync(new FakeReadingImporter(At(0)));
        await viewModel.ChooseFileCommand.ExecuteAsync(null);
        File.Delete(file);

        await viewModel.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal("Import_Failed", viewModel.StatusText);
        Assert.Empty(store.Merges);
    }

    [Fact]
    public async Task Without_a_sensor_it_asks_for_one_and_opens_no_dialog()
    {
        var viewModel = await CreateAsync(new FakeReadingImporter(At(0)), withDevice: false);

        await viewModel.ChooseFileCommand.ExecuteAsync(null);

        Assert.Equal("Data_NoSensor", viewModel.StatusText);
        Assert.Equal(0, dialogs.OpenRequests);
    }

    [Fact]
    public async Task A_sensor_without_an_importer_says_import_is_not_available()
    {
        var viewModel = await CreateAsync(new FakeReadingImporter(At(0)), providerId: "simulated");

        await viewModel.ChooseFileCommand.ExecuteAsync(null);

        Assert.Equal("Import_Unavailable", viewModel.StatusText);
        Assert.Equal(0, dialogs.OpenRequests);
    }

    [Fact]
    public async Task Choosing_again_clears_the_earlier_result()
    {
        var viewModel = await CreateAsync(new FakeReadingImporter(At(0)));
        await viewModel.ChooseFileCommand.ExecuteAsync(null);
        await viewModel.ConfirmCommand.ExecuteAsync(null);
        var done = viewModel.StatusText;
        Assert.NotNull(done);

        await viewModel.ChooseFileCommand.ExecuteAsync(null);

        // The same file again holds nothing new, so what shows is that, not the earlier "added" line.
        Assert.NotEqual(done, viewModel.StatusText);
        Assert.False(viewModel.HasPreview);
    }
}
