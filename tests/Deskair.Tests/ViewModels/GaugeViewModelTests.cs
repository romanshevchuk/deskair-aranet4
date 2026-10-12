using Deskair.App.ViewModels.Live;

namespace Deskair.Tests.ViewModels;

public class GaugeViewModelTests
{
    [Theory]
    [InlineData(800, 800)]
    [InlineData(400, 400)]
    [InlineData(2000, 2000)]
    [InlineData(100, 400)]
    [InlineData(3500, 2000)]
    public void Update_clamps_to_the_scale(double ppm, double expected)
    {
        var viewModel = new GaugeViewModel();

        viewModel.Update(ppm);

        Assert.Equal(expected, viewModel.Value);
    }

    [Fact]
    public void Update_with_null_clears_the_value()
    {
        var viewModel = new GaugeViewModel();
        viewModel.Update(900);

        viewModel.Update(null);

        Assert.Null(viewModel.Value);
    }
}
