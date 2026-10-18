using System.Globalization;

namespace Deskair.App.ViewModels.Flyout;

/// <summary>
/// Where the labels under the 24 h ribbon go (screens-and-states.md "Flyout"): at 00:00, 06:00, 12:00 and 18:00 local time, at their true positions, leaving out any within
/// 2 hours of the left end and 4 hours of the right end (the "Now" label needs the room), and "Now" at the right end. The ribbon ends now, so the round hours are wherever they fall in the last 24 hours.
/// </summary>
public static class FlyoutRibbonLabels
{
    private static readonly TimeSpan window = TimeSpan.FromHours(24);
    private static readonly TimeSpan startMargin = TimeSpan.FromHours(2);
    private static readonly TimeSpan endMargin = TimeSpan.FromHours(4);
    private static readonly int[] hours = [0, 6, 12, 18];

    public static IReadOnlyList<FlyoutRibbonLabel> Build(DateTimeOffset now, TimeZoneInfo zone, CultureInfo culture, string nowText)
    {
        var start = now - window;
        var localStart = TimeZoneInfo.ConvertTime(start, zone).Date;
        var labels = new List<FlyoutRibbonLabel>();

        for (var day = localStart; day <= localStart.AddDays(2); day = day.AddDays(1))
        {
            foreach (var hour in hours)
            {
                var local = day.AddHours(hour);
                var time = new DateTimeOffset(local, zone.GetUtcOffset(local));

                if (time - start >= startMargin && now - time >= endMargin)
                {
                    labels.Add(new FlyoutRibbonLabel(local.ToString("HH:mm", culture), (time - start) / window));
                }
            }
        }

        labels.Add(new FlyoutRibbonLabel(nowText, 1));

        return labels;
    }
}
