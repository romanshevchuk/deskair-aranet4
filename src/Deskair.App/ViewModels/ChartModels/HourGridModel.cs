using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.ChartModels;

/// <summary>One square of the History grid: its value in the unit shown (null = no data, drawn as an outline) and its tooltip ("Tue 13 Oct, 16:00–17:00 · 1234 ppm").</summary>
public sealed record HourGridCell(double? Value, string Text);

/// <summary>Whether a row is the best or the worst one of the period (behaviour-rules.md "Aggregation" rule 5).</summary>
public enum HourGridTag
{
    None,
    Best,
    Worst
}

/// <summary>
/// One row of the History grid. <see cref="Cells"/> has one entry per column; a null entry is not drawn (an hour after now). A row with
/// <see cref="CollapsedText"/> stands for a run of days without readings ("No readings · 15 Sep – 5 Oct") and has no cells.
/// </summary>
public sealed record HourGridRow(
    string Label,
    IReadOnlyList<HourGridCell?> Cells,
    string? AverageText = null,
    ZoneShare? Share = null,
    HourGridTag Tag = HourGridTag.None,
    bool LabelAlways = false,
    bool Emphasis = false,
    string? CollapsedText = null)
{
    public bool IsCollapsed => CollapsedText is not null;
}

/// <summary>
/// Everything the History grid draws, already localized (D-0018, design-tokens.md "History grid"). <see cref="Steps"/> are the lower bounds of the
/// heat steps 2 to 6 in the unit shown (800, 1000, 1200, 1400, 2000 ppm) with their labels; values below the first are step 1.
/// </summary>
public sealed record HourGridModel(
    IReadOnlyList<HourGridRow> Rows,
    IReadOnlyList<(int Column, string Text)> ColumnLabels,
    IReadOnlyList<(double Value, string Text)> Steps)
{
    public int Columns { get; init; } = 24;

    /// <summary>The bottom row ("Typical") with each hour's mean over the period; null for none.</summary>
    public HourGridRow? Typical { get; init; }

    /// <summary>The first column of the worst three hours, outlined in the typical row; null for none.</summary>
    public int? WorstStart { get; init; }

    /// <summary>"Worst 17:00–20:00", in the end column of the typical row.</summary>
    public string? WorstText { get; init; }

    /// <summary>The night hours as columns (start inclusive, end exclusive, may wrap past midnight) for the bracket above the grid; null for none.</summary>
    public (int Start, int End)? Night { get; init; }

    /// <summary>"Night · 22:00–07:00".</summary>
    public string? NightText { get; init; }

    public string BestText { get; init; } = string.Empty;

    public string WorstTagText { get; init; } = string.Empty;

    public string GoodText { get; init; } = string.Empty;

    public string ElevatedText { get; init; } = string.Empty;

    public string HighText { get; init; } = string.Empty;

    public string NoDataText { get; init; } = string.Empty;

    /// <summary>"Each square is one hour".</summary>
    public string UnitText { get; init; } = string.Empty;
}
