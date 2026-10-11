namespace Deskair.Domain.Analytics.TypicalDay;

/// <summary>Average of all readings in one local hour of the day (0 to 23). Average is null when there are none.</summary>
public sealed record HourOfDayAverage(int Hour, double? Average, int ReadingCount);
