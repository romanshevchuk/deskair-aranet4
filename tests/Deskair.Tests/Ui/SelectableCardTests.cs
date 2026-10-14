using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Deskair.App.Ui.Controls;

namespace Deskair.Tests.Ui;

public class SelectableCardTests
{
    [Fact]
    public void Narrator_sees_a_selectable_item_that_follows_IsSelected()
    {
        UiHost.Run(() =>
        {
            var card = UiHost.Prepare(new SelectableCard());
            var peer = UIElementAutomationPeer.CreatePeerForElement(card);
            var item = Assert.IsAssignableFrom<ISelectionItemProvider>(peer.GetPattern(PatternInterface.SelectionItem));

            Assert.False(item.IsSelected);

            card.IsSelected = true;

            Assert.True(item.IsSelected);
        });
    }

    [Fact]
    public void Selecting_through_automation_runs_the_command_once()
    {
        UiHost.Run(() =>
        {
            var runs = 0;
            var card = UiHost.Prepare(new SelectableCard { Command = new CountingCommand(() => runs++) });
            var item = (ISelectionItemProvider)UIElementAutomationPeer.CreatePeerForElement(card).GetPattern(PatternInterface.SelectionItem);

            // A button peer clicks through the dispatcher, so let it run after each request.
            item.Select();
            Drain();
            card.IsSelected = true;
            item.Select();
            Drain();

            Assert.Equal(1, runs);
        });
    }

    [Fact]
    public void A_metric_card_is_a_selectable_card()
    {
        UiHost.Run(() =>
        {
            var card = UiHost.Prepare(new MetricCard { IsSelected = true });
            var peer = UIElementAutomationPeer.CreatePeerForElement(card);

            Assert.True(((ISelectionItemProvider)peer.GetPattern(PatternInterface.SelectionItem)).IsSelected);
        });
    }

    private static void Drain()
        => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

    private sealed class CountingCommand(Action run) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => run();
    }
}
