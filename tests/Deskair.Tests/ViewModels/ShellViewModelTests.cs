using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Onboarding;
using Deskair.App.ViewModels.Shell;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

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

    [Fact]
    public void Adding_a_sensor_goes_to_Live_and_opens_the_sensor_search()
    {
        var messenger = new StrongReferenceMessenger();
        var store = new FakeDeviceStore();
        var registry = new DeviceRegistry(store);
        var onboarding = new OnboardingViewModel(
            [],
            new DeviceEnrollment(store, registry, new SettingsService(new FakeSettingsStore(), messenger), messenger),
            new FakeBluetoothRadio(),
            new FakeLocalizer(),
            new FakeClock(DateTimeOffset.UnixEpoch),
            NullLogger<OnboardingViewModel>.Instance);
        var viewModel = new ShellViewModel(messenger) { Onboarding = onboarding, Section = ShellSection.Settings };

        messenger.Send(new AddSensorRequested());

        Assert.Equal(ShellSection.Live, viewModel.Section);
        Assert.True(onboarding.IsOpen);
    }

    [Fact]
    public void Asking_to_import_goes_to_Settings()
    {
        var messenger = new StrongReferenceMessenger();
        var viewModel = new ShellViewModel(messenger) { Section = ShellSection.History };

        messenger.Send(new ImportRequested());

        Assert.Equal(ShellSection.Settings, viewModel.Section);
    }

    [Fact]
    public void Adding_a_sensor_without_the_welcome_screen_only_goes_to_Live()
    {
        var messenger = new StrongReferenceMessenger();
        var viewModel = new ShellViewModel(messenger) { Section = ShellSection.Settings };

        messenger.Send(new AddSensorRequested());

        Assert.Equal(ShellSection.Live, viewModel.Section);
    }
}
