using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.ChartModels;

/// <summary>One of the 24 bars of the typical-day chart.</summary>
public sealed record HourBar(int Hour, double? Average, Zone? Zone, bool InWorstWindow);
