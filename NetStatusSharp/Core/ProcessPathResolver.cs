using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Text;

namespace NetStatusSharp.Core;

internal static class ProcessPathResolver
{
    private const uint ProcessQueryLimitedInformation = 0x1000;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(
        SafeProcessHandle process,
        int flags,
        StringBuilder executableName,
        ref int size);

    public static string? TryGetExecutablePath(int processId)
    {
        using SafeProcessHandle process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (process.IsInvalid)
        {
            return null;
        }

        int capacity = 1024;
        var path = new StringBuilder(capacity);
        return QueryFullProcessImageName(process, 0, path, ref capacity) ? path.ToString() : null;
    }
}
