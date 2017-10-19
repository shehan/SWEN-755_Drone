using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using Common;

namespace WeatherDetection
{
    class Program : Heartbeat

    {
        private static Program _p;
        private static Timer _workTimer;
        private static NamedPipeClientStream _pipeStream;
        private static StreamWriter _streamWriter;
        private static NamedPipeClientStream _pipeStream2;
        private static StreamWriter _streamWriter2;

        static void Main(string[] args)
        {
            _p = new Program();

            if (args != null)
            {
                _p.StartBeating(args[0], "WeatherDetection", ModuleType.NonCritical);
            }
            else
            {
                Console.WriteLine("The program cannot start without the required arguments");
                Console.ReadLine();
                return;
            }

            _p.Initialize();

            var crashTimer = new Timer { Interval = 10000 };
            crashTimer.Elapsed += CrashTimer_Elapsed;
            crashTimer.Enabled = true;

            _workTimer = new Timer { Interval = 2000 };
            _workTimer.Elapsed += WorkTimer_Elapsed;
            _workTimer.Enabled = true;

            Console.ReadLine();
        }

        private void Initialize()
        {
            try
            {
                if (_pipeStream == null)
                {
                    _pipeStream = new NamedPipeClientStream("PipeTo" + "[Work]Telemetry");
                    _pipeStream.Connect();
                    _pipeStream2 = new NamedPipeClientStream("PipeTo" + "[Work]Telemetry");
                    _pipeStream2.Connect();

                    _streamWriter = new StreamWriter(_pipeStream)
                    {
                        AutoFlush = true
                    };

                    _streamWriter2 = new StreamWriter(_pipeStream2)
                    {
                        AutoFlush = true
                    };
                }
            }
            catch (Exception eee)
            {
                Console.WriteLine(eee);
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

            var guid = DateTime.Now.ToString();

            if (_pipeStream.IsConnected | _pipeStream2.IsConnected)
                Console.WriteLine($"Message Sent: {guid}", ConsoleColor.DarkCyan);

            if (_pipeStream.IsConnected)
            {
                _streamWriter.AutoFlush = true;
                _streamWriter.WriteLine($"WeatherDetection;Message;{guid}");
            }

            if (_pipeStream2.IsConnected)
            {
                _streamWriter2.AutoFlush = true;
                _streamWriter2.WriteLine($"WeatherDetection;Message;{guid}");
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

