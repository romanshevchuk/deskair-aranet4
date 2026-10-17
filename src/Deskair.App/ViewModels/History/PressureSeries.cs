using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Pressure;

namespace Deskair.App.ViewModels.History;

/// <summary>The Pressure line of Week, Month or Year: its points in the unit shown, the biggest 24 hour change in it, and the spacing of the points (an hour, or a day for Year) that tells a gap from a step.</summary>
public sealed record PressureSeries(IReadOnlyList<ChartPoint> Points, PressureChange? Change, TimeSpan Interval);
