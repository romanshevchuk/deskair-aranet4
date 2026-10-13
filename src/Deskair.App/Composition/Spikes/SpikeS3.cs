using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;

namespace Deskair.App.Composition.Spikes;

/// <summary>
/// Spike S3 (docs/plans/spike-s3-title-bar.md): which title bar keeps snap layouts and Mica? Opens one window with a 52 px header like the prototype's, in one of three ways:
/// <c>--spike-s3 --variant native</c> (the system title bar, the header below it), <c>--variant chrome</c> (WindowChrome with a 52 px caption and the system's own caption buttons)
/// or <c>--variant custom</c> (WindowChrome with our own buttons, and the maximize button reported to Windows as HTMAXBUTTON). Each one asks Windows for Mica.
/// A second after it opens, the window asks itself what Windows would do at a few points (the maximize button, the empty header, a tab, the edge) and writes the answers to
/// <c>artifacts/spike-s3/report.txt</c>; snap layouts need the answer HTMAXBUTTON over the maximize button. Throwaway: the real code is WIN-01a.
/// </summary>
public static class SpikeS3
{
    private const string OutputFolder = "artifacts/spike-s3";
    private const string ReportFileName = "report.txt";
    private const int AttachParentProcess = -1;
    private const double HeaderHeight = 52;
    private const int WmNcHitTest = 0x0084;
    private const int WmNcLeftButtonUp = 0x00A2;
    private const int HitClient = 1;
    private const int HitCaption = 2;
    private const int HitMinButton = 8;
    private const int HitMaxButton = 9;
    private const int HitClose = 20;
    private const int FirstBuildWithSystemBackdrop = 22621;
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmSystemBackdropType = 38;
    private const int DwmCaptionButtonBounds = 5;
    private const int BackdropMainWindow = 2;

    public static int Run(IReadOnlyList<string> args)
    {
        _ = AttachConsole(AttachParentProcess);

        Directory.CreateDirectory(OutputFolder);
        var reportPath = Path.GetFullPath(Path.Combine(OutputFolder, ReportFileName));

        void Write(string line)
        {
            var stamped = $"{TimeProvider.System.GetLocalNow():HH:mm:ss.fff} {line}";
            Console.WriteLine(stamped);
            File.AppendAllText(reportPath, stamped + Environment.NewLine, new UTF8Encoding(false));
        }

        var variant = ValueOf(args, "--variant")?.ToLowerInvariant() ?? "chrome";

        if (variant is not ("native" or "chrome" or "custom"))
        {
            Write($"Unknown variant '{variant}'. Use native, chrome or custom.");

            return 2;
        }

        Write($"Spike S3 - variant '{variant}'. Windows {Environment.OSVersion.Version}.");

        var application = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        var window = new SpikeWindow(variant, Write);

        return application.Run(window);
    }

    private static string? ValueOf(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static string HitName(int code) => code switch
    {
        HitClient => "HTCLIENT",
        HitCaption => "HTCAPTION",
        HitMinButton => "HTMINBUTTON",
        HitMaxButton => "HTMAXBUTTON",
        HitClose => "HTCLOSE",
        10 => "HTLEFT",
        11 => "HTRIGHT",
        12 => "HTTOP",
        13 => "HTTOPLEFT",
        14 => "HTTOPRIGHT",
        15 => "HTBOTTOM",
        16 => "HTBOTTOMLEFT",
        17 => "HTBOTTOMRIGHT",
        0 => "HTNOWHERE",
        _ => $"code {code}"
    };

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int processId);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint window, int attribute, out NativeRect value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(nint window, ref Margins margins);

    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint window, out NativeRect rectangle);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int left;
        public int right;
        public int top;
        public int bottom;
    }

    private sealed class SpikeWindow : Window
    {
        private readonly string variant;
        private readonly Action<string> write;
        private readonly TextBlock maximizeButton = new() { Text = "", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        private readonly Border maximizeHost = new() { Width = 46, Background = Brushes.Transparent };
        private readonly TextBlock tab = new() { Text = "History", Margin = new Thickness(14, 0, 14, 0), VerticalAlignment = VerticalAlignment.Center };

        public SpikeWindow(string variant, Action<string> write)
        {
            this.variant = variant;
            this.write = write;

            Title = $"Deskair spike S3: {variant}";
            Width = 1100;
            Height = 640;
            MinWidth = 500;
            Background = Brushes.Transparent;
            Content = Build();

            if (variant != "native")
            {
                WindowChrome.SetWindowChrome(this, new WindowChrome
                {
                    CaptionHeight = HeaderHeight,
                    ResizeBorderThickness = new Thickness(6),
                    GlassFrameThickness = new Thickness(0, 1, 0, 0),
                    UseAeroCaptionButtons = variant == "chrome",
                    CornerRadius = new CornerRadius(0)
                });
            }

            SourceInitialized += OnSourceInitialized;
            ContentRendered += (_, _) => StartProbe();
        }

        private DockPanel Build()
        {
            var header = new Grid { Height = HeaderHeight };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.Children.Add(new TextBlock { Text = "◉  Deskair", Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 16, FontWeight = FontWeights.SemiBold });

            var tabs = new StackPanel { Orientation = Orientation.Horizontal };
            tabs.Children.Add(new TextBlock { Text = "Live", Margin = new Thickness(14, 0, 14, 0), VerticalAlignment = VerticalAlignment.Center });
            tabs.Children.Add(tab);
            tabs.Children.Add(new TextBlock { Text = "Settings", Margin = new Thickness(14, 0, 14, 0), VerticalAlignment = VerticalAlignment.Center });
            WindowChrome.SetIsHitTestVisibleInChrome(tabs, true);
            Grid.SetColumn(tabs, 1);
            header.Children.Add(tabs);

            var right = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var chip = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center, Background = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)), Child = new TextBlock { Text = "Office  ⌄" } };
            WindowChrome.SetIsHitTestVisibleInChrome(chip, true);
            right.Children.Add(chip);

            if (variant == "custom")
            {
                right.Children.Add(CaptionButton("", () => WindowState = WindowState.Minimized));
                maximizeHost.Child = maximizeButton;
                right.Children.Add(maximizeHost);
                right.Children.Add(CaptionButton("", Close));
            }
            else if (variant == "chrome")
            {
                // The system draws its own buttons at the right edge; leave room for them.
                right.Children.Add(new Border { Width = 3 * 46 });
            }

            Grid.SetColumn(right, 2);
            header.Children.Add(right);

            var body = new Border
            {
                Margin = new Thickness(16),
                Padding = new Thickness(20),
                CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)),
                Child = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 14, Text = $"Variant: {variant}. Look at: is the window background see-through (Mica) and tinted by your wallpaper? Are there minimize, maximize and close buttons at the top right, and do they look native? Hover the maximize button for about a second: does the snap layouts box appear? Drag the empty part of the header: does the window move? Double-click it: does it maximize? Click Live, History, Settings and the Office chip: do they react (they are not draggable)?" }
            };

            var root = new DockPanel();
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);
            root.Children.Add(body);

            return root;
        }

        private static Border CaptionButton(string glyph, Action click)
        {
            var host = new Border { Width = 46, Background = Brushes.Transparent, Child = new TextBlock { Text = glyph, FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
            host.MouseLeftButtonUp += (_, _) => click();
            WindowChrome.SetIsHitTestVisibleInChrome(host, true);

            return host;
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            var source = HwndSource.FromHwnd(handle);
            source.CompositionTarget.BackgroundColor = Colors.Transparent;

            var dark = 0;
            _ = DwmSetWindowAttribute(handle, DwmUseImmersiveDarkMode, ref dark, sizeof(int));

            var margins = new Margins { left = -1, right = -1, top = -1, bottom = -1 };
            var extend = DwmExtendFrameIntoClientArea(handle, ref margins);
            write($"DwmExtendFrameIntoClientArea: 0x{extend:X8}.");

            if (Environment.OSVersion.Version.Build >= FirstBuildWithSystemBackdrop)
            {
                var backdrop = BackdropMainWindow;
                var result = DwmSetWindowAttribute(handle, DwmSystemBackdropType, ref backdrop, sizeof(int));
                write($"Mica (system backdrop 'main window') requested: 0x{result:X8} ({(result == 0 ? "ok" : "failed")}).");
            }
            else
            {
                write($"Mica not requested: build {Environment.OSVersion.Version.Build} is older than {FirstBuildWithSystemBackdrop}. A solid colour is the fallback.");
                Background = Brushes.WhiteSmoke;
            }

            if (variant == "custom")
            {
                source.AddHook(WndProc);
            }
        }

        private nint WndProc(nint window, int message, nint wParam, nint lParam, ref bool handled)
        {
            if (message == WmNcHitTest && IsOverMaximizeButton(lParam))
            {
                handled = true;

                return HitMaxButton;
            }

            if (message == WmNcLeftButtonUp && wParam == HitMaxButton)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                handled = true;
            }

            return 0;
        }

        private bool IsOverMaximizeButton(nint lParam)
        {
            var x = (short)(lParam.ToInt64() & 0xFFFF);
            var y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
            var topLeft = maximizeHost.PointToScreen(new Point(0, 0));
            var scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
            var width = maximizeHost.ActualWidth * scale;
            var height = maximizeHost.ActualHeight * scale;

            return x >= topLeft.X && x < topLeft.X + width && y >= topLeft.Y && y < topLeft.Y + height;
        }

        private void StartProbe()
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.2) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Probe();
            };
            timer.Start();
        }

        /// <summary>Asks the window what Windows would do at a few points, the way the mouse would, and writes the answers.</summary>
        private void Probe()
        {
            var handle = new WindowInteropHelper(this).Handle;
            _ = GetWindowRect(handle, out var bounds);
            var scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
            var points = new List<(string Name, int X, int Y)>();

            if (variant == "custom")
            {
                var center = maximizeHost.PointToScreen(new Point(maximizeHost.ActualWidth / 2, maximizeHost.ActualHeight / 2));
                points.Add(("centre of our maximize button", (int)center.X, (int)center.Y));
            }
            else if (DwmGetWindowAttribute(handle, DwmCaptionButtonBounds, out var buttons, Marshal.SizeOf<NativeRect>()) == 0)
            {
                // The three system buttons sit side by side: minimize, maximize, close. The middle third is the maximize button.
                var width = buttons.right - buttons.left;
                points.Add(("centre of the system maximize button", bounds.left + buttons.left + (width / 2), bounds.top + buttons.top + ((buttons.bottom - buttons.top) / 2)));
            }
            else
            {
                write("The system caption button bounds are not available (no system buttons are drawn).");
            }

            var headerY = bounds.top + (int)((variant == "native" ? 8 : HeaderHeight / 2) * scale);
            points.Add(("empty part of the header", bounds.left + (int)(Width * 0.25 * scale), headerY));
            points.Add(("left edge", bounds.left, bounds.top + (int)(300 * scale)));
            points.Add(("top left corner", bounds.left, bounds.top));

            var tabCentre = tab.PointToScreen(new Point(tab.ActualWidth / 2, tab.ActualHeight / 2));
            points.Add(("the History tab", (int)tabCentre.X, (int)tabCentre.Y));

            write($"Window at {bounds.left},{bounds.top} size {bounds.right - bounds.left}x{bounds.bottom - bounds.top} px, scale {scale:F2}.");

            foreach (var (name, x, y) in points)
            {
                var packed = (nint)(((y & 0xFFFF) << 16) | (x & 0xFFFF));
                var answer = (int)SendMessage(handle, WmNcHitTest, 0, packed);
                write($"  hit test at {name} ({x},{y}): {HitName(answer)}");
            }

            write("Now look at the window and try the things it lists; write down what you see.");
        }
    }
}
