using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Common;
using Timer = System.Timers.Timer;

namespace Telemetry_Redundant
{
    class Program : Heartbeat
    {
        private static Program _p;
        private static Timer _workTimer;
        private static bool _clientConnectionLost = false;


        static void Main(string[] args)
        {
            _p = new Program();

            if (args != null)
            {
                _p.StartBeating(args[0], "Telemetry_Redundant", ModuleType.Critical);
            }
            else
            {
                Console.WriteLine("The program cannot start without the required arguments");
                Console.ReadLine();
                return;
            }


            var threadStream = new Thread(_p.StartSyncStream);
            threadStream.IsBackground = true;
            threadStream.Start();

            //for (int i = 0; i < 2; i++)
            //{
            //    var threadWorkStream = new Thread(_p.StartWorkStream);
            //    threadWorkStream.IsBackground = true;
            //    threadWorkStream.Start();
            //}

            //var crashTimer = new Timer { Interval = 5000 };
            //crashTimer.Elapsed += CrashTimer_Elapsed;
            //crashTimer.Enabled = true;

            _workTimer = new Timer { Interval = 2000 };
            _workTimer.Elapsed += WorkTimer_Elapsed;
            _workTimer.Enabled = true;

            Console.ReadLine();
        }

        private void StartWorkStream()
        {
            try
            {
                PipeSecurity ps = new PipeSecurity();
                ps.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.FullControl, AccessControlType.Allow));
                ps.AddAccessRule(new PipeAccessRule("CREATOR OWNER", PipeAccessRights.FullControl,
                    AccessControlType.Allow));
                ps.AddAccessRule(new PipeAccessRule("SYSTEM", PipeAccessRights.FullControl, AccessControlType.Allow));
                ps.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, AccessControlType.Allow));

                NamedPipeServerStream pipeStream = new NamedPipeServerStream("PipeTo" + "[Work]Telemetry",
                    PipeDirection.InOut,
                    -1, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 1024, 1024, ps);

                pipeStream.WaitForConnection();

                StreamReader streamReader = new StreamReader(pipeStream);
                string module = string.Empty,
                    message = string.Empty,
                    messageType = string.Empty,
                    messageText = string.Empty;
                while ((message = streamReader.ReadLine()) != null)
                {
                    module = message.Split(';')[0];
                    messageType = message.Split(';')[1];
                    messageText = message.Split(';').Length == 3 ? message.Split(';')[2] : string.Empty;
                    switch (messageType)
                    {
                        case "Connected":
                            Console.WriteLine($"Connected: {module}", ConsoleColor.DarkCyan);
                            break;
                        case "Message":
                            Console.WriteLine($"Message Received: {messageText}", ConsoleColor.Magenta);
                            break;
                    }
                }

                Console.WriteLine($"Connection Lost {module}", ConsoleColor.Red);
            }
            catch (Exception eee)
            {
                Console.WriteLine(eee);
            }
        }

        private void StartSyncStream()
        {
            PipeSecurity ps = new PipeSecurity();
            ps.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule("CREATOR OWNER", PipeAccessRights.FullControl, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule("SYSTEM", PipeAccessRights.FullControl, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, AccessControlType.Allow));

            NamedPipeServerStream pipeStream = new NamedPipeServerStream("PipeTo" + "[Redundant]Telemetry", PipeDirection.InOut,
                1, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 1024, 1024, ps);

            pipeStream.WaitForConnection();

            StreamReader streamReader = new StreamReader(pipeStream);
            string module = string.Empty, message = string.Empty, messageType = string.Empty,messageText=string.Empty;
            while ((message = streamReader.ReadLine()) != null)
            {
                module = message.Split(';')[0];
                messageType = message.Split(';')[1];
                messageText = message.Split(';').Length==3? message.Split(';')[2]:string.Empty;
                switch (messageType)
                {
                    case "Connected":
                        Console.WriteLine($"Connected: {module}", ConsoleColor.DarkCyan);
                        break;
                    case "Message":
                        Console.WriteLine($"[Sync] Message Received: {messageText}", ConsoleColor.DarkCyan);
                        break;
                }
            }

            Console.WriteLine($"Main process dead. Becoming active.", ConsoleColor.Red);
            _clientConnectionLost = true;
        }

        private static void WorkTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_clientConnectionLost)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Doing Work...");

                var random = new Random();
                var randomNumber = random.Next(0, 8);
                if (randomNumber == 0)
                {
                    _workTimer.Stop();
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Hanging mode active...");
                    Thread.Sleep(5000);
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Hanging mode de-active...");
                    _workTimer.Start();
                }

            }
            _p.WorkBeat();
        }


        private static void CrashTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var random = new Random();
                var randomNumber = random.Next(0, 50);
                randomNumber = 100 / randomNumber;
            }
            catch (Exception ex)
            {
                ThreadPool.QueueUserWorkItem(
                    _ => throw ex);
            }
        }
    }
}
