using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Readings;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Services;

public class DeviceEnrollmentTests
{
    private static readonly DeviceId sensor = new("aranet4", "C3DEC8FCC7C6");

    private readonly FakeDeviceStore store = new();
    private readonly FakeSettingsStore settingsStore = new();
    private readonly StrongReferenceMessenger messenger = new();
    private readonly DeviceRegistry registry;
    private readonly SettingsService settings;
    private readonly DeviceEnrollment enrollment;
    private readonly List<DeviceSelected> selections = [];

    public DeviceEnrollmentTests()
    {
        registry = new DeviceRegistry(store);
        settings = new SettingsService(settingsStore, messenger);
        enrollment = new DeviceEnrollment(store, registry, settings, messenger);
        messenger.Register<DeviceSelected>(this, (_, message) => selections.Add(message));
    }

    [Fact]
    public async Task A_new_sensor_is_saved_as_a_room_with_the_default_interval()
    {
        var device = await enrollment.EnrollAsync(sensor, "  Office ", TestContext.Current.CancellationToken);

        var saved = Assert.Single(store.Snapshot());
        Assert.Equal(device, saved);
        Assert.Equal(sensor, saved.Id);
        Assert.Equal("Office", saved.RoomName);
        Assert.Null(saved.RoomType);
        Assert.Equal(TimeSpan.FromMinutes(2), Assert.Single(saved.IntervalHistory).Interval);
    }

    [Fact]
    public async Task The_new_room_is_selected_remembered_and_announced()
    {
        await enrollment.EnrollAsync(sensor, "Office", TestContext.Current.CancellationToken);

        Assert.Equal(sensor, registry.Selected);
        Assert.Equal(sensor, settings.Current.SelectedDevice);
        Assert.Equal(sensor, settingsStore.Stored.SelectedDevice);
        Assert.Equal(sensor, Assert.Single(selections).Device);
    }

    [Fact]
    public async Task A_second_sensor_becomes_the_selected_one()
    {
        var other = new DeviceId("aranet4", "AABBCCDDEEFF");
        await enrollment.EnrollAsync(sensor, "Office", TestContext.Current.CancellationToken);

        await enrollment.EnrollAsync(other, "Bedroom", TestContext.Current.CancellationToken);

        Assert.Equal(["Office", "Bedroom"], store.Snapshot().Select(device => device.RoomName));
        Assert.Equal(other, registry.Selected);
        Assert.Equal([sensor, other], selections.Select(message => message.Device));
    }

    [Fact]
    public async Task A_known_sensor_keeps_its_name_and_history_and_is_only_selected()
    {
        var history = new IntervalChange(DateTimeOffset.UnixEpoch, TimeSpan.FromMinutes(5));
        var existing = new DeviceInfo(sensor, "Kitchen", "type", [history], DateTimeOffset.UnixEpoch);
        store.Devices.Add(existing);

        var device = await enrollment.EnrollAsync(sensor, "Office", TestContext.Current.CancellationToken);

        Assert.Equal(existing, device);
        Assert.Equal(existing, Assert.Single(store.Snapshot()));
        Assert.Empty(store.Saved);
        Assert.Equal(sensor, registry.Selected);
        Assert.Single(selections);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_blank_name_is_rejected_and_nothing_changes(string name)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => enrollment.EnrollAsync(sensor, name, TestContext.Current.CancellationToken));

        Assert.Empty(store.Snapshot());
        Assert.Null(registry.Selected);
        Assert.Empty(selections);
    }
}
