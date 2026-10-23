namespace Deskair.Services.Platform;

public interface IStartupRegistration
{
    /// <summary>Whether the app starts with Windows. A state that cannot be read counts as not enabled.</summary>
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken);

    Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken);
}
