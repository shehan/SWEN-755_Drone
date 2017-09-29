using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows.Forms;
using Common;

namespace NonCriticalMonitor
{
    public partial class Form1 : Form
    {
        public Form1(string[] args)
        {
            InitializeComponent();
            monitorControl.Initialize(Process.GetCurrentProcess().Id.ToString(), MonitorControl.ModuleTpe.Critical, int.Parse(args[1]));

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
            streamWriter.WriteLine($"NonCritial Monitor;Connected;{Process.GetCurrentProcess().Id.ToString()}");
        }
    }
}
