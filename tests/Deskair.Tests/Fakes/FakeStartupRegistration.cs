using Deskair.Services.Platform;

namespace Deskair.Tests.Fakes;

/// <summary>A startup registration that can be made to fail.</summary>
public sealed class FakeStartupRegistration : IStartupRegistration
{
    public bool IsEnabled { get; set; }

    public bool Fail { get; set; }

    public int Calls { get; private set; }

    public Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken)
    {
        Calls++;

        if (Fail)
        {
            throw new UnauthorizedAccessException("registry");
        }

        IsEnabled = enabled;

        return Task.CompletedTask;
    }
}
