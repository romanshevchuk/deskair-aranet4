namespace Deskair.Services.Platform;

/// <summary>Whether the app runs from an installed package (the Store build) or as a plain program. The few things that differ between the two ask here.</summary>
public interface IPackageIdentity
{
    bool IsPackaged { get; }
}
