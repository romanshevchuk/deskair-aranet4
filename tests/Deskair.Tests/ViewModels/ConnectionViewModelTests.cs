using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Connection;
using Deskair.Services.Connection;

namespace Deskair.Tests.ViewModels;

public class ConnectionViewModelTests
{
    [Theory]
    [InlineData(ConnectionState.Normal, false, false, false, false)]
    [InlineData(ConnectionState.Stale, true, false, false, false)]
    [InlineData(ConnectionState.LowBattery, false, true, false, false)]
    [InlineData(ConnectionState.NotConnected, false, false, true, false)]
    [InlineData(ConnectionState.FirstRun, false, false, false, true)]
    public void Flags_come_from_the_state_only(ConnectionState state, bool stale, bool lowBattery, bool offline, bool firstRun)
    {
        var viewModel = new ConnectionViewModel();

        viewModel.Update(state);

        Assert.Equal(state, viewModel.State);
        Assert.Equal(stale, viewModel.ShowStaleBanner);
        Assert.Equal(lowBattery, viewModel.ShowLowBatteryBanner);
        Assert.Equal(offline, viewModel.ShowOfflineView);
        Assert.Equal(firstRun, viewModel.ShowFirstRunView);
    }

    [Fact]
    public void Normal_clears_every_flag()
    {
        var viewModel = new ConnectionViewModel();
        viewModel.Update(ConnectionState.Stale);

        viewModel.Update(ConnectionState.Normal);

        Assert.False(viewModel.ShowStaleBanner);
        Assert.False(viewModel.ShowLowBatteryBanner);
        Assert.False(viewModel.ShowOfflineView);
        Assert.False(viewModel.ShowFirstRunView);
    }

    [Fact]
    public void A_state_change_raises_the_flags()
    {
        var viewModel = new ConnectionViewModel();
        var raised = new List<string?>();
        viewModel.PropertyChanged += (_, args) => raised.Add(args.PropertyName);

        viewModel.Update(ConnectionState.NotConnected);

        Assert.Contains(nameof(ConnectionViewModel.ShowOfflineView), raised);
        Assert.Contains(nameof(ConnectionViewModel.ShowStaleBanner), raised);
    }

    [Fact]
    public void It_follows_connection_messages_through_the_dispatcher()
    {
        var messenger = new StrongReferenceMessenger();
        var dispatcher = new Deskair.Tests.Fakes.FakeUiDispatcher();
        var viewModel = new ConnectionViewModel(messenger, dispatcher);

        messenger.Send(new Deskair.Services.Messages.ConnectionStateChanged(ConnectionState.Stale));

        Assert.Equal(ConnectionState.Stale, viewModel.State);
        Assert.True(viewModel.ShowStaleBanner);
        Assert.Equal(1, dispatcher.Posted);
    }

    [Fact]
    public void Retry_asks_the_app_to_try_the_sensor_again()
    {
        var messenger = new StrongReferenceMessenger();
        var viewModel = new ConnectionViewModel(messenger, new Deskair.Tests.Fakes.FakeUiDispatcher());
        var asked = 0;
        messenger.Register<Deskair.Services.Messages.RetryRequested>(this, (_, _) => asked++);

        viewModel.RetryCommand.Execute(null);

        Assert.Equal(1, asked);
    }

    [Fact]
    public void Retry_without_a_messenger_does_nothing()
    {
        var viewModel = new ConnectionViewModel();

        viewModel.RetryCommand.Execute(null);

        Assert.Equal(ConnectionState.Normal, viewModel.State);
    }

    [Fact]
    public void Dismissing_the_low_battery_banner_hides_it_until_the_state_changes()
    {
        var viewModel = new ConnectionViewModel();
        viewModel.Update(ConnectionState.LowBattery);

        viewModel.DismissLowBatteryCommand.Execute(null);
        Assert.False(viewModel.ShowLowBatteryBanner);

        viewModel.Update(ConnectionState.LowBattery);
        Assert.False(viewModel.ShowLowBatteryBanner);

        viewModel.Update(ConnectionState.Normal);
        viewModel.Update(ConnectionState.LowBattery);
        Assert.True(viewModel.ShowLowBatteryBanner);
    }
}
