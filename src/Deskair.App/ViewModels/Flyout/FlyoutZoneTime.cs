using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.Flyout;

/// <summary>One entry of the ribbon legend: the time spent in a zone over the last 24 hours, as text ("2 h 40 min").</summary>
public sealed record FlyoutZoneTime(Zone Zone, string Text);
