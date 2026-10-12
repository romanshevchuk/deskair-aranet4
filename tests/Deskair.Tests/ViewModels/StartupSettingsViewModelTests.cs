using Deskair.App.ViewModels.Settings;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging;

namespace Deskair.Tests.ViewModels;

public class StartupSettingsViewModelTests
{
    private readonly FakeStartupRegistration registration = new();
    private readonly ListLogger<StartupSettingsViewModel> logger = new();
    private readonly StartupSettingsViewModel viewModel;

    public StartupSettingsViewModelTests()
    {
        viewModel = new StartupSettingsViewModel(registration, logger);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Load_shows_the_registration_state(bool enabled)
    {
        registration.IsEnabled = enabled;

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(enabled, viewModel.StartWithWindows);
    }

    [Fact]
    public async Task Loading_does_not_write_the_registration()
    {
        registration.IsEnabled = true;

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, registration.Calls);
    }

    [Fact]
    public async Task Changing_the_switch_updates_the_registration()
    {
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.StartWithWindows = true;

        await Wait.Until(() => registration.IsEnabled);
        Assert.True(viewModel.StartWithWindows);
    }

    [Fact]
    public async Task A_failing_registration_reverts_the_switch_and_logs()
    {
        registration.Fail = true;
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.StartWithWindows = true;

        await Wait.Until(() => !viewModel.StartWithWindows && logger.Entries.Count > 0);
        Assert.False(registration.IsEnabled);
        Assert.Equal(LogLevel.Warning, logger.Entries[0].Level);
    }

    [Fact]
    public async Task Reverting_does_not_try_again()
    {
        registration.Fail = true;
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.StartWithWindows = true;
        await Wait.Until(() => !viewModel.StartWithWindows && logger.Entries.Count > 0);

        Assert.Equal(1, registration.Calls);
    }
}
