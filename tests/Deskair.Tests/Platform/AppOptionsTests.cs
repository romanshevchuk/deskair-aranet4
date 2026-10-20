using Deskair.App.Composition;

namespace Deskair.Tests.Platform;

public class AppOptionsTests
{
    [Fact]
    public void No_arguments_run_the_real_app()
    {
        var options = AppOptions.Parse([]);

        Assert.False(options.Gallery);
        Assert.False(options.Simulated);
        Assert.False(options.FirstRun);
        Assert.Null(options.Sensor);
    }
    [Fact] public void The_test_toast_switch_is_off_unless_asked_for() { Assert.False(AppOptions.Parse([]).TestToast); Assert.True(AppOptions.Parse(["--simulated", "--TEST-TOAST"]).TestToast); }

    [Theory]
    [InlineData("--gallery")]
    [InlineData("--GALLERY")]
    public void The_gallery_flag_is_found_in_any_case(string argument)
    {
        Assert.True(AppOptions.Parse([argument]).Gallery);
    }

    [Fact]
    public void The_simulated_flag_is_found_next_to_other_arguments()
    {
        var options = AppOptions.Parse(["--other", "--simulated"]);

        Assert.True(options.Simulated);
        Assert.False(options.Gallery);
    }

    [Fact]
    public void The_sensor_option_is_read_with_its_value()
    {
        Assert.Equal("C3DEC8FCC7C6", AppOptions.Parse(["--sensor", "C3DEC8FCC7C6"]).Sensor);
        Assert.Null(AppOptions.Parse(["--sensor"]).Sensor);
    }

    [Fact]
    public void The_first_run_option_is_read()
    {
        Assert.True(AppOptions.Parse(["--first-run"]).FirstRun);
    }

    [Fact]
    public void The_background_argument_of_the_startup_entry_is_recognised()
    {
        Assert.True(AppOptions.Parse(["--BACKGROUND"]).StartedInBackground);
        Assert.False(AppOptions.Parse([]).StartedInBackground);
    }

    [Theory]
    [InlineData("-ToastActivated")]
    [InlineData("-Embedding")]
    [InlineData("-embedding")]
    public void A_start_caused_by_a_toast_press_is_recognised(string argument)
    {
        Assert.True(AppOptions.Parse([argument]).StartedByToast);
        Assert.False(AppOptions.Parse([]).StartedByToast);
    }
}
