namespace Deskair.Services.Import;

/// <summary>Structured preview ("64,000 readings, 12 Jul to 9 Oct · 62,500 new · 1,500 already present").</summary>
public sealed record ImportPreview(
    int TotalReadings,
    int NewReadings,
    int AlreadyPresent,
    DateTimeOffset? First,
    DateTimeOffset? Last);
