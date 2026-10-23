using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Deskair.Services.Sensors;

namespace Deskair.Tests.Fakes;

/// <summary>A scanner the test feeds by hand, one advertisement at a time. It keeps waiting (like a quiet scan) until the test ends it or the listener cancels it.</summary>
public sealed class ControlledBleScanner : IBleScanner
{
    private readonly Channel<BleAdvertisement> channel = Channel.CreateUnbounded<BleAdvertisement>();

    /// <summary>Advertisements sent but not yet taken by the listener.</summary>
    public int Waiting => channel.Reader.Count;

    public void Send(BleAdvertisement advertisement) => channel.Writer.TryWrite(advertisement);

    public void End() => channel.Writer.TryComplete();

    public async IAsyncEnumerable<BleAdvertisement> ScanAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var advertisement in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return advertisement;
        }
    }
}
