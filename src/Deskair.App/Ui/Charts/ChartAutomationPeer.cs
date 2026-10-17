using System.Windows.Automation;
using System.Windows.Automation.Peers;

namespace Deskair.App.Ui.Charts;

/// <summary>
/// Makes a chart visible to Narrator as an image with the name set through <c>AutomationProperties.Name</c> ("CO₂, last hour: low…"). A plain
/// <see cref="System.Windows.FrameworkElement"/> has no peer, so without this the name would never be read. A chart without a name stays hidden.
/// </summary>
public sealed class ChartAutomationPeer(ChartBase owner) : FrameworkElementAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;

    protected override string GetClassNameCore() => nameof(ChartBase);

    protected override bool IsControlElementCore() => !string.IsNullOrEmpty(AutomationProperties.GetName(owner));

    protected override bool IsContentElementCore() => IsControlElementCore();
}
