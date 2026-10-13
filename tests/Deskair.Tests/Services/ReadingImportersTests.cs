using Deskair.Domain.Readings;
using Deskair.Services.Import;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Services;

public class ReadingImportersTests
{
    private static readonly FakeReadingImporter phoneExport = new();

    private static FakeSensorProvider Provider(string id, params FakeReadingImporter[] importers)
        => new(id, new FakeSensor(), importers);

    [Fact]
    public void A_device_gets_the_importer_of_its_own_provider()
    {
        var importers = new ReadingImporters([Provider("simulated"), Provider("aranet4", phoneExport)]);

        Assert.Same(phoneExport, importers.For(new DeviceId("aranet4", "AA:01")));
    }

    [Fact]
    public void A_provider_without_importers_gives_none()
    {
        var importers = new ReadingImporters([Provider("simulated"), Provider("aranet4", phoneExport)]);

        Assert.Null(importers.For(new DeviceId("simulated", "sim-1")));
    }

    [Fact]
    public void An_unknown_provider_gives_none()
    {
        var importers = new ReadingImporters([Provider("aranet4", phoneExport)]);

        Assert.Null(importers.For(new DeviceId("other", "AA:01")));
    }
}
