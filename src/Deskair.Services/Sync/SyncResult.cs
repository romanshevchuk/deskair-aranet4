namespace Deskair.Services.Sync;

/// <summary>Complete is false when the download was cancelled or stopped early; the saved sync time must then stay unchanged.</summary>
public sealed record SyncResult(bool Complete, int ReadingsAdded);
