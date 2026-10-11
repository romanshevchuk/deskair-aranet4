namespace Deskair.Services.Storage;

/// <summary>Where a stored reading came from. Sensor values win over imports when slots collide.</summary>
public enum ReadingSource
{
    Device,
    Import
}
