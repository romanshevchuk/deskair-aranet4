namespace Deskair.Services.Messages;

/// <summary>Saved readings could not be read (<see cref="Unreadable"/> is true), or can be read again. Sent only when this changes.</summary>
public sealed record HistoryReadabilityChanged(bool Unreadable);
