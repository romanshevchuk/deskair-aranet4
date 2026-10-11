namespace Deskair.Services.Platform;

public interface IStartupRegistration
{
    bool IsEnabled { get; }

    Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken);
}
