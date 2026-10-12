using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.Domain.Metrics;
using Deskair.Domain.Zones;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Platform;
using Deskair.Services.Settings;

namespace Deskair.App.ViewModels.Shell;

/// <summary>The room chip in the header and its menu: the known rooms with their latest value and zone, and the choice of room.</summary>
public sealed partial class RoomChipViewModel : ObservableObject
{
    private readonly DeviceRegistry devices;
    private readonly SettingsService settings;
    private readonly IMessenger messenger;
    private readonly IUiDispatcher dispatcher;
    private readonly MetricFormatter formatter;

    [ObservableProperty]
    private IReadOnlyList<RoomItem> rooms = [];

    [ObservableProperty]
    private RoomItem? selectedRoom;

    [ObservableProperty]
    private bool isOpen;

    public RoomChipViewModel(
        DeviceRegistry devices,
        SettingsService settings,
        IMessenger messenger,
        IUiDispatcher dispatcher,
        MetricFormatter formatter)
    {
        this.devices = devices;
        this.settings = settings;
        this.messenger = messenger;
        this.dispatcher = dispatcher;
        this.formatter = formatter;
    }

    /// <summary>Builds the room list from the known devices. Call it again after a device is added or renamed.</summary>
    public void Load()
    {
        var known = devices.Known;

        Rooms = known.Select(device => new RoomItem(device.Id, device.RoomName, null, null)).ToList();
        SelectedRoom = devices.Selected is { } selected ? Rooms.FirstOrDefault(room => room.Device == selected) : null;
    }

    /// <summary>Starts following new readings so each room shows its latest value and zone. Stops when cancelled.</summary>
    public void Start(CancellationToken cancellationToken)
    {
        messenger.UnregisterAll(this);
        messenger.Register<RoomChipViewModel, ReadingReceived>(this, static (self, message) => self.OnReading(message));
        cancellationToken.Register(() => messenger.UnregisterAll(this));
    }

    [RelayCommand]
    private async Task ChooseRoomAsync(RoomItem room)
    {
        IsOpen = false;

        if (room.Device == devices.Selected)
        {
            return;
        }

        devices.Select(room.Device);
        SelectedRoom = Rooms.FirstOrDefault(candidate => candidate.Device == room.Device);

        await settings.UpdateAsync(current => current with { SelectedDevice = room.Device }, CancellationToken.None);
        messenger.Send(new DeviceSelected(room.Device));
    }

    private void OnReading(ReadingReceived message)
    {
        dispatcher.Post(() =>
        {
            var value = $"{formatter.Value(Metric.Co2, message.Reading.Co2Ppm)} {formatter.Unit(Metric.Co2)}";
            var zone = ZoneClassifier.Classify(message.Reading.Co2Ppm);

            Rooms = Rooms.Select(room => room.Device == message.Device ? room with { ValueText = value, Zone = zone } : room).ToList();
            SelectedRoom = Rooms.FirstOrDefault(room => room.Device == devices.Selected);
        });
    }
}
