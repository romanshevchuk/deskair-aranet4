namespace Deskair.Domain.Zones;

/// <summary>The share of readings in each CO₂ zone, each from 0 to 1; together they make 1.</summary>
public sealed record ZoneShare(double Good, double Elevated, double High);
