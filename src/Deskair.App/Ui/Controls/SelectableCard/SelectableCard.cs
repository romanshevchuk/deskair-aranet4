using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

/// <summary>
/// A card that is one choice in a group (the Live metric choice: the hero for CO₂ and the three mini cards). Clicking it runs its command;
/// <see cref="IsSelected"/> says whether it is the current choice, and Narrator announces it as a selectable item ("…, selected").
/// The look is the content's own; <see cref="MetricCard"/> builds on this with its full template.
/// </summary>
public class SelectableCard : Button
{
    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected),
        typeof(bool),
        typeof(SelectableCard),
        new PropertyMetadata(default(bool), OnIsSelectedChanged));

    static SelectableCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SelectableCard), new FrameworkPropertyMetadata(typeof(SelectableCard)));
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new SelectableCardAutomationPeer(this);

    private static void OnIsSelectedChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        if (UIElementAutomationPeer.FromElement((SelectableCard)owner) is SelectableCardAutomationPeer peer)
        {
            peer.RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, args.OldValue, args.NewValue);
        }
    }
}

/// <summary>A button that also reports the selection item pattern, so a screen reader says whether the card is the chosen one.</summary>
public sealed class SelectableCardAutomationPeer(SelectableCard owner) : ButtonAutomationPeer(owner), ISelectionItemProvider
{
    private SelectableCard Card => (SelectableCard)Owner;

    public bool IsSelected => Card.IsSelected;

    public IRawElementProviderSimple? SelectionContainer => null;

    public override object GetPattern(PatternInterface patternInterface)
        => patternInterface == PatternInterface.SelectionItem ? this : base.GetPattern(patternInterface);

    public void Select()
    {
        if (!Card.IsSelected)
        {
            ((IInvokeProvider)this).Invoke();
        }
    }

    public void AddToSelection() => Select();

    /// <summary>One card is always chosen; choosing another one is the way to leave this one.</summary>
    public void RemoveFromSelection()
    {
    }
}
