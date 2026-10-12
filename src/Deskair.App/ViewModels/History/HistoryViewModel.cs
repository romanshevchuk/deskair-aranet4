using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Queries;

namespace Deskair.App.ViewModels.History;

public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly HistoryToolbarViewModel toolbar;
    private readonly StatsViewModel stats;
    private readonly TypicalDayViewModel typicalDay;
    private readonly ReadingQueryService query;
    private readonly DeviceRegistry devices;
    private readonly LocalCalendar calendar;
    private readonly TimeProvider timeProvider;
    private CancellationTokenSource? current;

    [ObservableProperty]
    private IReadOnlyList<ChartBar>? bars;

    [ObservableProperty]
    private IReadOnlyList<ChartPoint>? points;

    [ObservableProperty]
    private IReadOnlyList<ChartGap>? gaps;

    public HistoryViewModel(
        HistoryToolbarViewModel toolbar,
        StatsViewModel stats,
        TypicalDayViewModel typicalDay,
        ReadingQueryService query,
        DeviceRegistry devices,
        LocalCalendar calendar,
        TimeProvider timeProvider)
    {
        this.toolbar = toolbar;
        this.stats = stats;
        this.typicalDay = typicalDay;
        this.query = query;
        this.devices = devices;
        this.calendar = calendar;
        this.timeProvider = timeProvider;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        current?.Cancel();
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        current = source;

        try
        {
            await LoadAsync(source.Token);
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // A newer refresh replaced this one, so its results are no longer wanted.
        }
        finally
        {
            if (ReferenceEquals(current, source))
            {
                current = null;
            }
        }
    }

    private async Task LoadAsync(CancellationToken token)
    {
        if (devices.Selected is not { } device)
        {
            Clear();
            return;
        }

        var range = toolbar.Range;
        var end = timeProvider.GetUtcNow();
        var start = StartOf(range, end);
        var metric = toolbar.Metric;
        var grouping = range == Period.Week ? Period.Day : toolbar.Grouping;

        var buckets = await Task.Run(
            () => query.GetBucketsAsync(device, metric, start, end, grouping, toolbar.Hours, token),
            token);
        var hours = await Task.Run(() => query.GetTypicalDayAsync(device, range, start, end, token), token);
        var recent = range == Period.Day ? await query.GetRecentAsync(device, end - start, token) : null;

        token.ThrowIfCancellationRequested();

        stats.Update(buckets);
        typicalDay.Update(hours);

        if (recent is null)
        {
            Bars = buckets.Select(ToBar).ToList();
            Points = null;
            Gaps = EmptyRuns(buckets);
            return;
        }

        var interval = devices.IntervalFor(device, end);
        Bars = null;
        Points = recent.Select(reading => new ChartPoint(reading.Timestamp, reading.ValueOf(metric))).ToList();
        Gaps = MissingStretches(recent.Select(reading => reading.Timestamp).ToList(), interval);
    }

    private DateTimeOffset StartOf(Period range, DateTimeOffset end)
    {
        if (range == Period.Day)
        {
            return end - TimeSpan.FromHours(24);
        }

        var days = range switch
        {
            Period.Week => 7,
            Period.Month => 30,
            _ => 365
        };

        return calendar.StartOfLocalDay(calendar.LocalDateOf(end).AddDays(1 - days));
    }

    private void Clear()
    {
        Bars = [];
        Points = [];
        Gaps = [];
        stats.Update([]);
        typicalDay.Update([]);
    }

    private static ChartBar ToBar(Bucket bucket)
        => new(bucket.Start, bucket.End, bucket.Minimum, bucket.Maximum, bucket.Average, bucket.State);

    private static List<ChartGap> EmptyRuns(IReadOnlyList<Bucket> buckets)
    {
        var runs = new List<ChartGap>();

        foreach (var bucket in buckets.Where(bucket => bucket.State == BucketState.NoData))
        {
            if (runs.Count > 0 && runs[^1].End == bucket.Start)
            {
                runs[^1] = runs[^1] with { End = bucket.End };
            }
            else
            {
                runs.Add(new ChartGap(bucket.Start, bucket.End));
            }
        }

        return runs;
    }

    private static List<ChartGap> MissingStretches(List<DateTimeOffset> times, TimeSpan interval)
    {
        var runs = new List<ChartGap>();

        for (var index = 1; index < times.Count; index++)
        {
            if (times[index] - times[index - 1] > interval * 1.5)
            {
                runs.Add(new ChartGap(times[index - 1] + interval, times[index]));
            }
        }

        return runs;
    }
}
