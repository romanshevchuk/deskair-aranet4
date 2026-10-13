using System.Windows.Controls;
using System.Windows.Documents;
using Deskair.App.Ui.Markup;

namespace Deskair.Tests.Ui;

public class InlineMarkupTests
{
    [Fact]
    public void A_bold_lead_is_split_from_the_rest()
    {
        var pieces = InlineMarkup.Parse("**Worst hours: 15:00–18:00** (avg 1,249 ppm). Open a window.");

        Assert.Equal([("Worst hours: 15:00–18:00", true), (" (avg 1,249 ppm). Open a window.", false)], pieces);
    }

    [Fact]
    public void Text_without_markers_is_one_plain_piece()
    {
        Assert.Equal([("Just a sentence.", false)], InlineMarkup.Parse("Just a sentence."));
    }

    [Fact]
    public void Bold_can_be_in_the_middle_and_more_than_once()
    {
        var pieces = InlineMarkup.Parse("a **b** c **d**");

        Assert.Equal([("a ", false), ("b", true), (" c ", false), ("d", true)], pieces);
    }

    [Fact]
    public void A_marker_that_is_never_closed_leaves_the_rest_in_bold()
    {
        Assert.Equal([("x", false), ("y", true)], InlineMarkup.Parse("x**y"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("****")]
    public void Nothing_to_show_gives_no_pieces(string? text)
    {
        Assert.Empty(InlineMarkup.Parse(text));
    }

    [Fact]
    public void The_attached_text_fills_a_text_block_with_runs_and_bold_runs()
    {
        UiHost.Run(() =>
        {
            var block = new TextBlock();

            InlineMarkup.SetText(block, "**Lead** and the rest");

            Assert.Equal(2, block.Inlines.Count);
            Assert.IsType<Bold>(block.Inlines.First());
            Assert.Equal("Lead and the rest", new TextRange(block.ContentStart, block.ContentEnd).Text.Trim());
        });
    }

    [Fact]
    public void Setting_a_new_text_replaces_the_old_one_and_null_clears_it()
    {
        UiHost.Run(() =>
        {
            var block = new TextBlock();
            InlineMarkup.SetText(block, "**One** two");

            InlineMarkup.SetText(block, "Three");
            Assert.Single(block.Inlines);

            InlineMarkup.SetText(block, null);
            Assert.Empty(block.Inlines);
        });
    }
}
