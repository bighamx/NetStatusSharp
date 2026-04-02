using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ProcessViewer
{
    public static class ProcessAPI
    {
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        [DllImport("Shell32.dll")]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            out SHFILEINFO psfi,
            uint cbfileInfo,
            SHGFI uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(UnmanagedType.LPStr, SizeConst = 260)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.LPStr, SizeConst = 80)]
            public string szTypeName;
        }

        [Flags]
        private enum SHGFI
        {
            SmallIcon = 0x00000001,
            LargeIcon = 0x00000000,
            Icon = 0x00000100,
            UseFileAttributes = 0x00000010
        }

        private static readonly object IconCacheLock = new object();
        private static readonly object ProcessNameCacheLock = new object();
        private static readonly Dictionary<string, Icon> IconCache = new Dictionary<string, Icon>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<int, string> ProcessNameCache = new Dictionary<int, string>();

        public static Icon GetIcon(string strPath, bool bSmall)
        {
            if (string.IsNullOrEmpty(strPath))
            {
                return null;
            }

            lock (IconCacheLock)
            {
                Icon cachedIcon;
                if (IconCache.TryGetValue(strPath, out cachedIcon))
                {
                    return cachedIcon;
                }

                Icon icon = TryLoadIcon(strPath, bSmall);
                IconCache[strPath] = icon;
                return icon;
            }
        }

        public static Icon GetIcon(int pid, bool bSmall)
        {
            try
            {
                using (Process process = Process.GetProcessById(pid))
                {
                    return GetIcon(process.MainModule.FileName, bSmall);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetProcessNameByPID(int processID)
        {
            lock (ProcessNameCacheLock)
            {
                string cachedName;
                if (ProcessNameCache.TryGetValue(processID, out cachedName))
                {
                    return cachedName;
                }
            }

            try
            {
                using (Process process = Process.GetProcessById(processID))
                {
                    string processName = process.ProcessName;
                    lock (ProcessNameCacheLock)
                    {
                        ProcessNameCache[processID] = processName;
                    }

                    return processName;
                }
            }
            catch (Exception)
            {
                lock (ProcessNameCacheLock)
                {
                    ProcessNameCache[processID] = "Unknown";
                }

                return "Unknown";
            }
        }

        private static Icon TryLoadIcon(string strPath, bool bSmall)
        {
            SHFILEINFO info;
            SHGFI flags = bSmall
                ? SHGFI.Icon | SHGFI.SmallIcon | SHGFI.UseFileAttributes
                : SHGFI.Icon | SHGFI.LargeIcon | SHGFI.UseFileAttributes;

            IntPtr result = SHGetFileInfo(
                strPath,
                FILE_ATTRIBUTE_NORMAL,
                out info,
                (uint)Marshal.SizeOf(typeof(SHFILEINFO)),
                flags);

            if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                using (Icon sourceIcon = Icon.FromHandle(info.hIcon))
                {
                    return (Icon)sourceIcon.Clone();
                }
            }
            finally
            {
                DestroyIcon(info.hIcon);
            }
        }
    }
}
