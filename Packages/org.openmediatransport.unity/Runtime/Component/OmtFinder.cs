using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenMediaTransport.Interop;

namespace OpenMediaTransport
{
    public static class OmtFinder
    {
        private static readonly object Gate = new object();
        private static string[] _cached = Array.Empty<string>();
        private static DateTime _nextRefresh = DateTime.MinValue;

        public static IEnumerable<string> sourceNames => EnumerateSourceNames();

        public static IReadOnlyList<string> EnumerateSourceNames()
        {
            lock (Gate)
            {
                if (DateTime.UtcNow >= _nextRefresh)
                {
                    _cached = Query();
                    _nextRefresh = DateTime.UtcNow.AddMilliseconds(500);
                }
                return _cached;
            }
        }

        private static string[] Query()
        {
            try
            {
                var array = OmtNative.omt_discovery_getaddresses(out var count);
                if (array == IntPtr.Zero || count <= 0)
                    return Array.Empty<string>();

                var names = new string[count];
                for (var i = 0; i < count; i++)
                {
                    var item = Marshal.ReadIntPtr(array, i * IntPtr.Size);
                    names[i] = OmtUtf8.FromPtr(item) ?? string.Empty;
                }
                return names;
            }
            catch (DllNotFoundException)
            {
                return Array.Empty<string>();
            }
            catch (EntryPointNotFoundException)
            {
                return Array.Empty<string>();
            }
        }
    }
}
