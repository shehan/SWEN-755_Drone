using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Threading;
using System.Timers;
using Common;
using Timer = System.Timers.Timer;

namespace ObstacleAvoidance
{
    class Program: Heartbeat
    {
        private static Program _p;

        static void Main(string[] args)
        {
            _p = new Program();

            if (args != null)
            {
                _p.StartBeating(args[0], "ObstacleAvoidance", ModuleType.Critical);
            }
            else
            {
                Console.WriteLine("The program cannot start without the required arguments");
                Console.ReadLine();
                return;
            }

            var pipedServerThread = new Thread(_p.StartServerListner);
            pipedServerThread.IsBackground = true;
            pipedServerThread.Start();

            var crashTimer = new Timer { Interval = 8000 };
            crashTimer.Elapsed += CrashTimer_Elapsed;           
            crashTimer.Enabled = true;

            var workTimer = new Timer { Interval = 3000 };
            workTimer.Elapsed += WorkTimer_Elapsed;
            workTimer.Enabled = true;

            Console.ReadLine();
        }


        private static void WorkTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _p.WorkBeat();
        }

        private static void CrashTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var random = new Random();
                var randomNumber = random.Next(0, 3);
                randomNumber = 100 / randomNumber;
            }
            catch (Exception ex)
            {
                ThreadPool.QueueUserWorkItem(
                    _ => throw ex);
            }
        }



        private void StartServerListner()
        {
            string module = string.Empty, message, messageType, text;
            PipeSecurity ps = new PipeSecurity();
            ps.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule("CREATOR OWNER", PipeAccessRights.FullControl, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule("SYSTEM", PipeAccessRights.FullControl, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, AccessControlType.Allow));
            using (var pipeStream =
                new NamedPipeServerStream("PipeTo"+ "ObstacleAvoidance", PipeDirection.InOut,
                    1, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 1024, 1024, ps))
            {
                pipeStream.WaitForConnection();

                using (var sr = new StreamReader(pipeStream))
                {
                    while ((message = sr.ReadLine()) != null)
                    {
                        module = message.Split(';')[0];
                        messageType = message.Split(';')[1];
                        text = message.Split(';')[2];

                        if (module == "ObjectRecognition")
                        {

                            switch (messageType)
                            {
                                case "Connected":
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"Connected: {module}");
                                    break;
                                default:
                                    Console.ForegroundColor = ConsoleColor.White;
                                    Console.WriteLine($"Message: {text}");
                                    break;
                            }
                        }
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Connection Lost: {module}");
        }

    }
}
