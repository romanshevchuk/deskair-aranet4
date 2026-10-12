using Deskair.Domain.Zones;

namespace Deskair.Domain.Analytics.Ribbon;

/// <summary>A stretch of time in one zone, one slot of the 24 h ribbon.</summary>
public sealed record ZoneSpan(DateTimeOffset Start, DateTimeOffset End, Zone Zone);
