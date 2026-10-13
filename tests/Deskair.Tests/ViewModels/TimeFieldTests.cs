using Deskair.App.ViewModels.Settings;

namespace Deskair.Tests.ViewModels;

public class TimeFieldTests
{
    private readonly TimeField field = new();
    private readonly List<TimeOnly> changes = [];

    public TimeFieldTests()
    {
        field.ValueChanged += (_, value) => changes.Add(value);
    }

    [Fact]
    public void Load_shows_the_time_as_hours_and_minutes_and_raises_nothing()
    {
        field.Load(new TimeOnly(6, 5));

        Assert.Equal("06:05", field.Text);
        Assert.Equal(new TimeOnly(6, 5), field.Value);
        Assert.False(field.IsInvalid);
        Assert.Empty(changes);
    }

    [Theory]
    [InlineData("21:15", 21, 15)]
    [InlineData("9:05", 9, 5)]
    [InlineData(" 22:00 ", 22, 0)]
    public void A_valid_time_typed_becomes_the_value_and_is_announced(string text, int hour, int minute)
    {
        field.Load(new TimeOnly(1, 0));

        field.Text = text;

        Assert.Equal(new TimeOnly(hour, minute), field.Value);
        Assert.Equal([new TimeOnly(hour, minute)], changes);
        Assert.False(field.IsInvalid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("25:00")]
    [InlineData("12:60")]
    [InlineData("noon")]
    public void Something_that_is_not_a_time_is_flagged_and_does_not_change_the_value(string text)
    {
        field.Load(new TimeOnly(7, 0));

        field.Text = text;

        Assert.True(field.IsInvalid);
        Assert.Equal(new TimeOnly(7, 0), field.Value);
        Assert.Empty(changes);
    }

    [Fact]
    public void Fixing_a_flagged_time_clears_the_flag()
    {
        field.Load(new TimeOnly(7, 0));
        field.Text = "noon";

        field.Text = "08:00";

        Assert.False(field.IsInvalid);
        Assert.Equal([new TimeOnly(8, 0)], changes);
    }

    [Fact]
    public void Typing_the_same_time_in_another_form_is_not_a_change()
    {
        field.Load(new TimeOnly(9, 5));

        field.Text = "9:05";

        Assert.Empty(changes);
    }

    [Fact]
    public void Load_clears_a_flag_left_by_a_typo()
    {
        field.Load(new TimeOnly(7, 0));
        field.Text = "noon";

        field.Load(new TimeOnly(22, 0));

        Assert.False(field.IsInvalid);
        Assert.Equal("22:00", field.Text);
        Assert.Empty(changes);
    }
}
