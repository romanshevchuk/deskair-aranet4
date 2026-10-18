using Deskair.Services.Platform;

namespace Deskair.Tests.Fakes;

public sealed class FakePackageIdentity : IPackageIdentity
{
    public FakePackageIdentity(bool isPackaged)
    {
        IsPackaged = isPackaged;
    }

    public bool IsPackaged { get; }
}
