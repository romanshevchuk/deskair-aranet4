namespace Deskair.Services.Sensors;

public sealed record BleAdvertisement(
    string Address,
    string? LocalName,
    int SignalDbm,
    IReadOnlyDictionary<ushort, byte[]> ManufacturerData);
