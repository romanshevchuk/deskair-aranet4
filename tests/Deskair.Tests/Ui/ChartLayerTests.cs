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

    /// <summary>Undoes the premultiplied alpha of a rendered pixel, so a half-transparent line can be compared with its token colour.</summary>
    private static Color Unpremultiplied(Color pixel)
        => pixel.A == 0 ? pixel : Color.FromRgb((byte)(pixel.R * 255 / pixel.A), (byte)(pixel.G * 255 / pixel.A), (byte)(pixel.B * 255 / pixel.A));

    [Fact]
    public void A_threshold_outside_the_range_draws_neither_line_nor_label()
    {
        UiHost.Run(() =>
        {
            // 400 to 1200: 1000 is inside (y 50), 1400 is not.
            var area = LayerRenderer.Area(yMin: 400, yMax: 1200);
            var layer = new ThresholdLayer { ElevatedLabel = "Elevated from 1000", HighLabel = "High from 1400" };
            var bitmap = LayerRenderer.Render(layer, area);

            Assert.True(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(45, PixelY(area, 1000) - 1, 100, 2)));
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(40, 0, 250, PixelY(area, 1000) - 25)));
        });
    }

    [Fact]
    public void A_threshold_line_takes_its_zone_colour_and_its_label_sits_above_it_on_the_right()
    {
        UiHost.Run(ThemeChoice.Light, () =>
        {
            var area = LayerRenderer.Area();
            var bitmap = LayerRenderer.Render(new ThresholdLayer { ElevatedLabel = "Elevated from 1000", HighLabel = "High from 1400" }, area);

            foreach (var (limit, zone) in new[] { (1000.0, "Elevated"), (1400.0, "High") })
            {
                var y = PixelY(area, limit);
                var pixel = Enumerable.Range(45, 100).Select(x => LayerRenderer.Pixel(bitmap, x, y)).First(candidate => candidate.A > 100);

                // The line is the zone dot colour at 60 % (D-0017: the zone shows where it means something), not a neutral grey.
                Assert.True(Near(LayerRenderer.Token($"Zone.{zone}.Dot.Brush"), Unpremultiplied(pixel), 12));
                Assert.InRange(pixel.A, 130, 175);

                // The label: right-aligned to the plot, just above the line, nothing below it on the left.
                Assert.True(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(230, y - 18, 58, 14)));
                Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(45, y - 18, 60, 14)));
            }
        });
    }

    [Fact]
    public void A_threshold_on_the_edge_of_the_axis_is_still_drawn_with_its_label_inside_the_plot()
    {
        UiHost.Run(() =>
        {
            // 1000 to 1300, as the Live chart fits a last hour of 1076 to 1171: 1000 is the bottom edge (y 170), 1300 would be the top.
            var bottom = LayerRenderer.Render(new ThresholdLayer { ElevatedLabel = "Elevated from 1000" }, LayerRenderer.Area(yMin: 1000, yMax: 1300));
            var top = LayerRenderer.Render(new ThresholdLayer { HighAbove = 1300, HighLabel = "High from 1300" }, LayerRenderer.Area(yMin: 1000, yMax: 1300));

            Assert.True(LayerRenderer.AnyDrawn(bottom, new Int32Rect(45, 169, 100, 2)));
            Assert.True(LayerRenderer.AnyDrawn(bottom, new Int32Rect(230, 152, 58, 14)));

            // On the top edge (y 10) the label goes under the line, so it stays inside the plot.
            Assert.False(LayerRenderer.AnyDrawn(top, new Int32Rect(40, 0, 250, 9)));
            Assert.True(LayerRenderer.AnyDrawn(top, new Int32Rect(230, 14, 58, 14)));
        });
    }

    [Fact]
    public void The_area_fill_fades_from_the_line_to_the_bottom_and_skips_gaps()
    {
        UiHost.Run(ThemeChoice.Light, () =>
        {
            var area = TimeArea();
            var points = new[] { At(0, 1200), At(30, 1200), At(40, null), At(70, 1200), At(100, 1200) };
            var bitmap = LayerRenderer.Render(new AreaLayer { Points = points, BrushKey = "Zone.Elevated.Line.Brush" }, area);

            var underLine = LayerRenderer.Pixel(bitmap, PixelX(area, 15), PixelY(area, 1200) + 3);
            var nearBottom = LayerRenderer.Pixel(bitmap, PixelX(area, 15), (int)area.Plot.Bottom - 3);

            Assert.InRange(underLine.A, 45, 60);
            Assert.True(nearBottom.A < underLine.A / 4);
            Assert.True(Near(LayerRenderer.Token("Zone.Elevated.Line.Brush"), Unpremultiplied(underLine), 16));
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(PixelX(area, 33), 20, PixelX(area, 67) - PixelX(area, 33), 140)));
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(PixelX(area, 15), 10, 5, PixelY(area, 1200) - 14)));
        });
    }

    [Fact]
    public void A_smoothed_line_passes_through_every_reading_and_does_not_overshoot_a_peak()
    {
        UiHost.Run(() =>
        {
            var area = TimeArea();
            var points = new[] { At(0, 800), At(20, 1000), At(40, 1600), At(60, 1000), At(80, 800), At(100, 900) };
            var bitmap = LayerRenderer.Render(new ZoneLineLayer { Points = points, Smooth = true }, area);

            foreach (var point in points.Take(5))
            {
                var minute = (int)(point.Time - start).TotalMinutes;
                Assert.True(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(PixelX(area, minute) - 1, PixelY(area, point.Value!.Value) - 2, 3, 5)));
            }

            // Monotone: nothing above the 1600 peak (y 70) beyond the line's own thickness.
            Assert.False(LayerRenderer.AnyDrawn(bitmap, new Int32Rect(40, 10, 250, PixelY(area, 1600) - 13)));
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
