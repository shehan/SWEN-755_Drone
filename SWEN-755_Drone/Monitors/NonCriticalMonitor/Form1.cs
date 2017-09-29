using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Common;

namespace NonCriticalMonitor
{
    public partial class Form1 : Form
    {
        readonly string[] _modules = new[] { @"Modules\ObstacleAvoidance", @"Modules\MissionPlanning" };

        public Form1()
        {
            InitializeComponent();
            monitorControl.Initialize(Process.GetCurrentProcess().Id.ToString(), MonitorControl.ModuleTpe.Critical, _modules.Length);

            Thread thread = new Thread(Initialize);
            thread.IsBackground = true;
            thread.Start();
        }


        private void Initialize()
        {
            NamedPipeClientStream pipeStream = new NamedPipeClientStream("PipeToMonitor");
            pipeStream.Connect();
            StreamWriter streamWriter = new StreamWriter(pipeStream)
            {
                AutoFlush = true
            };
            streamWriter.WriteLine($"NonCritial Monitor;Connected");
        }
    }
}
