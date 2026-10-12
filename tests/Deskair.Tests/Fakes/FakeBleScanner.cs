using System.Runtime.CompilerServices;
using Deskair.Services.Sensors;

namespace Deskair.Tests.Fakes;

/// <summary>Reports the given advertisements in order, then ends.</summary>
public sealed class FakeBleScanner : IBleScanner
{
    private readonly IReadOnlyList<BleAdvertisement> advertisements;

    public FakeBleScanner(params BleAdvertisement[] advertisements)
    {
        this.advertisements = advertisements;
    }

    public async IAsyncEnumerable<BleAdvertisement> ScanAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var advertisement in advertisements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return advertisement;

            await Task.Yield();
        }
    }
}
