using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetStatusSharp.Core;

internal static class ProcessIconCache
{
    private const uint ShgfiIcon = 0x00000100;
    private const uint ShgfiSmallIcon = 0x00000001;
    private static readonly ConcurrentDictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShellFileInfo
    {
        public IntPtr IconHandle;
        public int IconIndex;
        public uint Attributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string TypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string path,
        uint fileAttributes,
        out ShellFileInfo fileInfo,
        uint fileInfoSize,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr iconHandle);

    public static ImageSource? Get(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        if (Cache.TryGetValue(executablePath, out ImageSource? cached))
        {
            return cached;
        }

        ImageSource? loaded = Load(executablePath);
        if (loaded is not null)
        {
            Cache.TryAdd(executablePath, loaded);
        }

        return loaded;
    }

    private static ImageSource? Load(string path)
    {
        IntPtr result = SHGetFileInfo(
            path,
            0,
            out ShellFileInfo fileInfo,
            (uint)Marshal.SizeOf<ShellFileInfo>(),
            ShgfiIcon | ShgfiSmallIcon);

        if (result == IntPtr.Zero || fileInfo.IconHandle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            BitmapSource source = Imaging.CreateBitmapSourceFromHIcon(
                fileInfo.IconHandle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DestroyIcon(fileInfo.IconHandle);
        }
    }
}
