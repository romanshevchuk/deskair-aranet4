namespace Deskair.Services.Connection;

public enum ConnectionState
{
    Normal,
    Stale,
    NotConnected,
    LowBattery,
    FirstRun,

    /// <summary>The app is still trying to reach the sensor after a start, a wake from sleep or a new sensor: shown calmly, with no error and no retry button.</summary>
    Connecting
}
