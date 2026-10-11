namespace Deskair.Services.Connection;

public sealed record ConnectionInputs(
    bool HasSensor,
    DateTimeOffset? LastReadingAt,
    DateTimeOffset Now,
    TimeSpan Interval,
    bool RadioOn,
    int? BatteryPercent);
