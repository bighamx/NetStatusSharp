using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;

namespace ProcessViewer
{
    public static class NetProcessAPI
    {
        private const int AF_INET = 2;
        private const uint ERROR_INSUFFICIENT_BUFFER = 122;

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion, TCP_TABLE_CLASS tblClass, uint reserved = 0);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int dwOutBufLen, bool sort, int ipVersion, UDP_TABLE_CLASS tblClass, uint reserved = 0);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint SetTcpEntry(IntPtr pRow);

        public static TcpRow[] GetAllTcpConnections()
        {
            int buffSize = 0;
            uint firstResult = GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
            EnsureBufferResult(firstResult, buffSize, nameof(GetExtendedTcpTable));

            IntPtr buffTable = Marshal.AllocHGlobal(buffSize);
            try
            {
                uint ret = GetExtendedTcpTable(buffTable, ref buffSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
                if (ret != 0)
                {
                    throw new Win32Exception((int)ret, "读取 TCP 连接表失败。");
                }

                TcpTable tab = Marshal.PtrToStructure<TcpTable>(buffTable);
                IntPtr rowPtr = IntPtr.Add(buffTable, sizeof(uint));
                TcpRow[] table = new TcpRow[tab.dwNumEntries];

                for (int i = 0; i < tab.dwNumEntries; i++)
                {
                    table[i] = Marshal.PtrToStructure<TcpRow>(rowPtr);
                    rowPtr = IntPtr.Add(rowPtr, Marshal.SizeOf<TcpRow>());
                }

                return table;
            }
            finally
            {
                Marshal.FreeHGlobal(buffTable);
            }
        }

        public static UdpRow[] GetAllUdpConnections()
        {
            int buffSize = 0;
            uint ret = GetExtendedUdpTable(IntPtr.Zero, ref buffSize, true, AF_INET, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID);
            EnsureBufferResult(ret, buffSize, nameof(GetExtendedUdpTable));
            IntPtr buffTable = Marshal.AllocHGlobal(buffSize);
            try
            {
                ret = GetExtendedUdpTable(buffTable, ref buffSize, true, AF_INET, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID);
                if (ret != 0)
                {
                    throw new Win32Exception((int)ret, "读取 UDP 连接表失败。");
                }

                UdpTable tab = Marshal.PtrToStructure<UdpTable>(buffTable);
                IntPtr rowPtr = IntPtr.Add(buffTable, sizeof(uint));
                UdpRow[] table = new UdpRow[tab.dwNumEntries];

                for (int i = 0; i < tab.dwNumEntries; i++)
                {
                    table[i] = Marshal.PtrToStructure<UdpRow>(rowPtr);
                    rowPtr = IntPtr.Add(rowPtr, Marshal.SizeOf<UdpRow>());
                }

                return table;
            }
            finally
            {
                Marshal.FreeHGlobal(buffTable);
            }
        }

        private static void EnsureBufferResult(uint result, int bufferSize, string operation)
        {
            if (bufferSize <= 0)
            {
                if (result == 0)
                {
                    return;
                }

                throw new Win32Exception((int)result, operation + " 未返回有效缓冲区大小。");
            }

            if (result != 0 && result != ERROR_INSUFFICIENT_BUFFER)
            {
                throw new Win32Exception((int)result, operation + " 初始化失败。");
            }
        }
    }

    #region TCP返回的数据结构

    public enum TCP_TABLE_CLASS
    {
        TCP_TABLE_BASIC_LISTENER,
        TCP_TABLE_BASIC_CONNECTIONS,
        TCP_TABLE_BASIC_ALL,
        TCP_TABLE_OWNER_PID_LISTENER,
        TCP_TABLE_OWNER_PID_CONNECTIONS,
        TCP_TABLE_OWNER_PID_ALL,
        TCP_TABLE_OWNER_MODULE_LISTENER,
        TCP_TABLE_OWNER_MODULE_CONNECTIONS,
        TCP_TABLE_OWNER_MODULE_ALL
    }

    public enum ConnectionState
    {
        All = 0,
        Closed = 1,
        Listen = 2,
        Syn_Sent = 3,
        Syn_Rcvd = 4,
        Established = 5,
        Fin_Wait1 = 6,
        Fin_Wait2 = 7,
        Close_Wait = 8,
        Closing = 9,
        Last_Ack = 10,
        Time_Wait = 11,
        Delete_TCB = 12
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TcpRow
    {
        public ConnectionState state;
        public uint localAddr;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] localPort;

        public uint remoteAddr;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] remotePort;

        public int owningPid;

        public System.Net.IPAddress LocalAddress
        {
            get { return new IPAddress(localAddr); }
        }

        public ushort LocalPort
        {
            get { return (ushort)((localPort[0] << 8) | localPort[1]); }
        }

        public System.Net.IPAddress RemoteAddress
        {
            get { return new IPAddress(remoteAddr); }
        }

        public ushort RemotePort
        {
            get { return (ushort)((remotePort[0] << 8) | remotePort[1]); }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TcpTable
    {
        public uint dwNumEntries;
    }

    #endregion

    #region UDP结构

    public enum UDP_TABLE_CLASS
    {
        UDP_TABLE_BASIC,
        UDP_TABLE_OWNER_PID,
        UDP_TABLE_OWNER_MODULE
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UdpRow
    {
        public uint localAddr;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] localPort;

        public int owningPid;

        public System.Net.IPAddress LocalAddress
        {
            get { return new IPAddress(localAddr); }
        }

        public ushort LocalPort
        {
            get { return (ushort)((localPort[0] << 8) | localPort[1]); }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UdpTable
    {
        public uint dwNumEntries;
    }

    #endregion
}
