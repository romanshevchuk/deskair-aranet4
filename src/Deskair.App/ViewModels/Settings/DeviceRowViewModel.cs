using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Domain.Readings;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Settings;

/// <summary>One sensor in Settings → Devices: its address and an editable room name. A new name is saved when the field is left; a blank one is put back.</summary>
public sealed partial class DeviceRowViewModel : ObservableObject
{
    private readonly Func<DeviceId, string, Task<bool>> rename;
    private readonly ILogger logger;
    private string committed;
    private bool reverting;

    [ObservableProperty]
    private string name;

    public DeviceRowViewModel(DeviceId id, string name, Func<DeviceId, string, Task<bool>> rename, ILogger logger)
    {
        Id = id;
        this.name = name;
        committed = name;
        this.rename = rename;
        this.logger = logger;
    }

    public DeviceId Id { get; }

    public string Address => Id.Address;

    partial void OnNameChanged(string value)
    {
        if (!reverting)
        {
            _ = SaveAsync(value);
        }
    }

    private async Task SaveAsync(string value)
    {
        try
        {
            if (await rename(Id, value))
            {
                committed = value.Trim();
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Renaming {Address} failed.", Id.Address);
            }
        }

        // A blank, unchanged or failed name goes back to what is saved.
        if (value.Trim() != committed)
        {
            reverting = true;
            Name = committed;
            reverting = false;
        }
    }
}
