using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Main
{
    class Program
    {
        private static Program _program;
        private static readonly string[] monitors = { @"Monitors\NonCriticalMonitor", @"Monitors\CriticalMonitor" };

        static void Main(string[] args)
        {
            _program = new Program();

            foreach (var monitor in monitors)
            {
                var pipedServerThread = new Thread(_program.StartServerListner);
                pipedServerThread.IsBackground = true;
                pipedServerThread.Start();

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = monitor
                };
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Monitor Process Starting: {monitor}");
                Process.Start(processStartInfo);

            }

            Console.ReadLine();

        }

        private void StartServerListner()
        {
            string module = string.Empty, message, messageType;
            PipeSecurity ps = new PipeSecurity();
            ps.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule("CREATOR OWNER", PipeAccessRights.FullControl, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule("SYSTEM", PipeAccessRights.FullControl, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, AccessControlType.Allow));
            using (var pipeStream =
                new NamedPipeServerStream("PipeToMonitor", PipeDirection.InOut,
                    monitors.Length, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 1024, 1024, ps))
            {
                pipeStream.WaitForConnection();

                using (var sr = new StreamReader(pipeStream))
                {
                    while ((message = sr.ReadLine()) != null)
                    {
                        module = message.Split(';')[0];
                        messageType = message.Split(';')[1];
                        switch (messageType)
                        {
                            case "Connected":
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Connected: {module}");
                                break;
                        }
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Connection Lost: {module}");
        }
    }
}
