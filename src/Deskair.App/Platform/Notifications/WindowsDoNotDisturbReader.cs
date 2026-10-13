using System.Runtime.InteropServices;

namespace Deskair.App.Platform.Notifications;

/// <summary>
/// Reads whether Windows Do Not Disturb is on (docs/plans/research-do-not-disturb.md). Windows has no documented way to ask: on Windows 11 build 26200 the documented
/// <c>FocusSessionManager.IsFocusActive</c> and <c>SHQueryUserNotificationState</c> do not change when Do Not Disturb is switched. What does change is the state
/// <c>WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED</c> (a system state number, 0 off, non-zero on), read with the undocumented <c>NtQueryWnfStateData</c>.
/// When the state cannot be read, the answer is null, and the app treats alerts as allowed.
/// </summary>
public sealed class WindowsDoNotDisturbReader : IDoNotDisturbReader
{
    private const ulong ActiveProfileState = 0x0D83063EA3BF1C75;
    private const int Success = 0;

    public bool? Read()
    {
        try
        {
            var name = new WnfStateName { first = (uint)(ActiveProfileState & 0xFFFFFFFF), second = (uint)(ActiveProfileState >> 32) };
            var buffer = new byte[16];
            var size = (uint)buffer.Length;
            var status = NtQueryWnfStateData(ref name, 0, 0, out _, buffer, ref size);

            return status == Success && size > 0 ? buffer.Take((int)size).Any(value => value != 0) : null;
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            return null;
        }
    }

    [DllImport("ntdll.dll")]
    private static extern int NtQueryWnfStateData(ref WnfStateName name, nint typeId, nint scope, out uint changeStamp, byte[] buffer, ref uint size);

    [StructLayout(LayoutKind.Sequential)]
    private struct WnfStateName
    {
        public uint first;
        public uint second;
    }
}
