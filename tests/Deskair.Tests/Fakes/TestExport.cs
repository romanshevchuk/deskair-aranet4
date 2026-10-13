using Deskair.App.ViewModels.Data;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Import;
using Deskair.Services.Storage;
using Deskair.Storage.Export;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.Fakes;

/// <summary>Builds an <see cref="ExportViewModel"/> for tests of screens that only need it to exist.</summary>
public static class TestExport
{
    public static ExportViewModel Create(IReadingStore? store = null, DeviceRegistry? registry = null)
    {
        return new ExportViewModel(
            new ExportService(store ?? new FakeReadingStore(), new CsvExporter(), TimeZoneInfo.Utc),
            registry ?? new DeviceRegistry(new FakeDeviceStore()),
            new FakeFileDialogs(),
            new FakeLocalizer(),
            new LocalCalendar(TimeZoneInfo.Utc),
            TimeProvider.System,
            NullLogger<ExportViewModel>.Instance);
    }
}
