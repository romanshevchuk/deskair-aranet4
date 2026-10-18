using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Settings;

/// <summary>Settings → Devices: the known sensors with a room name each, and Add, which opens the sensor search.</summary>
public sealed partial class DevicesSettingsViewModel : ObservableObject
{
    private readonly DeviceRegistry devices;
    private readonly DeviceEnrollment enrollment;
    private readonly IMessenger messenger;
    private readonly ILogger<DevicesSettingsViewModel> logger;
    private bool listening;

    [ObservableProperty]
    private IReadOnlyList<DeviceInfo> items = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDevices))]
    private IReadOnlyList<DeviceRowViewModel> rows = [];

    public DevicesSettingsViewModel(DeviceRegistry devices, DeviceEnrollment enrollment, IMessenger messenger, ILogger<DevicesSettingsViewModel> logger)
    {
        this.devices = devices;
        this.enrollment = enrollment;
        this.messenger = messenger;
        this.logger = logger;
    }

    public bool HasDevices => Rows.Count > 0;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (!listening)
        {
            // The first load also starts following new rooms, so a sensor paired from the search shows up in the list. Stops when cancelled.
            listening = true;
            messenger.Register<DevicesSettingsViewModel, DeviceSelected>(this, static (self, _) => self.Reload());
            cancellationToken.Register(() =>
            {
                messenger.UnregisterAll(this);
                listening = false;
            });
        }

        Items = await devices.LoadAsync(cancellationToken);
        Rows = [.. Items.Select(RowFor)];
    }

    // A row that is already shown stays, so a room switch does not throw away a name being typed.
    private DeviceRowViewModel RowFor(DeviceInfo item)
    {
        return Rows.FirstOrDefault(row => row.Id == item.Id)
            ?? new DeviceRowViewModel(item.Id, item.RoomName, (id, name) => enrollment.RenameAsync(id, name, CancellationToken.None), logger);
    }

    [RelayCommand]
    private void AddSensor()
    {
        messenger.Send(new AddSensorRequested());
    }

    private void Reload()
    {
        _ = ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try
        {
            await LoadAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Reloading the device list failed.");
            }
        }
    }
}
