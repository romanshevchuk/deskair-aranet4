using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Deskair.App.Ui.Views.History;

public partial class HistoryToolbar : UserControl
{
    public static readonly DependencyProperty ExportCommandProperty = DependencyProperty.Register(
        nameof(ExportCommand),
        typeof(ICommand),
        typeof(HistoryToolbar),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ExportStatusProperty = DependencyProperty.Register(
        nameof(ExportStatus),
        typeof(string),
        typeof(HistoryToolbar),
        new PropertyMetadata(null));

    public HistoryToolbar()
    {
        InitializeComponent();
    }

    /// <summary>The result of the last export ("Saved …" or a failure), shown beside the button. Empty hides it.</summary>
    public string? ExportStatus
    {
        get => (string?)GetValue(ExportStatusProperty);
        set => SetValue(ExportStatusProperty, value);
    }

    /// <summary>What the Export button does. It comes from the screen, because the toolbar's own data context only holds the choices.</summary>
    public ICommand? ExportCommand
    {
        get => (ICommand?)GetValue(ExportCommandProperty);
        set => SetValue(ExportCommandProperty, value);
    }
}
