namespace Deskair.App.ViewModels.Live;

/// <summary>One of the three stats under the hero card ("71 %", "Good in last 24 h"). Text is already localized and formatted.</summary>
public sealed record StatItem(string? Value, string? Label);
