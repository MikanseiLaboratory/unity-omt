using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace OpenMediaTransport
{
    /// <summary>
    /// Native AOT libomt P/Invokes libvmx by short name. Unity's plugin folder is not
    /// on that search path, so the codec DLL must be loaded (or its directory added)
    /// before the first encode/decode.
    /// </summary>
    internal static class OmtNativeLibraries
    {
        private static bool _attempted;
        private static bool _loaded;

        internal static void Reset()
        {
            _attempted = false;
        }

        internal static void EnsureLoaded(bool logFailure = true)
        {
            if (_loaded || IsAlreadyLoaded())
            {
                _loaded = true;
                return;
            }

            foreach (var path in CandidatePaths())
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    continue;

                if (TryLoad(path, out var error))
                {
                    _loaded = true;
                    Debug.Log("[OMT] Loaded libvmx: " + path);
                    return;
                }

                Debug.LogWarning("[OMT] Could not load libvmx at " + path + ": " + error);
            }

            if (!logFailure || _attempted)
                return;

            _attempted = true;
            Debug.LogError(
                "[OMT] Unable to load libvmx. Video send and receive cannot start until libvmx sits next to libomt.");
        }

        private static IEnumerable<string> CandidatePaths()
        {
            var fileName = NativeFileName;
            if (string.IsNullOrEmpty(fileName))
                yield break;

            var libOmtDirectory = GetLoadedLibOmtDirectory();
            if (!string.IsNullOrEmpty(libOmtDirectory))
                yield return Path.Combine(libOmtDirectory, fileName);

#if UNITY_EDITOR
            yield return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "Packages",
                "org.openmediatransport.unity",
                "Runtime",
                "Plugins",
                EditorPluginFolder,
                fileName));
#endif

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            yield return Path.Combine(Application.dataPath, "Plugins", "x86_64", fileName);
            yield return Path.Combine(Application.dataPath, "Plugins", fileName);
#endif

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            yield return Path.Combine(Application.dataPath, "Plugins", fileName);
            var dataParent = Path.GetDirectoryName(Application.dataPath);
            var contents = dataParent != null ? Path.GetDirectoryName(dataParent) : null;
            if (!string.IsNullOrEmpty(contents))
                yield return Path.Combine(contents, "PlugIns", fileName);
#endif
        }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private const string NativeFileName = "libvmx.dll";
#if UNITY_EDITOR
        private const string EditorPluginFolder = "Windows/x86_64";
#endif

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandleW(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetModuleFileNameW(IntPtr hModule, StringBuilder lpFilename, int nSize);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryExW(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr AddDllDirectory(string newDirectory);

        private const uint LoadWithAlteredSearchPath = 0x00000008;

        private static bool IsAlreadyLoaded()
        {
            return GetModuleHandleW("libvmx.dll") != IntPtr.Zero
                || GetModuleHandleW("libvmx") != IntPtr.Zero;
        }

        private static string GetLoadedLibOmtDirectory()
        {
            var module = GetModuleHandleW("libomt.dll");
            if (module == IntPtr.Zero)
                module = GetModuleHandleW("libomt");
            if (module == IntPtr.Zero)
                return null;

            var buffer = new StringBuilder(32768);
            var length = GetModuleFileNameW(module, buffer, buffer.Capacity);
            if (length == 0)
                return null;

            return Path.GetDirectoryName(buffer.ToString());
        }

        private static bool TryLoad(string fullPath, out string error)
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                AddDllDirectory(directory);

            var handle = LoadLibraryExW(fullPath, IntPtr.Zero, LoadWithAlteredSearchPath);
            if (handle != IntPtr.Zero)
            {
                error = null;
                return true;
            }

            error = "Win32 error " + Marshal.GetLastWin32Error();
            return false;
        }

#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        private const string NativeFileName = "libvmx.dylib";
#if UNITY_EDITOR
        private const string EditorPluginFolder = "macOS";
#endif

        private const int RtldNow = 2;
        private const int RtldGlobal = 8;
        private static readonly IntPtr RtldDefault = (IntPtr)(-2);

        [StructLayout(LayoutKind.Sequential)]
        private struct DlInfo
        {
            public IntPtr dli_fname;
            public IntPtr dli_fbase;
            public IntPtr dli_sname;
            public IntPtr dli_saddr;
        }

        [DllImport("libSystem", EntryPoint = "dlopen")]
        private static extern IntPtr dlopen(string path, int mode);

        [DllImport("libSystem", EntryPoint = "dlsym")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libSystem", EntryPoint = "dladdr")]
        private static extern int dladdr(IntPtr addr, out DlInfo info);

        [DllImport("libSystem", EntryPoint = "dlerror")]
        private static extern IntPtr dlerror();

        private static bool IsAlreadyLoaded()
        {
            return dlsym(RtldDefault, "VMX_Create") != IntPtr.Zero;
        }

        private static string GetLoadedLibOmtDirectory()
        {
            var symbol = dlsym(RtldDefault, "omt_setloggingcallback");
            if (symbol == IntPtr.Zero)
                return null;
            if (dladdr(symbol, out var info) == 0 || info.dli_fname == IntPtr.Zero)
                return null;

            var path = Marshal.PtrToStringAnsi(info.dli_fname);
            return string.IsNullOrEmpty(path) ? null : Path.GetDirectoryName(path);
        }

        private static bool TryLoad(string fullPath, out string error)
        {
            dlerror();
            var handle = dlopen(fullPath, RtldNow | RtldGlobal);
            if (handle != IntPtr.Zero)
            {
                error = null;
                return true;
            }

            var message = Marshal.PtrToStringAnsi(dlerror());
            error = string.IsNullOrEmpty(message) ? "dlopen failed" : message;
            return false;
        }

#else
        private const string NativeFileName = null;
#if UNITY_EDITOR
        private const string EditorPluginFolder = "";
#endif

        private static bool IsAlreadyLoaded() => false;

        private static string GetLoadedLibOmtDirectory() => null;

        private static bool TryLoad(string fullPath, out string error)
        {
            error = "libvmx preload is not implemented on this platform";
            return false;
        }
#endif
    }
}
