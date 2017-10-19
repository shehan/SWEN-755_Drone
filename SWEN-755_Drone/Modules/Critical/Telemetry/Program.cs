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
using System.Timers;
using Common;
using Timer = System.Timers.Timer;

namespace Telemetry
{
    class Program : Heartbeat
    {
        private static Program _p;
        private static Timer _workTimer;
        private NamedPipeClientStream _pipeStream;
        private static StreamWriter _streamWriter;

        static void Main(string[] args)
        {
            _p = new Program();

            if (args != null)
            {
                _p.StartBeating(args[0], "Telemetry", ModuleType.Critical);
            }
            else
            {
                Console.WriteLine("The program cannot start without the required arguments");
                Console.ReadLine();
                return;
            }

            _p.Initialize();

            var crashTimer = new Timer { Interval = 5000 };
            crashTimer.Elapsed += CrashTimer_Elapsed;
            crashTimer.Enabled = true;

            _workTimer = new Timer { Interval = 2000 };
            _workTimer.Elapsed += WorkTimer_Elapsed;
            _workTimer.Enabled = true;

            Console.ReadLine();
        }

        private void Initialize()
        {
            if (_pipeStream == null)
            {
                _pipeStream = new NamedPipeClientStream("PipeTo" + "[Redundant]Telemetry");
                _pipeStream.Connect();
                _streamWriter = new StreamWriter(_pipeStream)
                {
                    AutoFlush = true
                };
            }
        }

        private static void WorkTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Doing Work...");
            var random = new Random();
            var randomNumber = random.Next(0, 50);
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

            _streamWriter.AutoFlush = true;
            var guid = Guid.NewGuid().ToString();
            Console.WriteLine($"[Sync] Message Sent: {guid}", ConsoleColor.DarkCyan);
            _streamWriter.WriteLine($"Telemetry;Message;{guid}");

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
