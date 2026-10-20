using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Data;
using Deskair.App.ViewModels.Settings;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Import;
using Deskair.Services.Messages;
using Deskair.Storage.Export;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.ViewModels;

public sealed class DataSettingsViewModelTests : IDisposable
{
    private readonly TempKnownFolders folders = new();
    private readonly FakeFolderOpener opener = new();
    private readonly FakeFileDialogs dialogs = new();
    private readonly FakeDataFolderChoice choice = new();
    private readonly ListLogger<DataSettingsViewModel> logger = new();
    private readonly StrongReferenceMessenger messenger = new();
    private readonly DataSettingsViewModel viewModel;

    public DataSettingsViewModelTests()
    {
        viewModel = Create(folders, opener, dialogs, choice, logger, messenger);
    }

    public void Dispose()
    {
        folders.Dispose();
    }

    /// <summary>Builds the view model with fakes that do nothing, for tests of other screens that only need it to exist.</summary>
    public static DataSettingsViewModel Create(
        TempKnownFolders folders,
        FakeFolderOpener? opener = null,
        FakeFileDialogs? dialogs = null,
        FakeDataFolderChoice? choice = null,
        ListLogger<DataSettingsViewModel>? logger = null,
        IMessenger? messenger = null)
    {
        var readings = new FakeReadingStore();
        var registry = new DeviceRegistry(new FakeDeviceStore());
        dialogs ??= new FakeFileDialogs();
        var localizer = new FakeLocalizer();
        var calendar = new LocalCalendar(TimeZoneInfo.Utc);

        return new DataSettingsViewModel(
            folders,
            opener ?? new FakeFolderOpener(),
            choice ?? new FakeDataFolderChoice(),
            dialogs,
            localizer,
            new ImportViewModel(
                new ImportService(readings, TimeZoneInfo.Utc),
                new ReadingImporters([]),
                registry,
                dialogs,
                localizer,
                calendar,
                new StrongReferenceMessenger(),
                NullLogger<ImportViewModel>.Instance),
            new ExportViewModel(
                new ExportService(readings, new CsvExporter(), TimeZoneInfo.Utc),
                registry,
                dialogs,
                localizer,
                calendar,
                TimeProvider.System,
                NullLogger<ExportViewModel>.Instance),
            messenger ?? new StrongReferenceMessenger(),
            logger ?? new ListLogger<DataSettingsViewModel>());
    }

    [Fact]
    public void Sync_history_asks_for_a_sync()
    {
        var asked = 0;
        messenger.Register<SyncRequested>(this, (_, _) => asked++);

        viewModel.SyncHistoryCommand.Execute(null);

        Assert.Equal(1, asked);
    }

    [Fact]
    public async Task Load_shows_the_data_folder_and_no_note()
    {
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(folders.DataFolder, viewModel.DataFolder);
        Assert.False(viewModel.HasFolderNote);
    }

    [Fact]
    public void Open_folder_shows_the_data_folder_in_the_file_browser()
    {
        viewModel.OpenFolderCommand.Execute(null);

        Assert.Equal([folders.DataFolder], opener.Opened);
    }

    [Fact]
    public void Import_and_export_are_available_to_the_view()
    {
        Assert.NotNull(viewModel.Import);
        Assert.NotNull(viewModel.Export);
    }

    [Fact]
    public async Task Changing_the_folder_remembers_it_and_says_it_is_used_after_a_restart()
    {
        dialogs.FolderToPick = @"D:\Air data";
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        await viewModel.ChangeFolderCommand.ExecuteAsync(null);

        Assert.Equal(@"D:\Air data", choice.Chosen);
        Assert.Equal(@"Data_Folder_Restart(folder=D:\Air data)", viewModel.FolderNote);
        Assert.Equal(folders.DataFolder, viewModel.DataFolder);
    }

    [Fact]
    public async Task A_cancelled_folder_dialog_changes_nothing()
    {
        dialogs.FolderToPick = null;
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        await viewModel.ChangeFolderCommand.ExecuteAsync(null);

        Assert.Null(choice.Chosen);
        Assert.False(viewModel.HasFolderNote);
    }

    [Fact]
    public async Task Picking_the_folder_in_use_needs_no_restart()
    {
        dialogs.FolderToPick = folders.DataFolder + Path.DirectorySeparatorChar;
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        await viewModel.ChangeFolderCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasFolderNote);
    }

    [Fact]
    public async Task A_folder_chosen_earlier_shows_the_note_when_the_screen_loads()
    {
        choice.Chosen = @"D:\Air data";

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(@"Data_Folder_Restart(folder=D:\Air data)", viewModel.FolderNote);
    }

    [Fact]
    public async Task A_folder_that_cannot_be_used_is_reported_and_logged()
    {
        dialogs.FolderToPick = @"D:\Air data";
        choice.Failure = new UnauthorizedAccessException("denied");
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        await viewModel.ChangeFolderCommand.ExecuteAsync(null);

        Assert.Equal("Data_Folder_Failed", viewModel.FolderNote);
        Assert.Null(choice.Chosen);
        Assert.Contains(logger.Entries, entry => entry.Exception is UnauthorizedAccessException);
    }
}
