using System.Windows;
using System.Windows.Media;
using Deskair.App.Ui.Charts;
using Deskair.App.Ui.Charts.Layers;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Aggregation;

namespace Deskair.Tests.Ui;

/// <summary>The layers of the History chart, looked at pixel by pixel.</summary>
public class HistoryLayerTests
{
    private static readonly DateTimeOffset start = new(2026, 10, 12, 0, 0, 0, TimeSpan.Zero);

    // x runs over 100 minutes: 2.5 px per minute across the 250 px wide plot (left 40); y from 400 to 2000 over 160 px (top 10, bottom 170).
    private static ChartArea TimeArea() => LayerRenderer.Area(start.UtcTicks, start.AddMinutes(100).UtcTicks);

    private static ChartBar Bar(int fromMinute, int toMinute, double? min, double? max, double? average, BucketState state = BucketState.Complete)
        => new(start.AddMinutes(fromMinute), start.AddMinutes(toMinute), min, max, average, state);

    private static int X(ChartArea area, int minute) => (int)Math.Round(area.X(start.AddMinutes(minute)));

    private static int Y(ChartArea area, double value) => (int)Math.Round(area.Y(value));

    private static bool Near(Color expected, Color actual, int tolerance = 12)
        => Math.Abs(expected.R - actual.R) <= tolerance && Math.Abs(expected.G - actual.G) <= tolerance && Math.Abs(expected.B - actual.B) <= tolerance;

    /// <summary>What a colour looks like at the given opacity over nothing: the bitmap holds premultiplied values.</summary>
    private static Color Soft(Color colour, double opacity)
        => Color.FromArgb((byte)Math.Round(255 * opacity), (byte)Math.Round(colour.R * opacity), (byte)Math.Round(colour.G * opacity), (byte)Math.Round(colour.B * opacity));

    private static bool NearSoft(Color expected, Color actual, int tolerance = 10)
        => Math.Abs(expected.A - actual.A) <= tolerance && Math.Abs(expected.R - actual.R) <= tolerance && Math.Abs(expected.G - actual.G) <= tolerance && Math.Abs(expected.B - actual.B) <= tolerance;

    [Fact]
    public void A_complete_bar_is_a_soft_pill_in_the_metric_colour_from_its_low_to_its_high()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var bitmap = LayerRenderer.Render(new RangeBarLayer { Bars = [Bar(10, 30, 700, 1200, 900)] }, area);

            var inside = LayerRenderer.Pixel(bitmap, X(area, 20), Y(area, 950));

            Assert.True(NearSoft(Soft(LayerRenderer.Token("Metric.Co2.Brush"), 0.4), inside), $"40 % of the metric colour, was {inside}");
            Assert.Equal(0, LayerRenderer.Pixel(bitmap, X(area, 20), Y(area, 1300)).A);
            Assert.Equal(0, LayerRenderer.Pixel(bitmap, X(area, 20), Y(area, 600)).A);
            Assert.Equal(0, LayerRenderer.Pixel(bitmap, X(area, 40), Y(area, 950)).A);
        });
    }

    [Fact]
    public void A_bar_is_narrower_than_its_slot_but_never_wider_than_forty_pixels()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var narrow = LayerRenderer.Render(new RangeBarLayer { Bars = [Bar(10, 30, 700, 1200, 900)] }, area);
            var wide = LayerRenderer.Render(new RangeBarLayer { Bars = [Bar(0, 100, 700, 1200, 900)] }, area);
            var y = Y(area, 950);

            // The slot is 50 px: the pill is 56 % of it (28 px) and leaves the 2 px next to the slot edges empty.
            Assert.Equal(0, LayerRenderer.Pixel(narrow, X(area, 10) + 2, y).A);
            Assert.True(LayerRenderer.Pixel(narrow, X(area, 20) - 12, y).A > 50);

            // A 250 px slot would give 140 px: the pill stops at 40 px, centred.
            var drawn = Enumerable.Range(0, LayerRenderer.Width).Count(x => LayerRenderer.Pixel(wide, x, y).A > 50);
            Assert.InRange(drawn, 38, 41);
        });
    }

    [Theory]
    [InlineData(900, "Good")]
    [InlineData(1200, "Elevated")]
    [InlineData(1700, "High")]
    public void A_CO2_bar_takes_the_soft_colour_of_the_zone_of_its_average(double average, string zone)
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var bitmap = LayerRenderer.Render(new RangeBarLayer { Bars = [Bar(10, 30, 700, 1900, average)], ZoneColoured = true }, area);

            var inside = LayerRenderer.Pixel(bitmap, X(area, 20), Y(area, 1100));

            Assert.True(NearSoft(Soft(LayerRenderer.Token($"Zone.{zone}.Segment.Brush"), 0.4), inside), $"{zone}: {inside}");
        });
    }

    [Fact]
    public void A_partial_bar_is_lighter_with_a_dashed_outline()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var bitmap = LayerRenderer.Render(new RangeBarLayer { Bars = [Bar(10, 30, 700, 1200, 900, BucketState.Partial)] }, area);

            var inside = LayerRenderer.Pixel(bitmap, X(area, 20), Y(area, 950));
            var edge = Enumerable.Range(X(area, 10) + 2, X(area, 30) - X(area, 10) - 4).Select(x => LayerRenderer.Pixel(bitmap, x, Y(area, 1200))).ToList();

            Assert.InRange(inside.A, 1, 140);
            Assert.Contains(edge, pixel => pixel.A > 200);
            Assert.Contains(edge, pixel => pixel.A < 200);
        });
    }

    [Fact]
    public void A_bucket_with_no_data_draws_no_bar()
    {
        UiHost.Run(() =>
        {
            var bitmap = LayerRenderer.Render(new RangeBarLayer { Bars = [Bar(10, 30, null, null, null, BucketState.NoData), Bar(40, 60, 800, 900, 850, BucketState.NoData)] }, TimeArea());

            Assert.False(LayerRenderer.AnyDrawn(bitmap));
        });
    }

    [Fact]
    public void A_bar_with_equal_low_and_high_still_shows_two_pixels()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var bitmap = LayerRenderer.Render(new RangeBarLayer { Bars = [Bar(10, 30, 1000, 1000, 1000)] }, area);
            var column = X(area, 20);

            var drawn = Enumerable.Range(0, LayerRenderer.Height).Count(y => LayerRenderer.Pixel(bitmap, column, y).A > 40);

            Assert.True(drawn >= 2, $"{drawn} pixels");
        });
    }

    [Fact]
    public void Bars_outside_the_plot_do_not_draw_there()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var bitmap = LayerRenderer.Render(new RangeBarLayer { Bars = [Bar(-30, 20, 300, 2500, 1000)] }, area);

            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(0, 0, 38, LayerRenderer.Height)));
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(40, 0, 200, 9)));
        });
    }

    [Fact]
    public void An_average_tick_is_drawn_across_each_bar_at_its_average()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var bitmap = LayerRenderer.Render(new AverageLineLayer { Bars = [Bar(10, 30, 700, 1200, 900)] }, area);

            Assert.True(LayerRenderer.Pixel(bitmap, X(area, 20), Y(area, 900)).A > 200);
            Assert.Equal(0, LayerRenderer.Pixel(bitmap, X(area, 20), Y(area, 1100)).A);
            Assert.Equal(0, LayerRenderer.Pixel(bitmap, X(area, 40), Y(area, 900)).A);
        });
    }


    [Fact]
    public void The_average_tick_is_three_pixels_thick_and_a_little_wider_than_its_bar()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var bar = Bar(10, 30, 700, 1200, 900);
            var bars = LayerRenderer.Render(new RangeBarLayer { Bars = [bar] }, area);
            var tick = LayerRenderer.Render(new AverageLineLayer { Bars = [bar] }, area);
            var y = Y(area, 900);
            var barColumns = Enumerable.Range(0, LayerRenderer.Width).Where(x => LayerRenderer.Pixel(bars, x, Y(area, 1100)).A > 40).ToList();
            var tickColumns = Enumerable.Range(0, LayerRenderer.Width).Where(x => LayerRenderer.Pixel(tick, x, y).A > 100).ToList();

            Assert.True(tickColumns.Min() < barColumns.Min() && tickColumns.Max() > barColumns.Max(), "the tick reaches past both sides of the bar");
            Assert.InRange(Enumerable.Range(0, LayerRenderer.Height).Count(row => LayerRenderer.Pixel(tick, X(area, 20), row).A > 100), 2, 4);
        });
    }

    [Theory]
    [InlineData(900, "Good")]
    [InlineData(1200, "Elevated")]
    [InlineData(1700, "High")]
    public void A_CO2_tick_takes_the_strong_colour_of_the_zone(double average, string zone)
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var bitmap = LayerRenderer.Render(new AverageLineLayer { Bars = [Bar(10, 30, 700, 1900, average)], ZoneColoured = true }, area);

            Assert.True(Near(LayerRenderer.Token($"Zone.{zone}.Line.Brush"), LayerRenderer.Pixel(bitmap, X(area, 20), Y(area, average))));
        });
    }
    [Fact]
    public void Buckets_without_an_average_or_data_get_no_tick()
    {
        UiHost.Run(() =>
        {
            var bitmap = LayerRenderer.Render(new AverageLineLayer { Bars = [Bar(10, 30, 700, 1200, null), Bar(40, 60, 800, 900, 850, BucketState.NoData)] }, TimeArea());

            Assert.False(LayerRenderer.AnyDrawn(bitmap));
        });
    }

    [Fact]
    public void No_bars_draws_nothing()
    {
        UiHost.Run(() =>
        {
            Assert.False(LayerRenderer.AnyDrawn(LayerRenderer.Render(new RangeBarLayer(), TimeArea())));
            Assert.False(LayerRenderer.AnyDrawn(LayerRenderer.Render(new AverageLineLayer { Bars = [] }, TimeArea())));
            Assert.False(LayerRenderer.AnyDrawn(LayerRenderer.Render(new WeekendShadingLayer(), TimeArea())));
            Assert.False(LayerRenderer.AnyDrawn(LayerRenderer.Render(new GapHatchLayer(), TimeArea())));
        });
    }

    [Fact]
    public void A_weekend_span_is_shaded_the_full_height_of_the_plot()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var layer = new WeekendShadingLayer { WeekendSpans = [new ChartGap(start.AddMinutes(20), start.AddMinutes(50))] };
            var bitmap = LayerRenderer.Render(layer, area);

            Assert.True(LayerRenderer.Pixel(bitmap, X(area, 30), 15).A > 0);
            Assert.True(LayerRenderer.Pixel(bitmap, X(area, 30), 165).A > 0);
            Assert.Equal(0, LayerRenderer.Pixel(bitmap, X(area, 10), 100).A);
            Assert.Equal(0, LayerRenderer.Pixel(bitmap, X(area, 60), 100).A);
            Assert.Equal(0, LayerRenderer.Pixel(bitmap, X(area, 30), 5).A);
        });
    }

    [Fact]
    public void Weekend_spans_outside_the_range_are_skipped_and_partly_visible_ones_are_clipped()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var layer = new WeekendShadingLayer { WeekendSpans = [new ChartGap(start.AddMinutes(-200), start.AddMinutes(-100)), new ChartGap(start.AddMinutes(80), start.AddMinutes(300))] };
            var bitmap = LayerRenderer.Render(layer, area);

            Assert.True(LayerRenderer.Pixel(bitmap, X(area, 90), 100).A > 0);
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(0, 0, 38, LayerRenderer.Height)));
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(292, 0, 8, LayerRenderer.Height)));
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(40, 0, 100, LayerRenderer.Height)));
        });
    }

    [Fact]
    public void A_wide_gap_is_hatched_with_lines_not_filled()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var bitmap = LayerRenderer.Render(new GapHatchLayer { Gaps = [new ChartGap(start.AddMinutes(20), start.AddMinutes(60))] }, area);
            var region = new Int32Rect(X(area, 25), 40, 40, 16);

            var pixels = new byte[region.Width * region.Height * 4];
            bitmap.CopyPixels(region, pixels, region.Width * 4, 0);
            var alphas = Enumerable.Range(0, region.Width * region.Height).Select(index => pixels[(index * 4) + 3]).ToList();

            Assert.Contains(alphas, alpha => alpha > 40);
            Assert.True(alphas.Count(alpha => alpha == 0) > alphas.Count / 2, "most of a hatched band is empty");
            Assert.Equal(0, LayerRenderer.Pixel(bitmap, X(area, 10), 100).A);
            Assert.Equal(0, LayerRenderer.Pixel(bitmap, X(area, 70), 100).A);
        });
    }

    [Fact]
    public void A_gap_narrower_than_a_tile_is_a_solid_strip()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var bitmap = LayerRenderer.Render(new GapHatchLayer { Gaps = [new ChartGap(start.AddMinutes(20), start.AddMinutes(22))] }, area);

            var column = X(area, 20) + 2;
            var alphas = Enumerable.Range(20, 120).Select(y => LayerRenderer.Pixel(bitmap, column, y).A).ToList();

            Assert.All(alphas, alpha => Assert.True(alpha > 0));
        });
    }

    [Fact]
    public void Touching_and_overlapping_gaps_are_merged_into_one()
    {
        var merged = GapHatchLayer.Merge(
        [
            new ChartGap(start.AddMinutes(30), start.AddMinutes(40)),
            new ChartGap(start.AddMinutes(10), start.AddMinutes(20)),
            new ChartGap(start.AddMinutes(20), start.AddMinutes(25)),
            new ChartGap(start.AddMinutes(22), start.AddMinutes(24)),
            new ChartGap(start.AddMinutes(60), start.AddMinutes(70))
        ]);

        Assert.Equal(
            [(10, 25), (30, 40), (60, 70)],
            merged.Select(gap => ((int)(gap.Start - start).TotalMinutes, (int)(gap.End - start).TotalMinutes)));
    }
}
