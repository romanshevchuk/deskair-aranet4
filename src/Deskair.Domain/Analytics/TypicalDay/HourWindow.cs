namespace Deskair.Domain.Analytics.TypicalDay;

/// <summary>Three hours in a row of the typical day, from <see cref="StartHour"/> (0 to 23, may wrap past midnight), and their mean.</summary>
public sealed record HourWindow(int StartHour, double Average);
