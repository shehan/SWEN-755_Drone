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

namespace CriticalMonitor
{

    public partial class Form1 : Form
    {
        private Dictionary<string, Process> _processList;
        private Dictionary<string, bool> _modules;
        string[] modules = { @"Modules\ObstacleAvoidance" };

        public Form1()
        {
            InitializeComponent();
            SpawnPipes();
        }

        private void SpawnPipes()
        {
            foreach (var p in modules)
            {
                var pipedServerThread = new Thread(StartServerListner);
                pipedServerThread.Start();

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "../"+p,
                    Arguments = Process.GetCurrentProcess().Id.ToString()
                };
                var process = Process.Start(processStartInfo);

              //  _processList.Add(p, process);
            }
        }


        private void StartServerListner()
        {
            string module = string.Empty;
            using (var pipeStream =
                new NamedPipeServerStream("PipeTo" + Process.GetCurrentProcess().Id + "Critical", PipeDirection.InOut, 10))
            {

                Console.WriteLine("[Server] Pipe Created, the current process ID is {0}",
                    Process.GetCurrentProcess().Id);

                //wait for a connection from another process
                pipeStream.WaitForConnection();
                Console.WriteLine("[Server] Pipe connection established");

                using (var sr = new StreamReader(pipeStream))
                {
                    string message, messageType;
                    //wait for message to arrive from the pipe, when message arrive print date/time and the message to the console.
                    while ((message = sr.ReadLine()) != null)
                    {
                        module = message.Split(';')[0];
                        messageType = message.Split(';')[1];
                        if (!_modules.ContainsKey(module))
                        {
                            if (messageType == "Connected")
                            {
                                _modules.Add(module, true);
                                Console.WriteLine("Connection Established: " + module);
                               // UpdateConnectionLog("Connection Established: " + module, Color.DarkCyan);
                            }
                        }

                        if (messageType == "Alive")
                        {
                            Console.WriteLine(string.Format("{0} - OK", module));
                            // UpdateDataLog(string.Format("{0} - OK", module), Color.DarkOliveGreen);
                        }
                    }
                    Console.WriteLine("{0}: {1}", DateTime.Now, message);
                }
            }

           // UpdateConnectionLog("Connection Lost: " + module, Color.Red);       
        }
    }
}
