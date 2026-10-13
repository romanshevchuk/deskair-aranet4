using System.Windows;
using System.Windows.Media;
using Deskair.App.Ui.Charts;
using Deskair.App.Ui.Charts.Layers;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Services.Settings;

namespace Deskair.Tests.Ui;

public class ChartLayerTests
{
    private static readonly DateTimeOffset start = new(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

    // x runs over 100 minutes: 3 px per minute across the 250 px wide plot (left 40, right 290); y from 400 to 2000 over 160 px (top 10, bottom 170).
    private static ChartArea TimeArea() => LayerRenderer.Area(start.UtcTicks, start.AddMinutes(100).UtcTicks);

    private static ChartPoint At(int minute, double? value) => new(start.AddMinutes(minute), value);

    private static bool Near(Color expected, Color actual, int tolerance = 24)
        => Math.Abs(expected.R - actual.R) <= tolerance && Math.Abs(expected.G - actual.G) <= tolerance && Math.Abs(expected.B - actual.B) <= tolerance;

    private static int PixelX(ChartArea area, int minute) => (int)Math.Round(area.X(start.AddMinutes(minute)));

    private static int PixelY(ChartArea area, double value) => (int)Math.Round(area.Y(value));

    [Fact]
    public void Band_draws_a_different_tint_for_each_zone_inside_the_plot_only()
    {
        UiHost.Run(() =>
        {
            var area = LayerRenderer.Area();
            var bitmap = LayerRenderer.Render(new BandLayer(), area);

            var good = LayerRenderer.Pixel(bitmap, 100, PixelY(area, 700));
            var elevated = LayerRenderer.Pixel(bitmap, 100, PixelY(area, 1200));
            var high = LayerRenderer.Pixel(bitmap, 100, PixelY(area, 1700));

            Assert.True(good.A > 0 && elevated.A > 0 && high.A > 0);
            Assert.NotEqual(good, elevated);
            Assert.NotEqual(elevated, high);
            Assert.True(good.G > good.R, "the good band is green");
            Assert.True(high.R > high.G, "the high band is red");
            Assert.Equal(0, LayerRenderer.Pixel(bitmap, 5, PixelY(area, 700)).A);
            Assert.Equal(0, LayerRenderer.Pixel(bitmap, 100, 190).A);
        });
    }

    [Fact]
    public void Band_with_limits_outside_the_range_draws_only_the_visible_part()
    {
        UiHost.Run(() =>
        {
            var area = LayerRenderer.Area(yMin: 400, yMax: 900);
            var bitmap = LayerRenderer.Render(new BandLayer(), area);

            Assert.True(LayerRenderer.Pixel(bitmap, 100, 100).A > 0);
            Assert.All(new[] { 15, 90, 160 }, y => Assert.True(LayerRenderer.Pixel(bitmap, 100, y).G > LayerRenderer.Pixel(bitmap, 100, y).R));
        });
    }

    [Fact]
    public void Grid_draws_one_line_per_tick_and_nothing_for_zero_ticks()
    {
        UiHost.Run(() =>
        {
            var area = LayerRenderer.Area();
            var withTicks = LayerRenderer.Render(new GridLayer { YTickCount = 4 }, area);
            var without = LayerRenderer.Render(new GridLayer { YTickCount = 0 }, area);

            foreach (var value in new[] { 400.0, 933.3333, 1466.6667, 2000.0 })
            {
                var y = PixelY(area, value);
                Assert.True(LayerRenderer.AnyDrawn(withTicks, new Int32Rect(100, Math.Clamp(y - 1, 0, 198), 1, 2)), $"a line near {value}");
            }

            Assert.False(LayerRenderer.AnyDrawn(without));
        });
    }

    [Fact]
    public void Grid_draws_at_the_given_ticks()
    {
        UiHost.Run(() =>
        {
            var area = LayerRenderer.Area();
            var bitmap = LayerRenderer.Render(new GridLayer { YTicks = [1000] }, area);

            Assert.True(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(100, PixelY(area, 1000) - 1, 1, 2)));
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(100, PixelY(area, 1500) - 2, 1, 4)));
        });
    }

    [Fact]
    public void Dashed_lines_skip_a_limit_outside_the_range()
    {
        UiHost.Run(() =>
        {
            var area = LayerRenderer.Area(yMin: 400, yMax: 1200);
            var bitmap = LayerRenderer.Render(new DashedZoneLinesLayer(), area);

            Assert.True(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(45, PixelY(area, 1000) - 1, 200, 2)));
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(45, 0, 200, PixelY(area, 1000) - 3)));
        });
    }


    [Fact]
    public void Both_dashed_limits_are_the_same_neutral_grey()
    {
        UiHost.Run(() =>
        {
            var area = LayerRenderer.Area();
            var bitmap = LayerRenderer.Render(new DashedZoneLinesLayer(), area);
            var expected = LayerRenderer.Token("Card.Border.Hover.Brush");

            foreach (var limit in new[] { 1000.0, 1400.0 })
            {
                var y = PixelY(area, limit);
                var pixel = Enumerable.Range(45, 200).Select(x => LayerRenderer.Pixel(bitmap, x, y)).First(candidate => candidate.A > 200);

                Assert.Equal(expected, Color.FromRgb(pixel.R, pixel.G, pixel.B));
            }
        });
    }
    [Fact]
    public void Zone_line_is_coloured_by_zone()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var points = new[] { At(0, 700), At(20, 700), At(40, 1700), At(60, 1700) };
            var bitmap = LayerRenderer.Render(new ZoneLineLayer { Points = points }, area);

            var green = LayerRenderer.Pixel(bitmap, PixelX(area, 10), PixelY(area, 700));
            var red = LayerRenderer.Pixel(bitmap, PixelX(area, 50), PixelY(area, 1700));

            Assert.Equal(LayerRenderer.Token("Zone.Good.Line.Brush"), Color.FromRgb(green.R, green.G, green.B));
            Assert.Equal(LayerRenderer.Token("Zone.High.Line.Brush"), Color.FromRgb(red.R, red.G, red.B));
        });
    }

    [Fact]
    public void Zone_line_breaks_at_a_gap_and_never_bridges_it()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var points = new[] { At(0, 700), At(20, 700), At(30, null), At(60, 700), At(80, 700) };
            var bitmap = LayerRenderer.Render(new ZoneLineLayer { Points = points }, area);
            var y = PixelY(area, 700);

            Assert.True(LayerRenderer.Pixel(bitmap, PixelX(area, 10), y).A > 0);
            Assert.True(LayerRenderer.Pixel(bitmap, PixelX(area, 70), y).A > 0);
            Assert.Equal(0, LayerRenderer.Pixel(bitmap, PixelX(area, 45), y).A);
        });
    }

    [Fact]
    public void Zone_line_with_one_point_draws_a_dot_and_with_none_draws_nothing()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var one = LayerRenderer.Render(new ZoneLineLayer { Points = [At(50, 1200)] }, area);
            var none = LayerRenderer.Render(new ZoneLineLayer { Points = [] }, area);
            var missing = LayerRenderer.Render(new ZoneLineLayer(), area);

            Assert.True(LayerRenderer.AnyDrawn(one));
            Assert.False(LayerRenderer.AnyDrawn(none));
            Assert.False(LayerRenderer.AnyDrawn(missing));
        });
    }

    [Fact]
    public void Zone_line_marks_the_latest_value_with_a_ring_in_its_zone_colour()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var bitmap = LayerRenderer.Render(new ZoneLineLayer { Points = [At(0, 700), At(80, 1200)] }, area);
            var ring = LayerRenderer.Pixel(bitmap, PixelX(area, 80) + 4, PixelY(area, 1200));

            Assert.True(Near(LayerRenderer.Token("Zone.Elevated.Line.Brush"), ring), $"the ring colour was {ring}");
        });
    }

    [Fact]
    public void Line_uses_the_brush_it_is_given()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var bitmap = LayerRenderer.Render(new LineLayer { Points = [At(0, 1200), At(80, 1200)], BrushKey = "Metric.Humidity.Brush" }, area);
            var pixel = LayerRenderer.Pixel(bitmap, PixelX(area, 40), PixelY(area, 1200));

            Assert.Equal(LayerRenderer.Token("Metric.Humidity.Brush"), Color.FromRgb(pixel.R, pixel.G, pixel.B));
        });
    }

    [Fact]
    public void Ideal_band_fills_the_plot_and_tints_the_ideal_range()
    {
        UiHost.Run(() =>
        {
            var area = LayerRenderer.Area(yMin: 15, yMax: 30);
            var layer = new IdealBandLayer { IdealMin = 20, IdealMax = 24, FillBrushKey = "Metric.Temperature.Fill.Brush", IdealBrushKey = "Metric.Temperature.Ideal.Brush" };
            var bitmap = LayerRenderer.Render(layer, area);

            var outside = LayerRenderer.Pixel(bitmap, 100, PixelY(area, 28));
            var inside = LayerRenderer.Pixel(bitmap, 100, PixelY(area, 22));

            Assert.True(outside.A > 0);
            Assert.True(inside.A > outside.A, "the ideal range is stronger than the plot fill");
            Assert.Equal(0, LayerRenderer.Pixel(bitmap, 5, PixelY(area, 22)).A);
        });
    }

    [Fact]
    public void Crosshair_draws_nothing_without_a_hover_or_outside_the_plot()
    {
        UiHost.Run(() =>
        {
            var area = LayerRenderer.Area();

            Assert.False(LayerRenderer.AnyDrawn(LayerRenderer.Render(new CrosshairLayer(), area)));
            Assert.False(LayerRenderer.AnyDrawn(LayerRenderer.Render(new CrosshairLayer { Hover = new ChartHover(start, 1000, 5, 50) }, area)));
        });
    }

    [Fact]
    public void Crosshair_draws_a_line_and_a_dot_at_the_hover()
    {
        UiHost.Run(() =>
        {
            var area = LayerRenderer.Area();
            var hover = new ChartHover(start, 1000, 150, area.Y(1000));
            var bitmap = LayerRenderer.Render(new CrosshairLayer { Hover = hover }, area);

            Assert.True(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(149, 20, 3, 10)), "the vertical line");
            Assert.True(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(146, (int)area.Y(1000) - 3, 8, 6)), "the dot");
        });
    }

    [Fact]
    public void Crosshair_with_no_data_draws_the_line_but_no_dot()
    {
        UiHost.Run(() =>
        {
            var area = LayerRenderer.Area();
            var bitmap = LayerRenderer.Render(new CrosshairLayer { Hover = new ChartHover(start, null, 150, null) }, area);

            Assert.True(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(149, 20, 3, 10)));
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(153, 80, 8, 20)), "no dot beside the line");
        });
    }

    [Fact]
    public void Crosshair_readout_is_drawn_when_it_has_text()
    {
        UiHost.Run(() =>
        {
            var area = LayerRenderer.Area();
            var plain = LayerRenderer.Render(new CrosshairLayer { Hover = new ChartHover(start, 1000, 150, area.Y(1000)) }, area);
            var withText = LayerRenderer.Render(new CrosshairLayer { Hover = new ChartHover(start, 1000, 150, area.Y(1000), "1,000 ppm · 10:00") }, area);

            Assert.False(LayerRenderer.AnyDrawn(plain, new Int32Rect(165, 20, 60, 20)));
            Assert.True(LayerRenderer.AnyDrawn(withText, new Int32Rect(165, 20, 60, 20)));
        });
    }

    [Fact]
    public void No_data_message_is_drawn_in_the_plot_only_when_there_is_text()
    {
        UiHost.Run(() =>
        {
            var area = LayerRenderer.Area();

            Assert.True(LayerRenderer.AnyDrawn(LayerRenderer.Render(new NoDataLayer { Text = "No data" }, area)));
            Assert.False(LayerRenderer.AnyDrawn(LayerRenderer.Render(new NoDataLayer(), area)));
        });
    }

    [Fact]
    public void Ribbon_draws_zone_blocks_and_leaves_missing_time_as_track()
    {
        UiHost.Run(() =>
        {
            var area = ChartArea.Create(new Size(LayerRenderer.Width, LayerRenderer.Height), new Thickness(0), start.UtcTicks, start.AddHours(24).UtcTicks, 0, 1)
                with
            { Resources = key => Application.Current.TryFindResource(key) };
            var segments = new[]
            {
                new ChartZoneSegment(start, start.AddHours(8), Deskair.Domain.Zones.Zone.Good),
                new ChartZoneSegment(start.AddHours(16), start.AddHours(24), Deskair.Domain.Zones.Zone.High)
            };
            var bitmap = LayerRenderer.Render(new RibbonLayer { Segments = segments }, area);
            var middle = LayerRenderer.Height / 2;

            var good = LayerRenderer.Pixel(bitmap, 50, middle);
            var gap = LayerRenderer.Pixel(bitmap, 150, middle);
            var high = LayerRenderer.Pixel(bitmap, 250, middle);

            Assert.Equal(LayerRenderer.Token("Zone.Good.Segment.Brush"), Color.FromRgb(good.R, good.G, good.B));
            Assert.Equal(LayerRenderer.Token("Zone.High.Segment.Brush"), Color.FromRgb(high.R, high.G, high.B));
            Assert.Equal(LayerRenderer.Token("Segment.Bg.Brush"), Color.FromRgb(gap.R, gap.G, gap.B));
        });
    }

    [Fact]
    public void The_chart_layers_follow_the_theme()
    {
        Color Sample() => LayerRenderer.Pixel(LayerRenderer.Render(new GridLayer { YTicks = [1000] }, LayerRenderer.Area()), 100, PixelY(LayerRenderer.Area(), 1000));

        var light = UiHost.Run(ThemeChoice.Light, Sample);
        var dark = UiHost.Run(ThemeChoice.Dark, Sample);

        Assert.NotEqual(light, dark);
    }
}
