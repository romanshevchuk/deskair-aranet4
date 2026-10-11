namespace Deskair.Services.Storage;

/// <summary>Where a stored reading came from (the src column). It does not decide which reading wins: the first one stored for a slot stays.</summary>
public enum ReadingSource
{
    Device,
    Import
}
