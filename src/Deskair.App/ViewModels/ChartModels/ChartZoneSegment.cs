using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.ChartModels;

/// <summary>A stretch of time spent in one zone (used by the 24 h ribbon).</summary>
public sealed record ChartZoneSegment(DateTimeOffset Start, DateTimeOffset End, Zone Zone);
