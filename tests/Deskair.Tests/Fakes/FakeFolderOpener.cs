using Deskair.Services.Platform;

namespace Deskair.Tests.Fakes;

public sealed class FakeFolderOpener : IFolderOpener
{
    public List<string> Opened { get; } = [];

    public void Open(string folder)
    {
        Opened.Add(folder);
    }
}
