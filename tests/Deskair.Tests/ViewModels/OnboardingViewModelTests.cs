using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Deskair.App.ViewModels.Onboarding;
using Deskair.Domain.Readings;
using Deskair.Sensors.Simulated;
using Deskair.Services.Devices;
using Deskair.Services.Sensors;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class OnboardingViewModelTests
{
    private readonly FakeBluetoothRadio radio = new();
    private readonly ListLogger<OnboardingViewModel> logger = new();
    private readonly DeviceRegistry registry = new(new FakeDeviceStore());

    private OnboardingViewModel Create(params ISensorProvider[] providers)
    {
        return new OnboardingViewModel(providers, registry, radio, logger);
    }

    private static DiscoveredSensor Sensor(string provider, string address, int signal = -60)
    {
        return new DiscoveredSensor(new DeviceId(provider, address), address, signal);
    }

    [Fact]
    public async Task The_simulated_provider_is_found()
    {
        var viewModel = Create(new SimulatedProvider(new FakeClock(DateTimeOffset.UnixEpoch)));

        await viewModel.ScanAsync(TestContext.Current.CancellationToken);

        var found = Assert.Single(viewModel.Nearby);
        Assert.Equal(SimulatedProvider.ProviderId, found.Id.ProviderId);
        Assert.False(viewModel.BluetoothOff);
    }

    [Fact]
    public async Task Sensors_show_up_as_they_are_found_from_every_provider()
    {
        var first = new ControlledProvider("first");
        var second = new ControlledProvider("second");
        var viewModel = Create(first, second);

        var scan = viewModel.ScanAsync(TestContext.Current.CancellationToken);
        first.Found.Writer.TryWrite(Sensor("first", "A"));
        await Wait.Until(() => viewModel.Nearby.Count == 1);
        second.Found.Writer.TryWrite(Sensor("second", "B"));
        await Wait.Until(() => viewModel.Nearby.Count == 2);
        first.Found.Writer.Complete();
        second.Found.Writer.Complete();
        await scan;

        Assert.Equal(["A", "B"], viewModel.Nearby.Select(sensor => sensor.Id.Address).Order());
    }

    [Fact]
    public async Task Seeing_a_sensor_again_updates_it_instead_of_listing_it_twice()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);

        var scan = viewModel.ScanAsync(TestContext.Current.CancellationToken);
        provider.Found.Writer.TryWrite(Sensor("first", "A", -80));
        provider.Found.Writer.TryWrite(Sensor("first", "A", -50));
        await Wait.Until(() => viewModel.Nearby.Count == 1 && viewModel.Nearby[0].SignalDbm == -50);
        provider.Found.Writer.Complete();
        await scan;

        Assert.Single(viewModel.Nearby);
    }

    [Fact]
    public async Task Bluetooth_off_ends_the_scan_with_the_hint()
    {
        radio.IsOn = false;
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);

        await viewModel.ScanAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.BluetoothOff);
        Assert.Empty(viewModel.Nearby);
        Assert.Equal(0, provider.Scans);
    }

    [Fact]
    public async Task Bluetooth_turned_off_during_the_scan_stops_it()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);

        var scan = viewModel.ScanAsync(TestContext.Current.CancellationToken);
        await Wait.Until(() => provider.Scans == 1);
        radio.IsOn = false;
        await scan.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.True(viewModel.BluetoothOff);
    }

    [Fact]
    public async Task Changing_the_step_stops_the_scan()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);

        var scan = viewModel.ScanAsync(TestContext.Current.CancellationToken);
        await Wait.Until(() => provider.Scans == 1);
        viewModel.StepIndex = 1;
        await scan.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.True(scan.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Cancelling_stops_the_scan()
    {
        var provider = new ControlledProvider("first");
        var viewModel = Create(provider);
        using var cts = new CancellationTokenSource();

        var scan = viewModel.ScanAsync(cts.Token);
        await Wait.Until(() => provider.Scans == 1);
        await cts.CancelAsync();
        await scan.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.True(scan.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task A_failing_provider_is_logged_and_the_others_continue()
    {
        var healthy = new ControlledProvider("healthy");
        var viewModel = Create(new FailingProvider(), healthy);

        var scan = viewModel.ScanAsync(TestContext.Current.CancellationToken);
        healthy.Found.Writer.TryWrite(Sensor("healthy", "A"));
        await Wait.Until(() => viewModel.Nearby.Count == 1);
        healthy.Found.Writer.Complete();
        await scan;

        Assert.Single(logger.Entries, entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning);
    }

    private class ProviderBase : ISensorProvider
    {
        public ProviderBase(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public string DisplayNameKey => "Sensor_Simulated_Name";

        public IReadOnlyList<string> OnboardingStepKeys { get; } = [];

        public IReadOnlyList<IReadingImporter> Importers { get; } = [];

        public virtual IAsyncEnumerable<DiscoveredSensor> ScanAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ISensor Create(DeviceId id) => throw new NotSupportedException();
    }

    private sealed class ControlledProvider : ProviderBase
    {
        private int scans;

        public ControlledProvider(string id)
            : base(id)
        {
        }

        public Channel<DiscoveredSensor> Found { get; } = Channel.CreateUnbounded<DiscoveredSensor>();

        public int Scans => Volatile.Read(ref scans);

        public override async IAsyncEnumerable<DiscoveredSensor> ScanAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref scans);

            await foreach (var sensor in Found.Reader.ReadAllAsync(cancellationToken))
            {
                yield return sensor;
            }
        }
    }

    private sealed class FailingProvider : ProviderBase
    {
        public FailingProvider()
            : base("failing")
        {
        }

        public override async IAsyncEnumerable<DiscoveredSensor> ScanAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new InvalidOperationException("adapter");

#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }
    }
}
