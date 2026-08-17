using System;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenMediaTransport.Interop
{
    internal static class OmtUtf8
    {
        internal static IntPtr Alloc(string value)
        {
            if (value == null)
                return IntPtr.Zero;

            var bytes = Encoding.UTF8.GetBytes(value);
            var ptr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            Marshal.WriteByte(ptr, bytes.Length, 0);
            return ptr;
        }

        internal static void Free(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
                Marshal.FreeHGlobal(ptr);
        }

        internal static string FromPtr(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return null;
            return Marshal.PtrToStringUTF8(ptr);
        }

        internal static string FromPtr(IntPtr ptr, int byteLengthIncludingNull)
        {
            if (ptr == IntPtr.Zero || byteLengthIncludingNull <= 0)
                return null;

            var length = byteLengthIncludingNull;
            if (Marshal.ReadByte(ptr, Math.Max(0, byteLengthIncludingNull - 1)) == 0)
                length = byteLengthIncludingNull - 1;
            if (length <= 0)
                return string.Empty;

            var bytes = new byte[length];
            Marshal.Copy(ptr, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }

        internal static string FromFixedBuffer(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
                return string.Empty;

            var length = Array.IndexOf(buffer, (byte)0);
            if (length < 0)
                length = buffer.Length;
            return Encoding.UTF8.GetString(buffer, 0, length);
        }

        internal static void WriteFixedBuffer(byte[] buffer, string value)
        {
            if (buffer == null)
                return;
            Array.Clear(buffer, 0, buffer.Length);
            if (string.IsNullOrEmpty(value))
                return;

            var bytes = Encoding.UTF8.GetBytes(value);
            var copy = Math.Min(bytes.Length, buffer.Length - 1);
            Buffer.BlockCopy(bytes, 0, buffer, 0, copy);
        }
    }
}
