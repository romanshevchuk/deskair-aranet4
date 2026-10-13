using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Deskair.App.ViewModels.Settings;

/// <summary>
/// A time of day typed as "HH:mm" (24 hour, "9:05" is fine too). <see cref="ValueChanged"/> is raised only for a typed time that reads as a different time;
/// a text that is not a time is flagged and leaves <see cref="Value"/> as it was. <see cref="Load"/> sets the time without raising anything.
/// </summary>
public sealed partial class TimeField : ObservableObject
{
    private static readonly string[] formats = ["H:mm", "HH:mm"];

    private bool loading;

    [ObservableProperty]
    private string text = string.Empty;

    [ObservableProperty]
    private bool isInvalid;

    public TimeOnly Value { get; private set; }

    public event EventHandler<TimeOnly>? ValueChanged;

    public void Load(TimeOnly value)
    {
        loading = true;

        try
        {
            Value = value;
            Text = value.ToString("HH:mm", CultureInfo.InvariantCulture);
            IsInvalid = false;
        }
        finally
        {
            loading = false;
        }
    }

    partial void OnTextChanged(string value)
    {
        if (loading)
        {
            return;
        }

        if (!TimeOnly.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            IsInvalid = true;
            return;
        }

        IsInvalid = false;

        if (time != Value)
        {
            Value = time;
            ValueChanged?.Invoke(this, time);
        }
    }
}
