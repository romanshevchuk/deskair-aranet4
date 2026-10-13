using Deskair.App.ViewModels.Shell;

namespace Deskair.Tests.Fakes;

public sealed class FakeWindowController : IMainWindowController
{
    public List<ShellSection?> Shown { get; } = [];

    public int QuitRequests { get; private set; }

    public void Show(ShellSection? section = null)
    {
        Shown.Add(section);
    }

    public void Quit()
    {
        QuitRequests++;
    }
}
