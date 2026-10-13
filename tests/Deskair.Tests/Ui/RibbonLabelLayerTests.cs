using System.Windows;
using Deskair.App.Ui.Charts;
using Deskair.App.Ui.Charts.Layers;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Ui;

/// <summary>The caption, the white notches and the hours around the Live 24 h ribbon.</summary>
public class RibbonLabelLayerTests
{
    // Midnight 12 October to midnight 13 October (UTC): 250 px across the plot, 10.4 px an hour, plot from y 10 to y 170 with the labels under it.
    private static readonly DateTimeOffset midnight = Samples.Utc(2026, 10, 12);

    private static ChartArea Day() => LayerRenderer.Area(midnight.UtcTicks, midnight.AddDays(1).UtcTicks);

    private static int X(ChartArea area, DateTimeOffset time) => (int)Math.Round(area.X(time));

    [Fact]
    public void A_notch_and_the_hour_mark_six_noon_and_six_in_the_evening()
    {
        UiHost.Run(() =>
        {
            var area = Day();
            var bitmap = LayerRenderer.Render(new RibbonLabelLayer { TimeZone = TimeZoneInfo.Utc }, area);

            foreach (var hour in new[] { 6, 12, 18 })
            {
                var x = X(area, midnight.AddHours(hour));

                Assert.Contains(Enumerable.Range(x - 1, 3), column => LayerRenderer.Pixel(bitmap, column, 90).A > 200);
                Assert.True(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(x - 10, (int)area.Plot.Bottom + 4, 20, 14)), $"the label {hour:00}h");
            }

            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(X(area, midnight.AddHours(8)) - 2, 10, 4, 160)), "nothing between the marks");
        });
    }

    [Fact]
    public void Midnight_at_either_end_of_the_bar_has_no_notch()
    {
        UiHost.Run(() =>
        {
            var area = Day();
            var bitmap = LayerRenderer.Render(new RibbonLabelLayer { TimeZone = TimeZoneInfo.Utc }, area);

            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect((int)area.Plot.Left - 1, 10, 3, 160)));
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect((int)area.Plot.Right - 1, 10, 3, 160)));
        });
    }

    [Fact]
    public void The_hours_are_local_time()
    {
        UiHost.Run(() =>
        {
            // Kyiv is UTC+3 on 12 October: 06:00 there is 03:00 UTC, which is where the first notch is.
            var area = Day();
            var bitmap = LayerRenderer.Render(new RibbonLabelLayer { TimeZone = Samples.Kyiv }, area);

            Assert.Contains(Enumerable.Range(X(area, midnight.AddHours(3)) - 1, 3), column => LayerRenderer.Pixel(bitmap, column, 90).A > 200);
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(X(area, midnight.AddHours(6)) - 2, 10, 4, 160)), "no notch at 06:00 UTC");
        });
    }

    [Fact]
    public void The_caption_sits_left_of_the_bar_and_only_when_it_is_given()
    {
        UiHost.Run(() =>
        {
            var area = Day();
            var with = LayerRenderer.Render(new RibbonLabelLayer { Caption = "24 h", TimeZone = TimeZoneInfo.Utc }, area);
            var without = LayerRenderer.Render(new RibbonLabelLayer { TimeZone = TimeZoneInfo.Utc }, area);
            var left = new Int32Rect(0, 10, (int)area.Plot.Left - 4, 160);

            Assert.True(LayerRenderer.AnyDrawn(with, left));
            Assert.False(LayerRenderer.AnyDrawn(without, left));
        });
    }
}
