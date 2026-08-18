using System;
using System.Diagnostics;
using System.IO;

namespace OpenMediaTransport
{
    internal static class OmtStorage
    {
        internal const string StoragePathEnv = "OMT_STORAGE_PATH";

        internal static string StorageDirectory()
        {
            var overridePath = Environment.GetEnvironmentVariable(StoragePathEnv);
            if (!string.IsNullOrEmpty(overridePath))
                return overridePath;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                if (string.IsNullOrEmpty(programData))
                    programData = @"C:\ProgramData";
                return Path.Combine(programData, "OMT");
            }

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home))
                home = Environment.GetEnvironmentVariable("HOME") ?? ".";
            return Path.Combine(home, ".OMT");
        }

        internal static string LogsDirectory()
        {
            return Path.Combine(StorageDirectory(), "logs");
        }

        /// <summary>
        /// libomtnet file name: <c>{MainModule.ModuleName}{pid}.log</c> under <see cref="LogsDirectory"/>.
        /// </summary>
        internal static string DefaultLogFilePath()
        {
            var process = Process.GetCurrentProcess();
            string stem;
            try
            {
                var module = process.MainModule;
                stem = module != null ? module.ModuleName + process.Id : process.Id.ToString();
            }
            catch (Exception)
            {
                stem = process.Id.ToString();
            }

            return Path.Combine(LogsDirectory(), stem + ".log");
        }
    }
}
