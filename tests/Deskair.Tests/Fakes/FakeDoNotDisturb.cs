using Deskair.Services.Alerts;

namespace Deskair.Tests.Fakes;

/// <summary>Windows Do Not Disturb as the test says it is.</summary>
public sealed class FakeDoNotDisturb : IDoNotDisturb
{
    public bool IsActive { get; set; }
}
