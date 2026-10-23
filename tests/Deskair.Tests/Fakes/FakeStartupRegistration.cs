using Deskair.Services.Platform;

namespace Deskair.Tests.Fakes;

/// <summary>A startup registration that can be made to fail.</summary>
public sealed class FakeStartupRegistration : IStartupRegistration
{
    private bool isEnabled;

    /// <summary>When true, reading the state throws, like a registry key that cannot be opened.</summary>
    public bool FailRead { get; set; }

    public bool IsEnabled
    {
        get => FailRead ? throw new UnauthorizedAccessException("registry") : isEnabled;
        set => isEnabled = value;
    }

    public bool Fail { get; set; }

    public Task<bool> IsEnabledAsync(CancellationToken cancellationToken) => Task.FromResult(IsEnabled);

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
