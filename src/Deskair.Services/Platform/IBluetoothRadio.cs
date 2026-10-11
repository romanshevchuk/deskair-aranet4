namespace Deskair.Services.Platform;

public interface IBluetoothRadio
{
    bool IsOn { get; }

    event EventHandler? StateChanged;
}
