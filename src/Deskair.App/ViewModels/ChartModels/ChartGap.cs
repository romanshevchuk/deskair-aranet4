namespace Deskair.App.ViewModels.ChartModels;

/// <summary>A run of time with no data, drawn hatched.</summary>
public sealed record ChartGap(DateTimeOffset Start, DateTimeOffset End);
