using ProcessViewer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetStatusSharp
{
    public partial class NetStatus : Form
    {
        public NetStatus()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            this.dataGridView1.Rows.Clear();
            var process = this.textBox1.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var pids = this.textBox4.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => Convert.ToInt32(x)).ToList();
            var lportds = this.textBox2.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => Convert.ToUInt16(x)).ToList();
            var rports = this.textBox3.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => Convert.ToUInt16(x)).ToList();
            bool? isTcp = this.comboBox1.SelectedIndex <= 0 ? default(bool?) : (this.comboBox1.SelectedIndex == 1 ? true : false);
            int? state = this.comboBox2.SelectedIndex <= 0 ? default(int?) : this.comboBox2.SelectedIndex;
            new Thread(() =>
            {
                button1_Click_thread(process, pids, lportds, rports, isTcp, state);
            }).Start();
        }

        private void button1_Click_thread(List<string> process, List<int> pids, List<ushort> lports, List<ushort> rports, bool? isTcp, int? state)
        {
            if (isTcp == null || isTcp.Value)
            {
                TcpRow[] array = NetProcessAPI.GetAllTcpConnections();
                array = (from x in array
                         where !pids.Any<int>() || pids.Contains(x.owningPid)
                         where !lports.Any<ushort>() || lports.Contains(x.LocalPort)
                         where !rports.Any<ushort>() || rports.Contains((ushort)x.RemotePort)
                         where state == null || x.state == (ProcessViewer.ConnectionState)state
                         select x).ToArray<TcpRow>();
                if (array.Any<TcpRow>())
                {
                    TcpRow[] array2 = array;
                    for (int i = 0; i < array2.Length; i++)
                    {
                        TcpRow tcpRow = array2[i];
                        string pname = ProcessAPI.GetProcessNameByPID(tcpRow.owningPid);
                        if (!process.Any<string>() || process.Any((string x) => x.ToLower().Contains(pname.ToLower())))
                        {
                            Icon icon = ProcessAPI.GetIcon(tcpRow.owningPid, true);
                            var row = new DataGridViewRow();
                            var icon2 = new DataGridViewImageCell();
                            icon2.Value = icon;
                            row.Cells.Add(icon2);
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = pname + " " + tcpRow.owningPid });
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = "TCP" });
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = tcpRow.LocalAddress.ToString() });
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = tcpRow.LocalPort.ToString() });
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = tcpRow.RemoteAddress.ToString() });
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = tcpRow.RemotePort.ToString() });
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = tcpRow.state.ToString() });

                            // 添加行到 DataGridView
                            AddRowToDataGridView(row);
                        }
                    }
                }
            }
            if (isTcp == null || !isTcp.Value)
            {
                UdpRow[] array4 = NetProcessAPI.GetAllUdpConnections();
                array4 = (from x in array4
                          where !pids.Any<int>() || pids.Contains(x.owningPid)
                          where !lports.Any<ushort>() || lports.Contains(x.LocalPort)
                          select x).ToArray<UdpRow>();
                if (array4.Any<UdpRow>())
                {
                    UdpRow[] array5 = array4;
                    for (int i = 0; i < array5.Length; i++)
                    {
                        UdpRow udpRow = array5[i];
                        string pname = ProcessAPI.GetProcessNameByPID(udpRow.owningPid);
                        if (!process.Any<string>() || process.Any((string x) => pname.ToLower().Contains(x.ToLower())))
                        {
                            Icon icon2 = ProcessAPI.GetIcon(udpRow.owningPid, true);
                            var row = new DataGridViewRow();
                            var icon3 = new DataGridViewImageCell();
                            icon3.Value = icon2;
                            row.Cells.Add(icon3);
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = pname + " " + udpRow.owningPid });
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = "UDP" });
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = udpRow.LocalAddress.ToString() });
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = udpRow.LocalPort.ToString() });
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = "-" });
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = "-" });
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = "-" });

                            // 添加行到 DataGridView
                            AddRowToDataGridView(row);
                        }
                    }
                }
            }
        }

        // 新增方法，用于线程安全地添加行到 DataGridView
        private void AddRowToDataGridView(DataGridViewRow row)
        {
            if (this.dataGridView1.InvokeRequired)
            {
                this.dataGridView1.Invoke(new Action(() =>
                {
                    row.Cells.Insert(0, new DataGridViewTextBoxCell() { Value = this.dataGridView1.Rows.Count });
                    this.dataGridView1.Rows.Add(row);
                }));
            }
            else
            {
                row.Cells.Insert(0, new DataGridViewTextBoxCell() { Value = this.dataGridView1.Rows.Count });
                this.dataGridView1.Rows.Add(row);
            }
        }

        delegate void D();
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void NetStatus_Load(object sender, EventArgs e)
        {
            this.comboBox1.SelectedIndex = 1;
            this.comboBox2.SelectedIndex = 0;
            this.button1_Click(null, null);
        }
    }
}
