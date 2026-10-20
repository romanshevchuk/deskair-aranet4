using System.Windows;
using Deskair.App.Ui.Charts;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.Tests.Ui;

public class HourGridLayoutTests
{
    private static HourGridRow Row(int shown = 24)
        => new("Mon 12", Enumerable.Range(0, 24).Select(hour => hour < shown ? new HourGridCell(900, "x") : null).ToList());

    private static HourGridModel Model(int rows, bool typical = true, bool collapsed = false)
    {
        var list = Enumerable.Range(0, rows).Select(_ => Row()).ToList();
        if (collapsed)
        {
            list.Insert(1, new HourGridRow(string.Empty, [], CollapsedText: "No readings"));
        }

        return new HourGridModel(list, [], []) { Typical = typical ? Row() : null, Night = (22, 7) };
    }

    [Fact]
    public void A_week_gets_rows_of_at_most_44_px_and_the_squares_fill_the_middle()
    {
        var layout = new HourGridLayout(Model(7), new Size(800, 600));

        Assert.Equal(44, layout.RowHeight);
        Assert.Equal(800 - 84 - 160, layout.GridWidth);
        Assert.Equal(new Rect(85, layout.Top + 1, (layout.GridWidth / 24) - 2, 42), layout.Cell(0, 0));
    }

    [Fact]
    public void Short_tags_keep_the_normal_end_column()
    {
        var layout = new HourGridLayout(Model(7), new Size(800, 600), tagWidth: 33);

        Assert.Equal(160, layout.EndWidth);
        Assert.Equal(800 - 84 - 160, layout.GridWidth);
    }

    [Fact]
    public void A_long_tag_widens_the_end_column_and_narrows_the_squares()
    {
        var layout = new HourGridLayout(Model(7), new Size(800, 600), tagWidth: 66);

        Assert.Equal(HourGridLayout.TagLeft + 66, layout.EndWidth);
        Assert.Equal(800 - 84 - layout.EndWidth, layout.GridWidth);
    }

    [Fact]
    public void A_month_shares_the_height_and_gets_compact_rows()
    {
        var layout = new HourGridLayout(Model(30), new Size(800, 470));

        Assert.InRange(layout.RowHeight, 9, 14);
        Assert.True(layout.IsCompact);
    }

    [Fact]
    public void A_folded_row_is_28_px_and_the_typical_row_follows_a_16_px_gap()
    {
        var layout = new HourGridLayout(Model(3, collapsed: true), new Size(800, 600));

        Assert.Equal(layout.RowTop(1) + 28, layout.RowTop(2));
        Assert.Equal(layout.RowsBottom + 16, layout.TypicalTop);
    }

    [Fact]
    public void Hit_testing_finds_squares_and_ignores_labels_folded_rows_and_hours_after_now()
    {
        var model = Model(2, collapsed: true) with { Rows = [Row(), new HourGridRow(string.Empty, [], CollapsedText: "No readings"), Row(shown: 10)] };
        var layout = new HourGridLayout(model, new Size(800, 600));
        Point Centre(int row, int column) => new(layout.Cell(row, column).Left + 2, layout.Cell(row, column).Top + 2);

        Assert.Equal((0, 5), layout.HitTest(Centre(0, 5)));
        Assert.Equal((-1, 23), layout.HitTest(Centre(-1, 23)));
        Assert.Null(layout.HitTest(new Point(20, layout.RowTop(0) + 5)));
        Assert.Null(layout.HitTest(new Point(300, layout.RowTop(1) + 5)));
        Assert.Null(layout.HitTest(Centre(2, 12)));
    }
}
