using System.Runtime.InteropServices;

namespace Deskair.App.Platform.Notifications;

/// <summary>The COM pieces Windows needs to hand a toast click back to the app: the callback interface it calls and the class factory it asks for the object.</summary>
internal static class ToastComInterop
{
    internal const uint LocalServerContext = 4;
    internal const uint MultipleUse = 1;
    internal const int NoAggregation = unchecked((int)0x80040110);

    [DllImport("ole32.dll")]
    internal static extern int CoRegisterClassObject(ref Guid classId, [MarshalAs(UnmanagedType.IUnknown)] object factory, uint context, uint flags, out uint cookie);

    [DllImport("ole32.dll")]
    internal static extern int CoRevokeClassObject(uint cookie);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct NotificationUserInputData
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string key;

        [MarshalAs(UnmanagedType.LPWStr)]
        internal string value;
    }

    [ComImport]
    [Guid("53E31837-6600-4A81-9395-75CFFE746F94")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface INotificationActivationCallback
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
    internal interface IClassFactory
    {
        [PreserveSig]
        int CreateInstance(nint outer, ref Guid interfaceId, out nint instance);

        [PreserveSig]
        int LockServer([MarshalAs(UnmanagedType.Bool)] bool @lock);
    }
}
