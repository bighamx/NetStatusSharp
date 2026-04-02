using ProcessViewer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetStatusSharp
{
    public partial class NetStatus : Form
    {
        private bool isLoading;

        public NetStatus()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (isLoading)
            {
                return;
            }

            FilterCriteria criteria;
            if (!TryBuildCriteria(out criteria))
            {
                return;
            }

            RefreshConnections(criteria);
        }

        private void NetStatus_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 1;
            comboBox2.SelectedIndex = 0;

            RefreshConnections(new FilterCriteria(
                new List<string>(),
                new List<int>(),
                new List<ushort>(),
                new List<ushort>(),
                true,
                null));
        }

        private void RefreshConnections(FilterCriteria criteria)
        {
            SetLoadingState(true);
            TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            Task.Factory.StartNew(() => BuildRows(criteria))
                .ContinueWith(task =>
                {
                    try
                    {
                        if (task.IsFaulted)
                        {
                            Exception exception = task.Exception != null ? task.Exception.GetBaseException() : null;
                            string message = exception != null ? exception.Message : "未知错误";
                            MessageBox.Show(this, "刷新连接列表失败：" + message, "NetStatusSharp", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        RenderRows(task.Result);
                    }
                    finally
                    {
                        SetLoadingState(false);
                    }
                }, uiScheduler);
        }

        private List<object[]> BuildRows(FilterCriteria criteria)
        {
            List<object[]> rows = new List<object[]>();

            if (!criteria.IsTcp.HasValue || criteria.IsTcp.Value)
            {
                IEnumerable<TcpRow> filteredTcpRows =
                    from row in NetProcessAPI.GetAllTcpConnections()
                    where criteria.Pids.Count == 0 || criteria.Pids.Contains(row.owningPid)
                    where criteria.LocalPorts.Count == 0 || criteria.LocalPorts.Contains(row.LocalPort)
                    where criteria.RemotePorts.Count == 0 || criteria.RemotePorts.Contains(row.RemotePort)
                    where !criteria.State.HasValue || row.state == criteria.State.Value
                    select row;

                foreach (TcpRow tcpRow in filteredTcpRows)
                {
                    string processName = ProcessAPI.GetProcessNameByPID(tcpRow.owningPid);
                    if (!MatchesProcessName(criteria.ProcessNames, processName))
                    {
                        continue;
                    }

                    rows.Add(new object[]
                    {
                        null,
                        ProcessAPI.GetIcon(tcpRow.owningPid, true),
                        processName + " " + tcpRow.owningPid,
                        "TCP",
                        tcpRow.LocalAddress.ToString(),
                        tcpRow.LocalPort.ToString(),
                        tcpRow.RemoteAddress.ToString(),
                        tcpRow.RemotePort.ToString(),
                        tcpRow.state.ToString()
                    });
                }
            }

            if (!criteria.IsTcp.HasValue || !criteria.IsTcp.Value)
            {
                IEnumerable<UdpRow> filteredUdpRows =
                    from row in NetProcessAPI.GetAllUdpConnections()
                    where criteria.Pids.Count == 0 || criteria.Pids.Contains(row.owningPid)
                    where criteria.LocalPorts.Count == 0 || criteria.LocalPorts.Contains(row.LocalPort)
                    select row;

                foreach (UdpRow udpRow in filteredUdpRows)
                {
                    string processName = ProcessAPI.GetProcessNameByPID(udpRow.owningPid);
                    if (!MatchesProcessName(criteria.ProcessNames, processName))
                    {
                        continue;
                    }

                    rows.Add(new object[]
                    {
                        null,
                        ProcessAPI.GetIcon(udpRow.owningPid, true),
                        processName + " " + udpRow.owningPid,
                        "UDP",
                        udpRow.LocalAddress.ToString(),
                        udpRow.LocalPort.ToString(),
                        "-",
                        "-",
                        "-"
                    });
                }
            }

            return rows;
        }

        private void RenderRows(List<object[]> rows)
        {
            dataGridView1.Rows.Clear();
            dataGridView1.SuspendLayout();

            try
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    rows[i][0] = i + 1;
                    dataGridView1.Rows.Add(rows[i]);
                }
            }
            finally
            {
                dataGridView1.ResumeLayout();
            }
        }

        private void SetLoadingState(bool loading)
        {
            isLoading = loading;
            button1.Enabled = !loading;
            button1.Text = loading ? "加载中..." : "刷新";
        }

        private bool TryBuildCriteria(out FilterCriteria criteria)
        {
            List<string> processNames = SplitAndTrim(textBox1.Text);
            List<int> pids;
            List<ushort> localPorts;
            List<ushort> remotePorts;

            if (!TryParseIntList(textBox4.Text, "PID", out pids) ||
                !TryParsePortList(textBox2.Text, "本地端口", out localPorts) ||
                !TryParsePortList(textBox3.Text, "远程端口", out remotePorts))
            {
                criteria = null;
                return false;
            }

            bool? isTcp = comboBox1.SelectedIndex <= 0 ? (bool?)null : comboBox1.SelectedIndex == 1;
            ConnectionState? state = comboBox2.SelectedIndex <= 0 ? (ConnectionState?)null : (ConnectionState)comboBox2.SelectedIndex;

            criteria = new FilterCriteria(processNames, pids, localPorts, remotePorts, isTcp, state);
            return true;
        }

        private static List<string> SplitAndTrim(string input)
        {
            return input
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrEmpty(value))
                .ToList();
        }

        private bool TryParseIntList(string input, string fieldName, out List<int> values)
        {
            values = new List<int>();
            foreach (string item in SplitAndTrim(input))
            {
                int value;
                if (!int.TryParse(item, out value) || value < 0)
                {
                    ShowValidationError(fieldName, item);
                    return false;
                }

                values.Add(value);
            }

            return true;
        }

        private bool TryParsePortList(string input, string fieldName, out List<ushort> values)
        {
            values = new List<ushort>();
            foreach (string item in SplitAndTrim(input))
            {
                ushort value;
                if (!ushort.TryParse(item, out value))
                {
                    ShowValidationError(fieldName, item);
                    return false;
                }

                values.Add(value);
            }

            return true;
        }

        private void ShowValidationError(string fieldName, string invalidValue)
        {
            MessageBox.Show(this, fieldName + " 中包含无效值：" + invalidValue, "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static bool MatchesProcessName(ICollection<string> processNames, string processName)
        {
            if (processNames.Count == 0)
            {
                return true;
            }

            string normalizedProcessName = processName ?? string.Empty;
            return processNames.Any(name => normalizedProcessName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }

    internal sealed class FilterCriteria
    {
        public FilterCriteria(List<string> processNames, List<int> pids, List<ushort> localPorts, List<ushort> remotePorts, bool? isTcp, ConnectionState? state)
        {
            ProcessNames = processNames;
            Pids = pids;
            LocalPorts = localPorts;
            RemotePorts = remotePorts;
            IsTcp = isTcp;
            State = state;
        }

        public List<string> ProcessNames { get; private set; }

        public List<int> Pids { get; private set; }

        public List<ushort> LocalPorts { get; private set; }

        public List<ushort> RemotePorts { get; private set; }

        public bool? IsTcp { get; private set; }

        public ConnectionState? State { get; private set; }
    }
}
