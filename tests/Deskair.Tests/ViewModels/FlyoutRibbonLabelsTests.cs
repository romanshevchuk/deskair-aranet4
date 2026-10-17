using System.Globalization;
using Deskair.App.ViewModels.Flyout;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class FlyoutRibbonLabelsTests
{
    private static readonly CultureInfo culture = CultureInfo.GetCultureInfo("en-US");

    private static IReadOnlyList<(string Text, double Position)> Build(DateTimeOffset now, TimeZoneInfo zone)
        => [.. FlyoutRibbonLabels.Build(now, zone, culture, "Now").Select(label => (label.Text, label.Position))];

    [Fact]
    public void At_13_00_the_labels_are_18_00_00_00_and_06_00_at_their_true_places_and_now()
    {
        var labels = Build(Samples.Utc(2026, 10, 14, 13), TimeZoneInfo.Utc);

        Assert.Equal(["18:00", "00:00", "06:00", "Now"], labels.Select(label => label.Text));
        Assert.Equal(5.0 / 24, labels[0].Position, 6);
        Assert.Equal(11.0 / 24, labels[1].Position, 6);
        Assert.Equal(17.0 / 24, labels[2].Position, 6);
        Assert.Equal(1.0, labels[3].Position);
    }

    [Fact]
    public void A_round_hour_within_two_hours_of_either_end_is_left_out()
    {
        // 12:00 is 1 hour before now and 18:00 yesterday is 1 hour after the start at 17:00 yesterday.
        var labels = Build(Samples.Utc(2026, 10, 14, 13), TimeZoneInfo.Utc);
        Assert.DoesNotContain(labels, label => label.Text == "12:00");

        var evening = Build(Samples.Utc(2026, 10, 14, 17), TimeZoneInfo.Utc);
        Assert.DoesNotContain(evening, label => label.Text == "18:00");
        Assert.Equal(["00:00", "06:00", "12:00", "Now"], evening.Select(label => label.Text));
    }

    [Fact]
    public void Exactly_two_hours_from_an_end_still_counts()
    {
        var labels = Build(Samples.Utc(2026, 10, 14, 14), TimeZoneInfo.Utc);

        Assert.Equal(["18:00", "00:00", "06:00", "12:00", "Now"], labels.Select(label => label.Text));
    }

    [Fact]
    public void Labels_are_in_local_time()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("plus3", TimeSpan.FromHours(3), "plus3", "plus3");

        // 10:00 UTC is 13:00 local, so the labels are the same as at 13:00 in UTC, but sit at the same places in the window.
        var labels = Build(Samples.Utc(2026, 10, 14, 10), zone);

        Assert.Equal(["18:00", "00:00", "06:00", "Now"], labels.Select(label => label.Text));
        Assert.Equal(5.0 / 24, labels[0].Position, 6);
    }

    [Fact]
    public void Now_is_always_last_with_the_given_text()
    {
        var labels = Build(Samples.Utc(2026, 10, 14, 13), TimeZoneInfo.Utc);

        Assert.Equal(("Now", 1.0), labels[^1]);
    }
}
