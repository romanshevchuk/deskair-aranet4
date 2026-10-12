using System.IO;
using Deskair.Domain.Readings;
using Deskair.Services.Devices;
using Deskair.Storage.Devices;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Storage;

public sealed class DeviceFileStoreTests : IDisposable
{
    private static readonly DeviceId office = new("aranet4", "AA:01");
    private static readonly DeviceId bedroom = new("aranet4", "AA:02");

    private readonly TempKnownFolders folders = new();
    private readonly DeviceFileStore store;

    public DeviceFileStoreTests()
    {
        store = new DeviceFileStore(folders);
    }

    // Folder name is "<providerId>-<address>" with characters that are not allowed in file names replaced (questions.md Q9).
    private string DeviceFolder(string name) => Path.Combine(folders.DataFolder, "devices", name);

    private static DeviceInfo Device(DeviceId id, string room) => new(
        id,
        room,
        "bedroom",
        [new IntervalChange(Samples.Utc(2026, 1, 1), TimeSpan.FromMinutes(2)), new IntervalChange(Samples.Utc(2026, 6, 1), TimeSpan.FromMinutes(5))],
        Samples.Utc(2026, 10, 9, 12));

    public void Dispose()
    {
        folders.Dispose();
    }

    [Fact]
    public async Task List_returns_nothing_when_there_is_no_devices_folder()
    {
        Assert.Empty(await store.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task List_returns_every_saved_device_with_all_its_fields()
    {
        await store.SaveAsync(Device(office, "Office"), CancellationToken.None);
        await store.SaveAsync(Device(bedroom, "Bedroom"), CancellationToken.None);

        var devices = await store.ListAsync(CancellationToken.None);

        Assert.Equal(2, devices.Count);
        var loaded = devices.Single(device => device.Id == office);
        Assert.Equal("Office", loaded.RoomName);
        Assert.Equal("bedroom", loaded.RoomType);
        Assert.Equal(Samples.Utc(2026, 10, 9, 12), loaded.LastSyncUtc);
        Assert.Equal(
            new[] { TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(5) },
            loaded.IntervalHistory.Select(change => change.Interval));
        Assert.Equal(Samples.Utc(2026, 6, 1), loaded.IntervalHistory[1].From);
    }

    [Fact]
    public async Task List_keeps_a_device_that_has_never_synced_and_has_no_room_type()
    {
        await store.SaveAsync(new DeviceInfo(office, "Office", null, [], null), CancellationToken.None);

        var loaded = Assert.Single(await store.ListAsync(CancellationToken.None));

        Assert.Null(loaded.RoomType);
        Assert.Null(loaded.LastSyncUtc);
        Assert.Empty(loaded.IntervalHistory);
    }

    [Fact]
    public async Task List_skips_a_folder_whose_device_file_cannot_be_read_or_is_missing()
    {
        await store.SaveAsync(Device(office, "Office"), CancellationToken.None);
        Directory.CreateDirectory(DeviceFolder("aranet4-BROKEN"));
        await File.WriteAllTextAsync(Path.Combine(DeviceFolder("aranet4-BROKEN"), "device.json"), "{ not json", TestContext.Current.CancellationToken);
        Directory.CreateDirectory(DeviceFolder("aranet4-READINGS-ONLY"));
        await File.WriteAllTextAsync(Path.Combine(DeviceFolder("aranet4-READINGS-ONLY"), "readings-2026-10.csv"), "x", TestContext.Current.CancellationToken);

        var devices = await store.ListAsync(CancellationToken.None);

        Assert.Equal(office, Assert.Single(devices).Id);
    }

    [Fact]
    public async Task Save_creates_the_device_folder_and_leaves_no_temporary_file()
    {
        await store.SaveAsync(Device(office, "Office"), CancellationToken.None);

        Assert.Equal(
            new[] { Path.Combine(DeviceFolder("aranet4-AA_01"), "device.json") },
            Directory.GetFiles(DeviceFolder("aranet4-AA_01")));
    }

    [Fact]
    public async Task Save_replaces_the_previous_file()
    {
        await store.SaveAsync(Device(office, "Office"), CancellationToken.None);
        await store.SaveAsync(Device(office, "Study"), CancellationToken.None);

        var loaded = Assert.Single(await store.ListAsync(CancellationToken.None));
        Assert.Equal("Study", loaded.RoomName);
    }

    [Fact]
    public async Task Save_that_fails_leaves_the_previous_file_untouched()
    {
        await store.SaveAsync(Device(office, "Office"), CancellationToken.None);
        var path = Path.Combine(DeviceFolder("aranet4-AA_01"), "device.json");
        var before = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);

        // A directory where the temporary file should go makes writing it fail.
        Directory.CreateDirectory(path + ".tmp");

        await Assert.ThrowsAnyAsync<Exception>(() => store.SaveAsync(Device(office, "Study"), CancellationToken.None));

        Assert.Equal(before, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Delete_removes_only_the_device_file_and_never_the_readings()
    {
        await store.SaveAsync(Device(office, "Office"), CancellationToken.None);
        var readings = Path.Combine(DeviceFolder("aranet4-AA_01"), "readings-2026-10.csv");
        await File.WriteAllTextAsync(readings, "history", TestContext.Current.CancellationToken);

        await store.DeleteAsync(office, CancellationToken.None);

        Assert.Empty(await store.ListAsync(CancellationToken.None));
        Assert.True(File.Exists(readings));
        Assert.False(File.Exists(Path.Combine(DeviceFolder("aranet4-AA_01"), "device.json")));
    }

    [Fact]
    public async Task Delete_of_an_unknown_device_does_nothing()
    {
        await store.DeleteAsync(office, CancellationToken.None);

        Assert.Empty(await store.ListAsync(CancellationToken.None));
    }
}
