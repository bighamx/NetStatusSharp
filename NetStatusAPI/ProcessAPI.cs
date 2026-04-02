using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace ProcessViewer
{

    /// <summary>
    /// 通过API获取进程图标
    /// </summary>
    public class ProcessAPI
    {
        [DllImport("Shell32.dll")]
        private static extern int SHGetFileInfo
        (
        string pszPath,
        uint dwFileAttributes,
        out SHFILEINFO psfi,
        uint cbfileInfo,
        SHGFI uFlags
        );

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public SHFILEINFO(bool b)
            {
                hIcon = IntPtr.Zero; iIcon = 0; dwAttributes = 0; szDisplayName = ""; szTypeName = "";
            }
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.LPStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.LPStr, SizeConst = 80)]
            public string szTypeName;
        };

        private enum SHGFI
        {
            SmallIcon = 0x00000001,
            LargeIcon = 0x00000000,
            Icon = 0x00000100,
            DisplayName = 0x00000200,
            Typename = 0x00000400,
            SysIconIndex = 0x00004000,
            UseFileAttributes = 0x00000010
        }
        //获取进程图标
        private static Dictionary<string, Icon> Cache = new Dictionary<string, Icon>();
        public static Icon GetIcon(string strPath, bool bSmall)
        {
            if (Cache.ContainsKey(strPath))
            {
                return Cache[strPath];
            }
            try
            {
                SHFILEINFO info = new SHFILEINFO(true);
                int cbFileInfo = Marshal.SizeOf(info);
                SHGFI flags;
                if (bSmall)
                    flags = SHGFI.Icon | SHGFI.SmallIcon | SHGFI.UseFileAttributes;
                else
                    flags = SHGFI.Icon | SHGFI.LargeIcon | SHGFI.UseFileAttributes;

                SHGetFileInfo(strPath, 256, out info, (uint)cbFileInfo, flags);
                var ico = Icon.FromHandle(info.hIcon);
                Cache.Add(strPath, ico);
                return ico;
            }
            catch (Exception)
            {
                Cache.Add(strPath, null);
                return null;
            }

        }


        //获取进程图标
        //private static Dictionary<int, Icon> Cache2 = new Dictionary<int, Icon>();
        public static Icon GetIcon(int pid, bool bSmall)
        {

            try
            {
                var p = System.Diagnostics.Process.GetProcessById(pid);
                var ico = GetIcon(p.MainModule.FileName, bSmall);

                return ico;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        //获取进程名称
        private static Dictionary<int, string> ProcessNameCache = new Dictionary<int, string>();

        public static string GetProcessNameByPID(int processID)
        {
            // 检查缓存中是否已有该进程名称
            if (ProcessNameCache.ContainsKey(processID))
            {
                return ProcessNameCache[processID];
            }

            try
            {
                // 获取进程名称并添加到缓存
                Process p = Process.GetProcessById(processID);
                string processName = p.ProcessName;
                ProcessNameCache[processID] = processName;
                return processName;
            }
            catch (Exception ex)
            {
                // 如果获取失败，缓存 "Unknown" 并返回
                ProcessNameCache[processID] = "Unknown";
                return "Unknown";
            }
        }
    }
}