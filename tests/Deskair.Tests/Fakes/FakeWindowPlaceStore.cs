using Deskair.Services.Settings;

namespace Deskair.Tests.Fakes;

/// <summary>An in-memory window place store that remembers what was saved.</summary>
public sealed class FakeWindowPlaceStore : IWindowPlaceStore
{
    public WindowPlace? Stored { get; set; }

    public List<WindowPlace> Saved { get; } = [];

    public Task<WindowPlace?> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(Stored);

    public Task SaveAsync(WindowPlace place, CancellationToken cancellationToken)
    {
        Saved.Add(place);
        Stored = place;

        return Task.CompletedTask;
    }
}
