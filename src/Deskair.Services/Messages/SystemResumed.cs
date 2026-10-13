namespace Deskair.Services.Messages;

/// <summary>Windows woke up from sleep. The sensor connection is gone by then, so the app connects again and syncs.</summary>
public sealed record SystemResumed;
