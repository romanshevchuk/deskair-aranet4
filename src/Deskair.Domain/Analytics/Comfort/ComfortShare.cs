namespace Deskair.Domain.Analytics.Comfort;

/// <summary>The share of readings below, inside and above the comfortable range, each from 0 to 1; together they make 1.</summary>
public sealed record ComfortShare(double Low, double Inside, double High);
