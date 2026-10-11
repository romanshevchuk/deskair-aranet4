using Deskair.Services.Platform;

namespace Deskair.Tests.Fakes;

public sealed class FakeSystemTheme : ISystemTheme
{
    public bool IsDark { get; set; }

    public event EventHandler? Changed;

    public void RaiseChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
