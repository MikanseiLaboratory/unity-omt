using System;
using System.Runtime.InteropServices;
using OpenMediaTransport.Interop;
using UnityEngine;

namespace OpenMediaTransport
{
    internal static class OmtRuntime
    {
        private static readonly object Gate = new object();
        private static int _references;
        private static OmtLoggingCallback _loggingCallback;
        private static bool _shutdownAvailable = true;
        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            lock (Gate)
            {
                _references = 0;
                _initialized = false;
                _loggingCallback = null;
                OmtNativeLibraries.Reset();
            }
        }

        internal static void AddRef()
        {
            lock (Gate)
            {
                if (_references == 0)
                    Initialize();
                _references++;
            }
        }

        internal static void Release()
        {
            lock (Gate)
            {
                if (_references > 0)
                    _references--;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterQuit()
        {
            Application.quitting -= Shutdown;
            Application.quitting += Shutdown;
        }

        internal static bool IsAvailable
        {
            get
            {
                try
                {
                    OmtNative.omt_discovery_getaddresses(out _);
                    return true;
                }
                catch (DllNotFoundException)
                {
                    return false;
                }
                catch (EntryPointNotFoundException)
                {
                    return false;
                }
            }
        }

        private static void Initialize()
        {
            if (_initialized)
                return;

            OmtNativeLibraries.EnsureLoaded(logFailure: false);

            _loggingCallback = OnNativeLog;
            try
            {
                OmtNative.omt_setloggingcallback(_loggingCallback);
            }
            catch (EntryPointNotFoundException)
            {
            }

            OmtNativeLibraries.EnsureLoaded(logFailure: true);
            Application.runInBackground = true;
            _initialized = true;
        }

        private static void Shutdown()
        {
            _initialized = false;
            _loggingCallback = null;
            if (!_shutdownAvailable)
                return;

            try
            {
                OmtNative.omt_shutdown();
            }
            catch (EntryPointNotFoundException)
            {
                _shutdownAvailable = false;
            }
        }

#if ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(OmtLoggingCallback))]
#endif
        private static void OnNativeLog(IntPtr message)
        {
            var text = OmtUtf8.FromPtr(message);
            if (!string.IsNullOrEmpty(text))
                Debug.Log("[OMT] " + text);
        }
    }

    internal static class OmtNativeUtil
    {
        internal static IntPtr StructureToPtr<T>(T value) where T : struct
        {
            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
            Marshal.StructureToPtr(value, ptr, false);
            return ptr;
        }

        internal static void SendVideo(IntPtr send, ref OmtMediaFrame frame)
        {
            var ptr = StructureToPtr(frame);
            try
            {
                OmtNative.omt_send(send, ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        internal static int ReceiveSend(IntPtr receive, ref OmtMediaFrame frame)
        {
            var ptr = StructureToPtr(frame);
            try
            {
                return OmtNative.omt_receive_send(receive, ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        internal static void WriteSenderInfo(IntPtr destination, string product, string manufacturer, string version)
        {
            if (destination == IntPtr.Zero)
                return;
            var size = OmtSenderInfoNative.MaxStringLength;
            Write(destination, product, size);
            Write(destination + size, manufacturer, size);
            Write(destination + size * 2, version, size);
        }

        private static void Write(IntPtr dest, string value, int max)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(value ?? string.Empty);
            var copy = Math.Min(bytes.Length, max - 1);
            if (copy > 0)
                Marshal.Copy(bytes, 0, dest, copy);
            Marshal.WriteByte(dest, copy, 0);
        }
    }
}
