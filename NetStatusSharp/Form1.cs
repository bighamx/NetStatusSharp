using ProcessViewer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetStatusSharp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            dataGridView1.Rows.Clear();
            //tcp
            if (this.comboBox1.SelectedText == "" || this.comboBox1.SelectedText == "全部" || this.comboBox1.SelectedText == "TCP")
            {
                var processNames = new List<string>();
                var allConns = NetProcessAPI.GetAllTcpConnections();
                //pid
                if (!string.IsNullOrEmpty(this.textBox1.Text))
                {
                    var t = this.textBox1.Text.Split(',');
                    foreach (var p in t)
                    {
                        if (Int64.TryParse(p.Trim(), out long pid))
                        {
                            allConns = allConns.Where(x => x.owningPid == pid).ToArray();
                        }
                        else
                        {
                            processNames.Add(p.Trim());
                        }
                    }

                }
                //local port
                if (!string.IsNullOrEmpty(this.textBox2.Text))
                {
                    var t = this.textBox2.Text.Split(',');
                    foreach (var p in t)
                    {
                        if (Int32.TryParse(p.Trim(), out int port))
                        {
                            allConns = allConns.Where(x => x.LocalPort == port).ToArray();
                        }
                    }

                }
                //remote port
                if (!string.IsNullOrEmpty(this.textBox3.Text))
                {
                    var t = this.textBox3.Text.Split(',');
                    foreach (var p in t)
                    {
                        if (Int32.TryParse(p.Trim(), out int port))
                        {
                            allConns = allConns.Where(x => x.RemotePort == port).ToArray();
                        }
                    }
                }
                if (allConns.Any())
                {
                    foreach (var p in allConns)
                    {

                        var pname = ProcessAPI.GetProcessNameByPID(p.owningPid);
                        if (!processNames.Any() || processNames.Any(x => x.ToLower().Contains(pname.ToLower())))
                        {
                            var icon = ProcessAPI.GetIcon(p.owningPid, true);
                            dataGridView1.Rows.Add(new object[]
                            {
                                icon,
                                pname+" "+p.owningPid,
                                "TCP",
                                p.LocalAddress.ToString(),
                                p.LocalPort.ToString(),
                                p.RemoteAddress.ToString(),
                                p.RemotePort.ToString(),
                                p.state.ToString()
                             });
                        }

                    }
                }
            }


            //udp
            if (this.comboBox1.SelectedText == "" || this.comboBox1.SelectedText == "全部" || this.comboBox1.SelectedText == "UDP")
            {
                var allUconns = NetProcessAPI.GetAllUdpConnections();
                var processNames = new List<string>();
                //pid
                if (!string.IsNullOrEmpty(this.textBox1.Text))
                {
                    var t = this.textBox1.Text.Split(',');
                    foreach (var p in t)
                    {
                        if (Int64.TryParse(p.Trim(), out long pid))
                        {
                            allUconns = allUconns.Where(x => x.owningPid == pid).ToArray();
                        }
                        else
                        {
                            processNames.Add(p.Trim());
                        }
                    }
                }
                //local port
                if (!string.IsNullOrEmpty(this.textBox2.Text))
                {
                    var t = this.textBox2.Text.Split(',');
                    foreach (var p in t)
                    {
                        if (Int32.TryParse(p.Trim(), out int port))
                        {
                            allUconns = allUconns.Where(x => x.LocalPort == port).ToArray();
                        }
                    }
                }
                if (allUconns != null)
                {
                    foreach (var p in allUconns)
                    {
                        var pname = ProcessAPI.GetProcessNameByPID(p.owningPid);
                        if (!processNames.Any() || processNames.Any(x => pname.ToLower().Contains(x.ToLower())))
                        {
                            var icon = ProcessAPI.GetIcon(p.owningPid, true);
                            dataGridView1.Rows.Add(new object[]
                            {
                                    icon,
                                    pname+" "+p.owningPid,
                                    "UDP",
                                    p.LocalAddress.ToString(),
                                    p.LocalPort.ToString(),
                                    "-",
                                    "-",
                                    "-"
                            });
                        }

                    }
                }
            }



        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
