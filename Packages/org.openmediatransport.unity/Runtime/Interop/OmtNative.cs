using System;
using System.Runtime.InteropServices;

namespace OpenMediaTransport.Interop
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OmtLoggingCallback(IntPtr message);

    internal static class OmtNative
    {
        internal const string LibraryName = "libomt";

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr omt_discovery_getaddresses(out int count);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr omt_receive_create(IntPtr address, OmtFrameType frameTypes, OmtPreferredVideoFormat format, OmtReceiveFlags flags);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_receive_destroy(IntPtr instance);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr omt_receive(IntPtr instance, OmtFrameType frameTypes, int timeoutMilliseconds);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int omt_receive_send(IntPtr instance, IntPtr frame);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_receive_settally(IntPtr instance, ref OmtTally tally);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int omt_receive_gettally(IntPtr instance, int timeoutMilliseconds, out OmtTally tally);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_receive_setflags(IntPtr instance, OmtReceiveFlags flags);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_receive_setsuggestedquality(IntPtr instance, OmtQuality quality);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_receive_getsenderinformation(IntPtr instance, IntPtr info);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_receive_getvideostatistics(IntPtr instance, out OmtStatistics stats);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_receive_getaudiostatistics(IntPtr instance, out OmtStatistics stats);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr omt_send_create(IntPtr name, OmtQuality quality);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_send_setsenderinformation(IntPtr instance, IntPtr info);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_send_addconnectionmetadata(IntPtr instance, IntPtr metadata);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_send_clearconnectionmetadata(IntPtr instance);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_send_setredirect(IntPtr instance, IntPtr newAddress);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int omt_send_getaddress(IntPtr instance, IntPtr address, int maxLength);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_send_destroy(IntPtr instance);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int omt_send(IntPtr instance, IntPtr frame);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int omt_send_connections(IntPtr instance);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr omt_send_receive(IntPtr instance, int timeoutMilliseconds);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int omt_send_gettally(IntPtr instance, int timeoutMilliseconds, out OmtTally tally);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_send_getvideostatistics(IntPtr instance, out OmtStatistics stats);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_send_getaudiostatistics(IntPtr instance, out OmtStatistics stats);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_setloggingfilename(IntPtr filename);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_setloggingcallback(OmtLoggingCallback callback);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int omt_settings_get_string(IntPtr name, IntPtr value, int maxLength);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_settings_set_string(IntPtr name, IntPtr value);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int omt_settings_get_integer(IntPtr name);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_settings_set_integer(IntPtr name, int value);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void omt_shutdown();
    }
}
