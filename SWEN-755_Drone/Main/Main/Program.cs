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
        private static string criticalProcessId;
        private static string nonCriticalProcessId;
        private static readonly string[] Monitors = {  @"Monitors\CriticalMonitor" };        
        private static readonly string[] CriticalModules = {@"Modules\Telemetry",  };
        private static readonly string[] CriticalModulesRedundant = { @"Modules\Telemetry_Redundant" };
        private static readonly string[] NonCriticalModules = {  };

        static void Main(string[] args)
        {
            _program = new Program();

            foreach (var monitor in Monitors)
            {
                var moduleCount = monitor.Contains("Non") ? NonCriticalModules.Length : CriticalModules.Length+CriticalModulesRedundant.Length;
                var pipedServerThread = new Thread(_program.StartServerListner);
                pipedServerThread.IsBackground = true;
                pipedServerThread.Start();

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = monitor,
                    Arguments = moduleCount.ToString()
                };
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Monitor Process Starting: {monitor}");
                Process.Start(processStartInfo);

            }

            Console.ReadLine();

        }

        private void SpawnProcess(string type)
        {
            if (type.Equals("NONCRITICAL"))
            {
                foreach (var nonCriticalModule in NonCriticalModules)
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = nonCriticalModule,
                        Arguments = nonCriticalProcessId
                    };
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Non-Critical Process Starting: {nonCriticalModule}");
                    Process.Start(processStartInfo);
                }
            }
            else
            {
                foreach (var criticalModule in CriticalModules)
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = criticalModule,
                        Arguments = criticalProcessId
                    };
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Critical Process Starting: {criticalModule}");
                    Process criticalProcess = new Process();
                    criticalProcess.StartInfo = processStartInfo;
                    criticalProcess.Start();
                    criticalProcess.ProcessorAffinity = (IntPtr)1;
                }

                foreach (var criticalModule in CriticalModulesRedundant)
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = criticalModule,
                        Arguments = criticalProcessId
                    };
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[redundant] Critical Process Starting: {criticalModule}");
                    Process criticalProcess = new Process();
                    criticalProcess.StartInfo = processStartInfo;
                    criticalProcess.Start();
                    criticalProcess.ProcessorAffinity = (IntPtr)2;
                }
            }
        }

        private void StartServerListner()
        {
            string module = string.Empty, message, messageType, processId;
            PipeSecurity ps = new PipeSecurity();
            ps.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule("CREATOR OWNER", PipeAccessRights.FullControl, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule("SYSTEM", PipeAccessRights.FullControl, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, AccessControlType.Allow));
            using (var pipeStream =
                new NamedPipeServerStream("PipeToMonitor", PipeDirection.InOut,
                    Monitors.Length, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 1024, 1024, ps))
            {
                pipeStream.WaitForConnection();

                using (var sr = new StreamReader(pipeStream))
                {
                    while ((message = sr.ReadLine()) != null)
                    {
                        module = message.Split(';')[0];
                        messageType = message.Split(';')[1];
                        processId = message.Split(';')[2];

                        if (module == "Critial Monitor")
                        {
                            criticalProcessId = processId;
                            SpawnProcess("CRITICAL");
                        }
                        else
                        {
                            nonCriticalProcessId = processId;
                            SpawnProcess("NONCRITICAL");
                        }

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
