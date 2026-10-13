using System.Windows;
using Deskair.App.Ui.Charts;
using Deskair.App.Ui.Charts.Layers;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Zones;

namespace Deskair.Tests.Ui;

/// <summary>The bars, outline and labels of the typical-day chart, looked at pixel by pixel.</summary>
public class TypicalDayLayerTests
{
    // x runs over 24 hours across the 250 px wide plot (left 40): 10.4 px per hour. y runs from 400 to 1600 over 130 px (top 30, bottom 160), with room above the plot for the outline.
    private static ChartArea Area()
    {
        var area = ChartArea.Create(new Size(LayerRenderer.Width, LayerRenderer.Height), new Thickness(40, 30, 10, 40), 0, 24, 400, 1600);

        return area with { Resources = key => Application.Current.TryFindResource(key) };
    }

    private static List<HourBar> Day(Func<int, bool> worst) => Enumerable.Range(0, 24).Select(hour => new HourBar(hour, 1200, Zone.Elevated, worst(hour))).ToList();

    private static int Middle(ChartArea area, int hour) => (int)area.X(hour + 0.5);

    [Fact]
    public void An_ordinary_hour_is_a_soft_bar_and_a_worst_hour_is_full_colour()
    {
        UiHost.Run(() =>
        {
            var area = Area();
            var bitmap = LayerRenderer.Render(new HourBarLayer { Hours = Day(hour => hour is 10 or 11 or 12) }, area);
            var y = (int)area.Y(800);

            Assert.InRange(LayerRenderer.Pixel(bitmap, Middle(area, 4), y).A, 120, 160);
            Assert.True(LayerRenderer.Pixel(bitmap, Middle(area, 11), y).A > 250);
        });
    }

    [Fact]
    public void Bars_leave_a_gap_between_neighbours()
    {
        UiHost.Run(() =>
        {
            var area = Area();
            var bitmap = LayerRenderer.Render(new HourBarLayer { Hours = Day(_ => false) }, area);
            var y = (int)area.Y(800);

            // The edge between hour 4 and hour 5 is a 6 px gap (3 px each side).
            Assert.Equal(0, LayerRenderer.Pixel(bitmap, (int)area.X(5), y).A);
            Assert.True(LayerRenderer.Pixel(bitmap, (int)area.X(5) + 5, y).A > 50);
        });
    }

    [Fact]
    public void An_hour_without_an_average_draws_no_bar()
    {
        UiHost.Run(() =>
        {
            var area = Area();
            var hours = Day(_ => false);
            hours[7] = new HourBar(7, null, null, false);
            var bitmap = LayerRenderer.Render(new HourBarLayer { Hours = hours }, area);

            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect((int)area.X(7), 0, (int)(area.X(8) - area.X(7)), LayerRenderer.Height)));
        });
    }

    [Fact]
    public void The_worst_hours_get_a_dashed_outline_rising_above_the_plot_and_the_other_hours_do_not()
    {
        UiHost.Run(() =>
        {
            var area = Area();
            var withWorst = LayerRenderer.Render(new HourBarLayer { Hours = Day(hour => hour is 14 or 15 or 16) }, area);
            var without = LayerRenderer.Render(new HourBarLayer { Hours = Day(_ => false) }, area);
            var above = new Int32Rect((int)area.X(14), (int)area.Plot.Top - 7, (int)(area.X(17) - area.X(14)), 6);

            Assert.True(LayerRenderer.AnyDrawn(withWorst, above), "the top edge of the outline sits above the plot");
            Assert.False(LayerRenderer.AnyDrawn(without, above));
        });
    }

    [Fact]
    public void The_text_for_the_worst_hours_is_centred_above_the_outline_and_only_once()
    {
        UiHost.Run(() =>
        {
            var area = Area();
            var hours = Day(hour => hour is 22 or 23 or 0);
            var withText = LayerRenderer.Render(new HourBarLayer { Hours = hours, WorstText = "worst 22–01h" }, area);
            var without = LayerRenderer.Render(new HourBarLayer { Hours = hours }, area);
            var region = new Int32Rect(0, 0, LayerRenderer.Width, (int)area.Plot.Top - 8);

            Assert.True(LayerRenderer.AnyDrawn(withText, region), "the text above the outline");
            Assert.False(LayerRenderer.AnyDrawn(without, region), "nothing up there without the text");
        });
    }

    [Fact]
    public void The_axis_has_a_label_every_three_hours_under_the_middle_of_the_bar()
    {
        UiHost.Run(() =>
        {
            var area = Area();
            var bitmap = LayerRenderer.Render(new HourAxisLayer { YTicks = [1000, 1400] }, area);
            var top = (int)area.Plot.Bottom + 8;

            foreach (var hour in new[] { 0, 3, 6, 9, 12, 15, 18, 21 })
            {
                Assert.True(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(Middle(area, hour) - 8, top, 16, 14)), $"{hour:00}h");
            }

            // Between the labels of 00h and 03h (the plot is narrow here, so they are close) there is a gap with nothing in it.
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect((int)area.Plot.Left + 19, top, 4, 14)), "a gap between neighbouring labels");
        });
    }

    [Fact]
    public void The_axis_labels_only_the_ticks_it_is_given_at_the_left()
    {
        UiHost.Run(() =>
        {
            var area = Area();
            var bitmap = LayerRenderer.Render(new HourAxisLayer { YTicks = [1000, 1400] }, area);

            Assert.True(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(0, (int)area.Y(1000) - 8, 38, 16)));
            Assert.True(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(0, (int)area.Y(1400) - 8, 38, 16)));
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(0, (int)area.Y(600) - 8, 38, 16)));
        });
    }

    [Fact]
    public void A_dashed_grid_has_gaps_and_a_plain_one_does_not()
    {
        UiHost.Run(() =>
        {
            var area = Area();
            var dashed = LayerRenderer.Render(new GridLayer { YTicks = [1000], Dashed = true }, area);
            var plain = LayerRenderer.Render(new GridLayer { YTicks = [1000] }, area);
            var y = (int)area.Snap(area.Y(1000));

            int Drawn(System.Windows.Media.Imaging.BitmapSource bitmap) => Enumerable.Range((int)area.Plot.Left, (int)area.Plot.Width).Count(x => LayerRenderer.Pixel(bitmap, x, y).A > 100);

            Assert.True(Drawn(dashed) < Drawn(plain) * 0.92, $"gaps in the dashed line: {Drawn(dashed)} of {Drawn(plain)}");
            Assert.True(Drawn(plain) > area.Plot.Width * 0.95, "a plain line is unbroken");
        });
    }
}
