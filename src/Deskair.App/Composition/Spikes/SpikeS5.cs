using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Deskair.App.Composition.Spikes;

/// <summary>
/// Spike S5 (docs/plans/spike-s5-toasts.md): can an unpackaged desktop app show a toast with Snooze and Open buttons and be told which button was pressed,
/// using only the Windows API that ships with Windows (<c>Windows.UI.Notifications</c>) and its own COM activator? Throwaway: the real code is ALERT-02a.
/// <list type="bullet">
/// <item><c>--spike-s5</c>: registers the app identity and the activator for the current user, shows a toast and waits two minutes for a click.</item>
/// <item><c>--spike-s5 --leave</c>: the same, but the program exits right after showing the toast. Press a button later: Windows starts the program again and it records the click.</item>
/// <item><c>--spike-s5 --cleanup</c>: removes everything the spike registered.</item>
/// </list>
/// Windows adds <c>-Embedding</c> when a click starts the program. Every run appends to <c>artifacts/spike-s5/report.txt</c>, which is where a click that started the
/// program (no console) is recorded.
/// </summary>
public static class SpikeS5
{
    private const string AppUserModelId = "Deskair.SpikeS5";
    private const string DisplayName = "Deskair (toast spike S5)";
    private const string ActivatorClassId = "5C7E6B0A-3A52-4F5C-9E55-3D7C1B2A9F41";
    private const string DefaultOutputFolder = "artifacts/spike-s5";
    private const string ReportFileName = "report.txt";
    private const int AttachParentProcess = -1;
    private const uint LocalServer = 4;
    private const uint MultipleUse = 1;
    private const int NoAggregation = unchecked((int)0x80040110);
    private static readonly TimeSpan waitForClick = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan waitWhenStartedByClick = TimeSpan.FromSeconds(30);

    private const string ToastXml =
        """
        <toast launch="action=open" activationType="foreground">
          <visual>
            <binding template="ToastGeneric">
              <text>CO₂ is elevated · Office</text>
              <text>1,068 ppm. Open a window for about 10 minutes.</text>
            </binding>
          </visual>
          <actions>
            <action content="Snooze 1 h" arguments="action=snooze" activationType="foreground"/>
            <action content="Open Deskair" arguments="action=open" activationType="foreground"/>
          </actions>
        </toast>
        """;

    public static int Run(IReadOnlyList<string> args)
    {
        _ = AttachConsole(AttachParentProcess);

        var folder = Path.GetFullPath(ValueOf(args, "--out") ?? DefaultOutputFolder);
        Directory.CreateDirectory(folder);
        var reportPath = Path.Combine(folder, ReportFileName);

        void Write(string line)
        {
            var stamped = $"{TimeProvider.System.GetLocalNow():HH:mm:ss.fff} [{Environment.ProcessId}] {line}";
            Console.WriteLine(stamped);
            File.AppendAllText(reportPath, stamped + Environment.NewLine, new UTF8Encoding(false));
        }

        var exitCode = 1;

        try
        {
            // On the thread pool: a thread there lives in the multithreaded COM apartment, so Windows can call the activator without a message loop.
            exitCode = Task.Run(() => RunAsync(args, folder, Write)).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            Write($"SPIKE FAILED: {exception.GetType().Name}: {exception.Message}");
            Write(exception.ToString());
        }

        Write($"Exit code {exitCode}. Report: {reportPath}");

        return exitCode;
    }

    private static async Task<int> RunAsync(IReadOnlyList<string> args, string folder, Action<string> write)
    {
        if (Has(args, "--cleanup"))
        {
            Unregister();
            write("Cleanup done: the identity and the activator are removed from the registry (HKCU).");

            return 0;
        }

        var startedByClick = Has(args, "-Embedding");
        var activated = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        write(startedByClick
            ? "Spike S5 started BY WINDOWS because a toast button was pressed while the program was not running."
            : $"Spike S5 - toasts from an unpackaged app. Started {TimeProvider.System.GetLocalNow():yyyy-MM-dd HH:mm:ss zzz}.");
        write($"Windows {Environment.OSVersion.Version}, packaged: {IsPackaged()}, elevated: {IsElevated()}, process: {Environment.ProcessPath}");

        Register(folder);
        write($"Registered for this user: identity '{AppUserModelId}' and activator {{{ActivatorClassId}}} (HKCU\\Software\\Classes).");
        _ = SetCurrentProcessExplicitAppUserModelID(AppUserModelId);

        var classCookie = RegisterActivator(new Activator(arguments =>
        {
            write($"ACTIVATED with arguments '{arguments}'.");
            activated.TrySetResult(arguments);
        }));
        write(classCookie == 0 ? "Could not register the activator class factory." : "Activator class factory registered in this process.");

        try
        {
            if (startedByClick)
            {
                return await WaitAsync(activated.Task, waitWhenStartedByClick, write);
            }

            if (!Show(write))
            {
                return 1;
            }

            if (Has(args, "--leave"))
            {
                write("Toast shown; leaving now. Press a button on it: Windows should start this program again and a line 'ACTIVATED' appears in the report.");

                return 0;
            }

            write($"Toast shown. Press 'Snooze 1 h', 'Open Deskair' or the toast itself within {waitForClick.TotalSeconds:F0} s.");

            return await WaitAsync(activated.Task, waitForClick, write);
        }
        finally
        {
            if (classCookie != 0)
            {
                _ = CoRevokeClassObject(classCookie);
            }
        }
    }

    private static bool Show(Action<string> write)
    {
        var document = new XmlDocument();
        document.LoadXml(ToastXml);

        var notifier = ToastNotificationManager.CreateToastNotifier(AppUserModelId);
        write($"Notification setting for this app: {ReadSetting(notifier)} (Enabled is what we want).");

        var toast = new ToastNotification(document);
        toast.Activated += (_, _) => write("ToastNotification.Activated event fired in this process.");
        toast.Dismissed += (_, e) => write($"ToastNotification.Dismissed event: {e.Reason}.");
        toast.Failed += (_, e) => write($"ToastNotification.Failed event: {e.ErrorCode.Message}");

        notifier.Show(toast);

        return true;
    }

    /// <summary>The setting can be unknown to Windows until the first toast of an identity was shown, in which case reading it fails; that is a finding, not an error.</summary>
    private static string ReadSetting(ToastNotifier notifier)
    {
        try
        {
            return notifier.Setting.ToString();
        }
        catch (COMException exception)
        {
            return $"unknown (0x{exception.HResult:X8} before the first toast)";
        }
    }

    private static async Task<int> WaitAsync(Task<string> activated, TimeSpan timeout, Action<string> write)
    {
        try
        {
            var arguments = await activated.WaitAsync(timeout);

            write(arguments switch
            {
                "action=snooze" => "RESULT: the Snooze button reached the program.",
                "action=open" => "RESULT: Open Deskair (or the toast body) reached the program.",
                _ => $"RESULT: something reached the program: '{arguments}'."
            });

            return 0;
        }
        catch (TimeoutException)
        {
            write($"RESULT: no activation within {timeout.TotalSeconds:F0} s. Either no button was pressed or the click never reached the program.");

            return 1;
        }
    }

    private static void Register(string folder)
    {
        using (var identity = Registry.CurrentUser.CreateSubKey($@"Software\Classes\AppUserModelId\{AppUserModelId}"))
        {
            identity.SetValue("DisplayName", DisplayName);
            identity.SetValue("CustomActivator", $"{{{ActivatorClassId}}}");
        }

        using var server = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{{{ActivatorClassId}}}\LocalServer32");
        server.SetValue(null, $"\"{Environment.ProcessPath}\" --spike-s5 --out \"{folder}\"");
    }

    private static void Unregister()
    {
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\AppUserModelId\{AppUserModelId}", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\CLSID\{{{ActivatorClassId}}}", throwOnMissingSubKey: false);
    }

    private static uint RegisterActivator(Activator activator)
    {
        var classId = Guid.Parse(ActivatorClassId);
        var result = CoRegisterClassObject(ref classId, new ClassFactory(activator), LocalServer, MultipleUse, out var cookie);

        return result == 0 ? cookie : 0;
    }

    private static bool IsPackaged()
    {
        try
        {
            return Windows.ApplicationModel.Package.Current.Id.FullName.Length > 0;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();

        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static bool Has(IReadOnlyList<string> args, string name) => args.Contains(name, StringComparer.OrdinalIgnoreCase);

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

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int processId);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);

    [DllImport("ole32.dll")]
    private static extern int CoRegisterClassObject(ref Guid classId, [MarshalAs(UnmanagedType.IUnknown)] object factory, uint context, uint flags, out uint cookie);

    [DllImport("ole32.dll")]
    private static extern int CoRevokeClassObject(uint cookie);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotificationUserInputData
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string key;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string value;
    }

    [ComImport]
    [Guid("53E31837-6600-4A81-9395-75CFFE746F94")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface INotificationActivationCallback
    {
        void Activate(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [MarshalAs(UnmanagedType.LPWStr)] string invokedArgs,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] NotificationUserInputData[] data,
            [MarshalAs(UnmanagedType.U4)] uint dataCount);
    }

    [ComImport]
    [Guid("00000001-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IClassFactory
    {
        [PreserveSig]
        int CreateInstance(nint outer, ref Guid interfaceId, out nint instance);

        [PreserveSig]
        int LockServer([MarshalAs(UnmanagedType.Bool)] bool @lock);
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(ActivatorClassId)]
    private sealed class Activator : INotificationActivationCallback
    {
        private readonly Action<string> onActivated;

        public Activator(Action<string> onActivated)
        {
            this.onActivated = onActivated;
        }

        public void Activate(string appUserModelId, string invokedArgs, NotificationUserInputData[] data, uint dataCount)
        {
            onActivated(invokedArgs);
        }
    }

    [ComVisible(true)]
    private sealed class ClassFactory : IClassFactory
    {
        private readonly Activator activator;

        public ClassFactory(Activator activator)
        {
            this.activator = activator;
        }

        public int CreateInstance(nint outer, ref Guid interfaceId, out nint instance)
        {
            instance = 0;

            if (outer != 0)
            {
                return NoAggregation;
            }

            var unknown = Marshal.GetIUnknownForObject(activator);

            try
            {
                return Marshal.QueryInterface(unknown, in interfaceId, out instance);
            }
            finally
            {
                _ = Marshal.Release(unknown);
            }
        }

        public int LockServer(bool @lock) => 0;
    }
}
