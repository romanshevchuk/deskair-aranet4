using System.IO;
using Deskair.Services.Settings;
using Deskair.Storage.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Storage;

public sealed class JsonWindowPlaceStoreTests : IDisposable
{
    private readonly TempKnownFolders folders = new();
    private readonly JsonWindowPlaceStore store;

    public JsonWindowPlaceStoreTests()
    {
        store = new JsonWindowPlaceStore(folders);
    }

    private string FilePath => Path.Combine(folders.StateFolder, "window.json");

    public void Dispose()
    {
        folders.Dispose();
    }

    [Fact]
    public async Task Load_returns_nothing_when_there_is_no_file()
    {
        Assert.Null(await store.LoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Load_reads_back_what_Save_wrote()
    {
        var saved = new WindowPlace(-1700, 40, 1300, 800, true);

        await store.SaveAsync(saved, CancellationToken.None);

        Assert.Equal(saved, await store.LoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Save_writes_to_the_state_folder_and_never_to_the_data_folder()
    {
        await store.SaveAsync(new WindowPlace(10, 20, 1200, 728, false), CancellationToken.None);

        Assert.Equal(new[] { FilePath }, Directory.GetFiles(folders.StateFolder));
        Assert.Empty(Directory.GetFiles(folders.DataFolder));
    }

    [Theory]
    [InlineData("""{ "left": 10, "top": 20, "width": 0, "height": 700, "maximized": false }""")]
    [InlineData("""{ "left": 10, "top": 20, "width": 1200, "height": -5, "maximized": false }""")]
    [InlineData("{ this is not json")]
    [InlineData("")]
    [InlineData("null")]
    public async Task Load_returns_nothing_for_a_file_it_cannot_use(string content)
    {
        Directory.CreateDirectory(folders.StateFolder);
        await File.WriteAllTextAsync(FilePath, content, TestContext.Current.CancellationToken);

        Assert.Null(await store.LoadAsync(CancellationToken.None));
    }
}
