using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Services.Devices;
using Deskair.Services.Queries;

namespace Deskair.App.ViewModels.Live;

public sealed partial class ChartCardViewModel : ObservableObject
{
    private readonly ReadingQueryService query;
    private readonly DeviceRegistry devices;

    [ObservableProperty]
    private IReadOnlyList<ChartPoint>? points;

    public ChartCardViewModel(ReadingQueryService query, DeviceRegistry devices)
    {
        this.query = query;
        this.devices = devices;
    }

    public async Task RefreshAsync(Metric metric, LiveRange range, CancellationToken cancellationToken)
    {
        if (devices.Selected is not { } device)
        {
            Points = [];
            return;
        }

        var recent = await query.GetRecentAsync(device, WindowOf(range), cancellationToken);

        Points = ToPoints(recent, metric, device);
    }

    private static TimeSpan WindowOf(LiveRange range)
    {
        return range switch
        {
            LiveRange.OneHour => TimeSpan.FromHours(1),
            LiveRange.SixHours => TimeSpan.FromHours(6),
            _ => TimeSpan.FromHours(24)
        };
    }

    private List<ChartPoint> ToPoints(IReadOnlyList<Reading> recent, Metric metric, DeviceId device)
    {
        var result = new List<ChartPoint>(recent.Count);

        for (var index = 0; index < recent.Count; index++)
        {
            var reading = recent[index];

            if (index > 0)
            {
                var previous = recent[index - 1].Timestamp;
                var interval = devices.IntervalFor(device, reading.Timestamp);

                if (reading.Timestamp - previous > interval * 1.5)
                {
                    result.Add(new ChartPoint(previous + interval, null));
                }
            }

            result.Add(new ChartPoint(reading.Timestamp, reading.ValueOf(metric)));
        }

        return result;
    }
}
