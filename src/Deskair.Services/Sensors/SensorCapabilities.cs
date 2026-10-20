namespace Deskair.Services.Sensors;

/// <param name="Broadcasts">The sensor announces its current readings by itself, so <see cref="ISensor.ListenAsync"/> works and no connection has to stay open.</param>
public sealed record SensorCapabilities(bool History, bool Battery, bool SignalStrength, bool Broadcasts = false);
