using Deskair.Domain.Readings;
using Deskair.Services.Devices;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Services;

public class DeviceRegistryTests
{
    private static readonly DeviceId first = new("aranet4", "AA:01");
    private static readonly DeviceId second = new("aranet4", "AA:02");

    private static DeviceInfo Device(DeviceId id, string room)
    {
        return new DeviceInfo(id, room, null, [], null);
    }

    [Fact]
    public async Task Load_returns_the_known_devices_and_selects_the_first()
    {
        var store = new FakeDeviceStore();
        store.Devices.AddRange([Device(first, "Office"), Device(second, "Bedroom")]);
        var registry = new DeviceRegistry(store);

        var devices = await registry.LoadAsync(CancellationToken.None);

        Assert.Equal(2, devices.Count);
        Assert.Equal(first, registry.Selected);
    }

    [Fact]
    public async Task Load_with_no_devices_returns_an_empty_list_and_selects_nothing()
    {
        var registry = new DeviceRegistry(new FakeDeviceStore());

        var devices = await registry.LoadAsync(CancellationToken.None);

        Assert.Empty(devices);
        Assert.Null(registry.Selected);
    }

    [Fact]
    public async Task Load_reads_the_store_once_per_call_and_keeps_an_existing_selection()
    {
        var store = new FakeDeviceStore();
        store.Devices.AddRange([Device(first, "Office"), Device(second, "Bedroom")]);
        var registry = new DeviceRegistry(store);

        await registry.LoadAsync(CancellationToken.None);
        registry.Select(second);
        await registry.LoadAsync(CancellationToken.None);

        Assert.Equal(2, store.ListCalls);
        Assert.Equal(second, registry.Selected);
    }

    [Fact]
    public async Task Load_moves_the_selection_when_the_selected_device_was_removed()
    {
        var store = new FakeDeviceStore();
        store.Devices.AddRange([Device(first, "Office"), Device(second, "Bedroom")]);
        var registry = new DeviceRegistry(store);
        await registry.LoadAsync(CancellationToken.None);
        registry.Select(second);

        store.Devices.RemoveAll(device => device.Id == second);
        await registry.LoadAsync(CancellationToken.None);

        Assert.Equal(first, registry.Selected);
    }

    [Fact]
    public async Task IntervalFor_uses_the_interval_in_effect_at_that_time()
    {
        var store = new FakeDeviceStore();
        store.Devices.Add(new DeviceInfo(
            first,
            "Office",
            null,
            [
                new IntervalChange(Samples.Utc(2026, 10, 1), TimeSpan.FromMinutes(5)),
                new IntervalChange(Samples.Utc(2026, 1, 1), TimeSpan.FromMinutes(2))
            ],
            null));
        var registry = new DeviceRegistry(store);
        await registry.LoadAsync(CancellationToken.None);

        Assert.Equal(TimeSpan.FromMinutes(2), registry.IntervalFor(first, Samples.Utc(2026, 6, 1)));
        Assert.Equal(TimeSpan.FromMinutes(5), registry.IntervalFor(first, Samples.Utc(2026, 10, 1)));
        Assert.Equal(TimeSpan.FromMinutes(2), registry.IntervalFor(first, Samples.Utc(2025, 6, 1)));
    }

    [Fact]
    public async Task IntervalFor_falls_back_to_two_minutes_for_an_unknown_device_or_no_history()
    {
        var store = new FakeDeviceStore();
        store.Devices.Add(Device(first, "Office"));
        var registry = new DeviceRegistry(store);
        await registry.LoadAsync(CancellationToken.None);

        Assert.Equal(TimeSpan.FromMinutes(2), registry.IntervalFor(first, Samples.Utc(2026, 6, 1)));
        Assert.Equal(TimeSpan.FromMinutes(2), registry.IntervalFor(second, Samples.Utc(2026, 6, 1)));
    }

    [Fact]
    public async Task Select_changes_the_selected_device()
    {
        var store = new FakeDeviceStore();
        store.Devices.AddRange([Device(first, "Office"), Device(second, "Bedroom")]);
        var registry = new DeviceRegistry(store);
        await registry.LoadAsync(CancellationToken.None);

        registry.Select(second);

        Assert.Equal(second, registry.Selected);
    }

    [Fact]
    public async Task Select_of_the_selected_device_does_nothing()
    {
        var store = new FakeDeviceStore();
        store.Devices.Add(Device(first, "Office"));
        var registry = new DeviceRegistry(store);
        await registry.LoadAsync(CancellationToken.None);

        registry.Select(first);

        Assert.Equal(first, registry.Selected);
    }

    [Fact]
    public async Task Select_rejects_an_unknown_device()
    {
        var store = new FakeDeviceStore();
        store.Devices.Add(Device(first, "Office"));
        var registry = new DeviceRegistry(store);
        await registry.LoadAsync(CancellationToken.None);

        Assert.Throws<KeyNotFoundException>(() => registry.Select(second));
        Assert.Equal(first, registry.Selected);
    }
}
