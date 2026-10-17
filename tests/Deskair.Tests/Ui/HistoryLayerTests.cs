using System.Windows;
using System.Windows.Media;
using Deskair.App.Ui.Charts;
using Deskair.App.Ui.Charts.Layers;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.Tests.Ui;

/// <summary>The gap layer of the History chart, looked at pixel by pixel.</summary>
public class HistoryLayerTests
{
    private static readonly DateTimeOffset start = new(2026, 10, 12, 0, 0, 0, TimeSpan.Zero);

    // x runs over 100 minutes: 2.5 px per minute across the 250 px wide plot (left 40); y from 400 to 2000 over 160 px (top 10, bottom 170).
    private static ChartArea TimeArea() => LayerRenderer.Area(start.UtcTicks, start.AddMinutes(100).UtcTicks);

    private static int X(ChartArea area, int minute) => (int)Math.Round(area.X(start.AddMinutes(minute)));

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
