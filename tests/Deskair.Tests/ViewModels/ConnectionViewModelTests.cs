using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Connection;
using Deskair.Domain.Readings;
using Deskair.Services.Connection;
using Deskair.Services.Messages;
using Deskair.Tests.Fakes;

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
        var dispatcher = new FakeUiDispatcher();
        var viewModel = new ConnectionViewModel(messenger, dispatcher, new FakeLocalizer());

        messenger.Send(new ConnectionStateChanged(ConnectionState.Stale));

        Assert.Equal(ConnectionState.Stale, viewModel.State);
        Assert.True(viewModel.ShowStaleBanner);
        Assert.Equal(1, dispatcher.Posted);
    }

    [Fact]
    public void Retry_asks_the_app_to_try_the_sensor_again()
    {
        var messenger = new StrongReferenceMessenger();
        var viewModel = new ConnectionViewModel(messenger, new FakeUiDispatcher(), new FakeLocalizer());
        var asked = 0;
        messenger.Register<RetryRequested>(this, (_, _) => asked++);

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

    private static readonly DeviceId office = new("test", "A1");

    private readonly StrongReferenceMessenger messenger = new();

    private ConnectionViewModel CreateConnected() => new(messenger, new FakeUiDispatcher(), new FakeLocalizer());

    [Fact]
    public void Before_any_retry_the_button_says_try_again_and_works()
    {
        var viewModel = CreateConnected();

        Assert.Equal("Action_Retry", viewModel.RetryText);
        Assert.False(viewModel.IsRetrying);
        Assert.True(viewModel.RetryCommand.CanExecute(null));
    }

    [Fact]
    public void Pressing_it_says_trying_and_switches_the_button_off_until_there_is_an_answer()
    {
        var viewModel = CreateConnected();
        var asked = 0;
        messenger.Register<RetryRequested>(this, (_, _) => asked++);

        viewModel.RetryCommand.Execute(null);

        Assert.True(viewModel.IsRetrying);
        Assert.Equal("Action_Retrying", viewModel.RetryText);
        Assert.False(viewModel.RetryCommand.CanExecute(null));
        Assert.Equal(1, asked);
    }

    [Fact]
    public void A_failed_attempt_says_it_is_still_unreachable_and_allows_another_try()
    {
        var viewModel = CreateConnected();
        viewModel.RetryCommand.Execute(null);

        messenger.Send(new SensorConnectionFailed(office));

        Assert.False(viewModel.IsRetrying);
        Assert.True(viewModel.RetryFailed);
        Assert.Equal("Action_RetryFailed", viewModel.RetryText);
        Assert.True(viewModel.RetryCommand.CanExecute(null));
    }

    [Fact]
    public void Trying_again_clears_the_earlier_failure()
    {
        var viewModel = CreateConnected();
        messenger.Send(new SensorConnectionFailed(office));

        viewModel.RetryCommand.Execute(null);

        Assert.False(viewModel.RetryFailed);
        Assert.Equal("Action_Retrying", viewModel.RetryText);
    }

    [Fact]
    public void A_connected_sensor_ends_the_trying_and_the_failure()
    {
        var viewModel = CreateConnected();
        viewModel.RetryCommand.Execute(null);
        messenger.Send(new SensorConnectionFailed(office));
        viewModel.RetryCommand.Execute(null);

        messenger.Send(new SensorStatusUpdated(office, new SensorStatus(TimeSpan.FromMinutes(2), 80, -60)));

        Assert.False(viewModel.IsRetrying);
        Assert.False(viewModel.RetryFailed);
        Assert.Equal("Action_Retry", viewModel.RetryText);
    }

    [Fact]
    public void Choosing_another_sensor_forgets_the_retry()
    {
        var viewModel = CreateConnected();
        viewModel.RetryCommand.Execute(null);

        messenger.Send(new DeviceSelected(new DeviceId("test", "B2")));

        Assert.False(viewModel.IsRetrying);
        Assert.Equal("Action_Retry", viewModel.RetryText);
    }

    [Fact]
    public void The_text_follows_the_language()
    {
        var viewModel = CreateConnected();
        var raised = new List<string?>();
        viewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        messenger.Send(new LanguageChanged("uk-UA"));

        Assert.Contains(nameof(ConnectionViewModel.RetryText), raised);
    }
}
