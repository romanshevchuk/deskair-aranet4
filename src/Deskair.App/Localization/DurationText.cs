namespace Deskair.App.Localization;

public static class DurationText
{
    /// <summary>A length of time in whole minutes as "38 min", "1 h" or "1 h 7 min": the minutes are not padded with a zero.</summary>
    public static string Duration(this ILocalizer localizer, int minutes)
    {
        var hours = minutes / 60;
        var rest = minutes % 60;

        if (hours == 0)
        {
            return localizer.Format("Duration_Minutes", ("min", rest));
        }

        return rest == 0
            ? localizer.Format("Duration_Hours", ("h", hours))
            : localizer.Format("Duration_HoursMinutes", ("h", hours), ("min", rest));
    }
}
