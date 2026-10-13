using Deskair.Services.Settings;

namespace Deskair.Tests.Fakes;

/// <summary>An in-memory settings store that remembers what was saved.</summary>
public sealed class FakeSettingsStore : ISettingsStore
{
    public AppSettings Stored { get; set; } = AppSettings.Default;

    public List<AppSettings> Saved { get; } = [];

    /// <summary>When set, saving throws it instead of storing.</summary>
    public Exception? FailSaves { get; set; }

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(Stored);

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        if (FailSaves is not null)
        {
            throw FailSaves;
        }

        Saved.Add(settings);
        Stored = settings;

        return Task.CompletedTask;
    }
}
