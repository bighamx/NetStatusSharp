using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace ProcessViewer
{
    public static class NetProcessAPI
    {
        private const int AF_INET = 2;

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion, TCP_TABLE_CLASS tblClass, uint reserved = 0);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int dwOutBufLen, bool sort, int ipVersion, UDP_TABLE_CLASS tblClass, uint reserved = 0);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint SetTcpEntry(IntPtr pRow);

        public static TcpRow[] GetAllTcpConnections()
        {
            int buffSize = 0;
            GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);

            IntPtr buffTable = Marshal.AllocHGlobal(buffSize);
            try
            {
                uint ret = GetExtendedTcpTable(buffTable, ref buffSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
                if (ret != 0)
                {
                    return new TcpRow[0];
                }

                TcpTable tab = (TcpTable)Marshal.PtrToStructure(buffTable, typeof(TcpTable));
                IntPtr rowPtr = (IntPtr)(buffTable.ToInt64() + Marshal.SizeOf(typeof(uint)));
                TcpRow[] table = new TcpRow[tab.dwNumEntries];

                for (int i = 0; i < tab.dwNumEntries; i++)
                {
                    table[i] = (TcpRow)Marshal.PtrToStructure(rowPtr, typeof(TcpRow));
                    rowPtr = (IntPtr)(rowPtr.ToInt64() + Marshal.SizeOf(typeof(TcpRow)));
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
            IntPtr buffTable = Marshal.AllocHGlobal(buffSize);
            try
            {
                ret = GetExtendedUdpTable(buffTable, ref buffSize, true, AF_INET, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID);
                if (ret != 0)
                {
                    return new UdpRow[0];
                }

                UdpTable tab = (UdpTable)Marshal.PtrToStructure(buffTable, typeof(UdpTable));
                IntPtr rowPtr = (IntPtr)(buffTable.ToInt64() + Marshal.SizeOf(typeof(uint)));
                UdpRow[] table = new UdpRow[tab.dwNumEntries];

                for (int i = 0; i < tab.dwNumEntries; i++)
                {
                    table[i] = (UdpRow)Marshal.PtrToStructure(rowPtr, typeof(UdpRow));
                    rowPtr = (IntPtr)(rowPtr.ToInt64() + Marshal.SizeOf(typeof(UdpRow)));
                }

                return table;
            }
            finally
            {
                Marshal.FreeHGlobal(buffTable);
            }
        }

        public static void CloseConnByLocalPort(int port)
        {
            TcpRow[] tcpRows = (from row in GetAllTcpConnections()
                                where row.LocalPort == port
                                select row).ToArray();

            for (int i = 0; i < tcpRows.Length; i++)
            {
                tcpRows[i].state = ConnectionState.Delete_TCB;
                IntPtr rowPointer = GetPtrToNewObject(tcpRows[i]);

                try
                {
                    SetTcpEntry(rowPointer);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(rowPointer);
                }
            }
        }

        public static IntPtr GetPtrToNewObject(object obj)
        {
            IntPtr ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(obj));
            Marshal.StructureToPtr(obj, ptr, false);
            return ptr;
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
            get { return new System.Net.IPAddress(localAddr); }
        }

        public ushort LocalPort
        {
            get { return BitConverter.ToUInt16(new[] { localPort[1], localPort[0] }, 0); }
        }

        public System.Net.IPAddress RemoteAddress
        {
            get { return new System.Net.IPAddress(remoteAddr); }
        }

        public ushort RemotePort
        {
            get { return BitConverter.ToUInt16(new[] { remotePort[1], remotePort[0] }, 0); }
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
            get { return new System.Net.IPAddress(localAddr); }
        }

        public ushort LocalPort
        {
            get { return BitConverter.ToUInt16(new[] { localPort[1], localPort[0] }, 0); }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UdpTable
    {
        public uint dwNumEntries;
    }

    #endregion
}
