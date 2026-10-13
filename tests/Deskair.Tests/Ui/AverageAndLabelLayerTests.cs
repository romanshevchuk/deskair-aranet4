using System.Globalization;
using System.Windows;
using Deskair.App.Ui.Charts;
using Deskair.App.Ui.Charts.Layers;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Aggregation;

namespace Deskair.Tests.Ui;

/// <summary>The dashed average of the whole period and the labels under day bars.</summary>
public class AverageAndLabelLayerTests
{
    // Friday 9 October 2026 up to Monday 12 October: three one-day slots across a 250 px plot (left 40), with 60 px under the plot for the labels and the average row.
    private static readonly DateTimeOffset friday = new(2026, 10, 9, 0, 0, 0, TimeSpan.Zero);

    private static ChartArea TallArea()
    {
        var area = ChartArea.Create(new Size(LayerRenderer.Width, LayerRenderer.Height), new Thickness(40, 10, 10, 60), friday.UtcTicks, friday.AddDays(3).UtcTicks, 400, 2000);

        return area with { Resources = key => Application.Current.TryFindResource(key) };
    }

    private static ChartBar Day(int offset, double? average, BucketState state = BucketState.Complete)
        => new(friday.AddDays(offset), friday.AddDays(offset + 1), average - 200, average + 200, average, state);

    private static List<ChartBar> ThreeDays() => [Day(0, 1000), Day(1, 1200), Day(2, 900)];

    [Fact]
    public void The_average_of_the_period_is_a_dashed_line_across_the_whole_plot()
    {
        UiHost.Run(() =>
        {
            var area = TallArea();
            var bitmap = LayerRenderer.Render(new OverallAverageLayer { Value = 1000, Label = "avg" }, area);
            var y = (int)area.Snap(area.Y(1000));

            var drawn = Enumerable.Range((int)area.Plot.Left, (int)area.Plot.Width).Count(x => LayerRenderer.Pixel(bitmap, x, y).A > 100);

            // Dashes of 4 and gaps of 4 leave about half the line drawn, and the line reaches both ends of the plot.
            Assert.InRange(drawn, 90, 160);
            Assert.True(LayerRenderer.AnyDrawn(bitmap, new Int32Rect((int)area.Plot.Left, y - 1, 12, 3)));
        });
    }

    [Fact]
    public void The_label_sits_above_the_line_at_the_right_end()
    {
        UiHost.Run(() =>
        {
            var area = TallArea();
            var bitmap = LayerRenderer.Render(new OverallAverageLayer { Value = 1000, Label = "avg" }, area);
            var y = (int)area.Snap(area.Y(1000));

            Assert.True(LayerRenderer.AnyDrawn(bitmap, new Int32Rect((int)area.Plot.Right - 50, y - 24, 50, 18)), "text above the line, at the right");
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect((int)area.Plot.Left, y - 24, 100, 18)), "nothing above the line at the left");
        });
    }

    [Fact]
    public void Without_a_value_or_outside_the_plot_nothing_is_drawn()
    {
        UiHost.Run(() =>
        {
            var area = TallArea();

            Assert.False(LayerRenderer.AnyDrawn(LayerRenderer.Render(new OverallAverageLayer { Value = null, Label = "avg" }, area)));
            Assert.False(LayerRenderer.AnyDrawn(LayerRenderer.Render(new OverallAverageLayer { Value = 3000, Label = "avg" }, area)));
            Assert.False(LayerRenderer.AnyDrawn(LayerRenderer.Render(new OverallAverageLayer { Value = 100, Label = "avg" }, area)));
        });
    }

    [Fact]
    public void Each_day_bar_is_named_under_the_plot_in_the_middle_of_its_slot()
    {
        UiHost.Run(() =>
        {
            var area = TallArea();
            var bars = ThreeDays();
            var bitmap = LayerRenderer.Render(new BarLabelLayer { Bars = bars, Culture = CultureInfo.GetCultureInfo("en-US"), TimeZone = TimeZoneInfo.Utc }, area);
            var top = (int)area.Plot.Bottom + 8;

            foreach (var bar in bars)
            {
                var middle = (int)((area.X(bar.Start) + area.X(bar.End)) / 2);

                Assert.True(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(middle - 12, top, 24, 14)), $"a label under {bar.Start:ddd}");
            }

            // The gaps between the labels stay empty, and nothing goes inside the plot.
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect((int)area.Plot.Left, 0, (int)area.Plot.Width, (int)area.Plot.Bottom)));
        });
    }

    [Fact]
    public void The_last_bar_is_called_today_and_the_row_of_averages_appears_only_with_its_word()
    {
        UiHost.Run(() =>
        {
            var area = TallArea();
            var rowTop = (int)area.Plot.Bottom + 8 + 20;
            var without = LayerRenderer.Render(new BarLabelLayer { Bars = ThreeDays(), TodayLabel = "Today" }, area);
            var with = LayerRenderer.Render(new BarLabelLayer { Bars = ThreeDays(), TodayLabel = "Today", RowLabel = "avg" }, area);
            var row = new Int32Rect(0, rowTop, LayerRenderer.Width, 18);

            Assert.False(LayerRenderer.AnyDrawn(without, row), "no row without its word");
            Assert.True(LayerRenderer.AnyDrawn(with, row), "the row of averages");
            Assert.True(LayerRenderer.AnyDrawn(with, new Int32Rect((int)area.Plot.Left - 40, rowTop, 36, 18)), "the word at the left of the row");
        });
    }

    [Fact]
    public void A_day_with_no_data_has_a_dash_in_the_row_instead_of_an_average()
    {
        UiHost.Run(() =>
        {
            var area = TallArea();
            var bars = new List<ChartBar> { Day(0, 1000), Day(1, null, BucketState.NoData), Day(2, 900) };
            var bitmap = LayerRenderer.Render(new BarLabelLayer { Bars = bars, RowLabel = "avg" }, area);
            var rowTop = (int)area.Plot.Bottom + 8 + 20;
            var middle = (int)((area.X(bars[1].Start) + area.X(bars[1].End)) / 2);

            var dash = Enumerable.Range(middle - 8, 16).Count(x => Enumerable.Range(rowTop, 18).Any(y => LayerRenderer.Pixel(bitmap, x, y).A > 100));

            // A dash is a few pixels wide, a three-digit number about twice that.
            Assert.InRange(dash, 2, 7);
        });
    }

    [Fact]
    public void Without_bars_nothing_is_drawn()
    {
        UiHost.Run(() =>
        {
            var area = TallArea();

            Assert.False(LayerRenderer.AnyDrawn(LayerRenderer.Render(new BarLabelLayer { Bars = null, RowLabel = "avg" }, area)));
            Assert.False(LayerRenderer.AnyDrawn(LayerRenderer.Render(new BarLabelLayer { Bars = [], RowLabel = "avg" }, area)));
        });
    }
}
