namespace Deskair.Domain.Analytics.AirImproved;

/// <summary>
/// One "air improved" event (behaviour-rules.md "Air improved"): where it started, the CO₂ there, how far it fell by the time the event ended, and how long that took.
/// It is not called "window opened": the rule only sees the air get better.
/// </summary>
public sealed record AirImprovedEvent(DateTimeOffset Start, double StartPpm, double DropPpm, TimeSpan Duration);
