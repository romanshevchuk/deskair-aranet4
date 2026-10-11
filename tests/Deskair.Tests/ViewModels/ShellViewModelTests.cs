using Deskair.App.ViewModels.Shell;

namespace Deskair.Tests.ViewModels;

public class ShellViewModelTests
{
    [Fact]
    public void Starts_on_Live()
    {
        Assert.Equal(ShellSection.Live, new ShellViewModel().Section);
    }

    [Theory]
    [InlineData(ShellSection.History)]
    [InlineData(ShellSection.Settings)]
    [InlineData(ShellSection.Live)]
    public void Navigate_changes_the_section(ShellSection target)
    {
        var viewModel = new ShellViewModel();

        viewModel.NavigateCommand.Execute(target);

        Assert.Equal(target, viewModel.Section);
    }

    [Fact]
    public void Changing_the_section_raises_PropertyChanged()
    {
        var viewModel = new ShellViewModel();
        var raised = new List<string?>();
        viewModel.PropertyChanged += (_, args) => raised.Add(args.PropertyName);

        viewModel.Section = ShellSection.History;

        Assert.Contains(nameof(ShellViewModel.Section), raised);
    }
}
