using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.App.ViewModels.Live;
using Deskair.Domain.Analytics.Trend;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.Flyout;

public sealed partial class FlyoutViewModel : ObservableObject
{
    private readonly AirSummaryBuilder summaries;

    [ObservableProperty]
    private Zone? zone;

    [ObservableProperty]
    private double? co2Ppm;

    [ObservableProperty]
    private string? roomName;

    /// <summary>When the shown reading was taken; the view turns it into "updated 12 s ago". Null when there is no reading.</summary>
    [ObservableProperty]
    private DateTimeOffset? updatedAt;

    [ObservableProperty]
    private TrendDirection trend;

    [ObservableProperty]
    private string? adviceTitle;

    [ObservableProperty]
    private string? adviceText;

    [ObservableProperty]
    private bool showNotConnected;

    public FlyoutViewModel(ILocalizer localizer)
    {
        summaries = new AirSummaryBuilder(localizer);
    }

    public void Update(Reading? reading, IReadOnlyList<Reading> recent, DateTimeOffset now, string? room)
    {
        RoomName = room;

        if (reading is not { } latest)
        {
            Zone = null;
            Co2Ppm = null;
            UpdatedAt = null;
            Trend = TrendDirection.Steady;
            AdviceTitle = null;
            AdviceText = null;
            ShowNotConnected = true;
            return;
        }

        var summary = summaries.Build(latest, recent, now);

        Zone = summary.Zone;
        Co2Ppm = summary.Co2Ppm;
        UpdatedAt = latest.Timestamp;
        Trend = summary.Trend;
        AdviceTitle = summary.AdviceTitle;
        AdviceText = summary.AdviceText;
        ShowNotConnected = false;
    }
}
