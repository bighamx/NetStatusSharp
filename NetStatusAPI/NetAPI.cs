using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ProcessViewer
{
    public class NetProcessAPI
    {

        /// <summary>
        /// 获取TCP连接对象
        /// </summary>
        /// <param name="pTcpTable"></param>
        /// <param name="dwOutBufLen"></param>
        /// <param name="sort"></param>
        /// <param name="ipVersion"></param>
        /// <param name="tblClass"></param>
        /// <param name="reserved"></param>
        /// <returns></returns>
        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion, TCP_TABLE_CLASS tblClass, uint reserved = 0);


        /// <summary>
        /// 获取UDP连接对象
        /// </summary>
        /// <param name="pTcpTable"></param>
        /// <param name="dwOutBufLen"></param>
        /// <param name="sort"></param>
        /// <param name="ipVersion"></param>
        /// <param name="tblClass"></param>
        /// <param name="reserved"></param>
        /// <returns></returns>
        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int dwOutBufLen, bool sort, int ipVersion, UDP_TABLE_CLASS tblClass, uint reserved = 0);



        // 针对于TCP管理
        //  由于该部分使用到了Linq查询 需要.Net版本较高（当前使用的是.Net2.0）,再着该部分代码在现有功能点并不需要， 所以注释了（主要是我懒得改了 ^_^）
        /// <summary>
        /// 设置
        /// </summary>
        /// <param name="pRow"></param>
        /// <returns></returns>
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint SetTcpEntry(IntPtr pRow);
        public static void CloseConnByLocalPort(int port)
        {
            var tcpRows = GetAllTcpConnections();
            var tmpRows = from i in tcpRows
                          where i.LocalPort == port
                          select i;
            TcpRow[] tcpArry = tmpRows.ToArray();
            for (int i = 0; i < tcpArry.Length; i++)
            {
                tcpArry[i].state = ConnectionState.Delete_TCB;
                SetTcpEntry(GetPtrToNewObject(tcpArry[i]));
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
    /// <summary>
    /// TCP表类型
    /// </summary>
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
    /// <summary> 
    /// TCP当前状态
    /// </summary> 
    public enum ConnectionState : int
    {
        /// <summary> All </summary> 
        All = 0,
        /// <summary> Closed </summary> 
        Closed = 1,
        /// <summary> Listen </summary> 
        Listen = 2,
        /// <summary> Syn_Sent </summary> 
        Syn_Sent = 3,
        /// <summary> Syn_Rcvd </summary> 
        Syn_Rcvd = 4,
        /// <summary> Established </summary> 
        Established = 5,
        /// <summary> Fin_Wait1 </summary> 
        Fin_Wait1 = 6,
        /// <summary> Fin_Wait2 </summary> 
        Fin_Wait2 = 7,
        /// <summary> Close_Wait </summary> 
        Close_Wait = 8,
        /// <summary> Closing </summary> 
        Closing = 9,
        /// <summary> Last_Ack </summary> 
        Last_Ack = 10,
        /// <summary> Time_Wait </summary> 
        Time_Wait = 11,
        /// <summary> Delete_TCB </summary> 
        Delete_TCB = 12
    }


    /// <summary>
    /// TCP列结构（行）
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TcpRow
    {
        // DWORD is System.UInt32 in C#
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
            get
            {
                return new System.Net.IPAddress(localAddr);
            }
        }
        public ushort LocalPort
        {
            get
            {
                return BitConverter.ToUInt16(
                new byte[2] { localPort[1], localPort[0] }, 0);
            }
        }
        public System.Net.IPAddress RemoteAddress
        {
            get
            {
                return new System.Net.IPAddress(remoteAddr);
            }
        }
        public ushort RemotePort
        {
            get
            {
                return BitConverter.ToUInt16(
                new byte[2] { remotePort[1], remotePort[0] }, 0);
            }
        }
    }

    /// <summary>
    /// TCP表结构
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TcpTable
    {
        public uint dwNumEntries;
        TcpRow table;
    }
    #endregion




    #region UDP结构

    /// <summary>
    /// UDP表类型
    /// </summary>
    public enum UDP_TABLE_CLASS
    {
        UDP_TABLE_BASIC,
        UDP_TABLE_OWNER_PID,
        UDP_TABLE_OWNER_MODULE
    }



    /// <summary>
    /// TCP列结构（行）
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UdpRow
    {
        public uint localAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] localPort;
        public int owningPid;
        public System.Net.IPAddress LocalAddress
        {
            get
            {
                return new System.Net.IPAddress(localAddr);
            }
        }
        public ushort LocalPort
        {
            get
            {
                return BitConverter.ToUInt16(
                new byte[2] { localPort[1], localPort[0] }, 0);
            }
        }
    }


    /// <summary>
    /// UDP表结构
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UdpTable
    {
        public uint dwNumEntries;
        UdpRow table;
    }
    #endregion


}