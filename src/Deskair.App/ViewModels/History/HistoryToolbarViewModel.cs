using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;

namespace Deskair.App.ViewModels.History;

/// <summary>
/// The History choices: metric, range, and which period of the range (‹ and ›, D-0019). The screen supplies how far back ‹ may go
/// and the period's title; changing the range returns to the latest period (behaviour-rules.md "History periods").
/// </summary>
public sealed partial class HistoryToolbarViewModel : ObservableObject
{
    [ObservableProperty]
    private Metric metric;

    [ObservableProperty]
    private Period range;

    /// <summary>How many periods back from the latest; 0 is the latest.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLatest))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPeriodCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPeriodCommand))]
    private int offset;

    /// <summary>How far ‹ may go (the period before the first reading; see <see cref="HistoryPeriod.MaxOffset"/>). Set by the screen.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousPeriodCommand))]
    private int maxOffset;

    /// <summary>The period's title between ‹ and › ("Last 24 hours", "Tue 13 Oct", "8 – 14 Oct 2026"). Set by the screen.</summary>
    [ObservableProperty]
    private string? periodLabel;

    public bool IsLatest => Offset == 0;

    /// <summary>Whether a change of this property asks for different data (the period's title and limit are results, not choices).</summary>
    public static bool IsChoice(string? propertyName)
        => propertyName is nameof(Metric) or nameof(Range) or nameof(Offset);

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void PreviousPeriod() => Offset++;

    private bool CanGoBack() => Offset < MaxOffset;

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void NextPeriod() => Offset--;

    private bool CanGoForward() => Offset > 0;

    /// <summary>Back to the latest period (the empty state's "Go to the latest").</summary>
    [RelayCommand]
    private void Latest() => Offset = 0;

    partial void OnRangeChanged(Period value)
    {
        Offset = 0;
    }
}
