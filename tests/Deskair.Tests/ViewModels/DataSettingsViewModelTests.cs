using Deskair.App.ViewModels.Settings;
using Deskair.Services.Import;
using Deskair.Storage.Export;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public sealed class DataSettingsViewModelTests : IDisposable
{
    private readonly TempKnownFolders folders = new();

    public void Dispose()
    {
        folders.Dispose();
    }

    [Fact]
    public async Task Load_shows_the_data_folder()
    {
        var readings = new FakeReadingStore();
        var viewModel = new DataSettingsViewModel(
            new ImportService(readings, TimeZoneInfo.Utc),
            new ExportService(readings, new CsvExporter(), TimeZoneInfo.Utc),
            folders);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(folders.DataFolder, viewModel.DataFolder);
    }
}
