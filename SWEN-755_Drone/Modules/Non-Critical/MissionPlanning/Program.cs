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

namespace MissionPlanning
{
    class Program: Heartbeat

    {
        private static Program _p;
        private static Timer _workTimer;
        private static NamedPipeServerStream _namedPipeServerStream;
        private static StreamWriter _streamWriter;
        private static StreamReader _streamReader;
        private static bool _clientConnected = false;

        static void Main(string[] args)
        {
            _p = new Program();

            if (args != null)
            {
                _p.StartBeating(args[0], "MissionPlanning", ModuleType.NonCritical);
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

            var crashTimer = new Timer { Interval = 10000 };
            crashTimer.Elapsed += CrashTimer_Elapsed;
            crashTimer.Enabled = true;

            _workTimer = new Timer { Interval = 2000 };
            _workTimer.Elapsed += WorkTimer_Elapsed;
            _workTimer.Enabled = true;

            Console.ReadLine();
        }

        private void StartServerListner()
        {
            try
            {
                PipeSecurity ps = new PipeSecurity();
                ps.AddAccessRule(new PipeAccessRule("Users",
                    PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
                ps.AddAccessRule(new PipeAccessRule("CREATOR OWNER", PipeAccessRights.FullControl,
                    AccessControlType.Allow));
                ps.AddAccessRule(
                    new PipeAccessRule("SYSTEM", PipeAccessRights.FullControl, AccessControlType.Allow));
                ps.AddAccessRule(
                    new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, AccessControlType.Allow));

                _namedPipeServerStream = new NamedPipeServerStream(
                    "PipeTo" + "MissionPlanning",
                    PipeDirection.InOut,
                    1, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 1024, 1024, ps);

                _namedPipeServerStream.WaitForConnection();
                _clientConnected = true;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Connection Successful!");

                if (_streamReader == null)
                {
                    _streamReader = new StreamReader(_namedPipeServerStream);
                }

                if (_streamWriter == null)
                {
                    _streamWriter = new StreamWriter(_namedPipeServerStream);
                }

            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
            }
        }


        private static async void WorkTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _workTimer.Stop();

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

            try
            {
                if (_clientConnected)
                {
                    if (!_namedPipeServerStream.IsConnected)
                    {
                        _clientConnected = false;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Connection to MissionPlanning lost!");
                        _workTimer.Start();
                        return;
                    }
                }
                else
                {
                    _p.WorkBeat();
                    _workTimer.Start();
                    return;
                }

                if (null != _streamReader)
                {
                    string message = string.Empty;
                    char[] buf = new char[300];

                    int count = await _streamReader.ReadAsync(buf, 0, 300);

                    if (0 < count)
                    {
                        message = new string(buf, 0, count);
                    }
                    Console.WriteLine($"{message}");
                    var module = message.Split(';')[0];
                    Console.WriteLine($"{message}");
                    var messageType = message.Split(';')[1];
                    var text = message.Split(';')[2];

                    if (module == "GeoFencing")
                    {

                        switch (messageType)
                        {
                            case "Ack":
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.WriteLine($"Acknowledgement received: {text}");
                                break;
                        }
                    }
                }

                if (null != _streamWriter)
                {
                    var rnd = new Random();
                    _streamWriter.AutoFlush = true;
                    _streamWriter.Write($"MissionPlanning;Message;{rnd.Next(sbyte.MinValue/2, sbyte.MaxValue)}");
                }
                _p.WorkBeat();
                _workTimer.Start();
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
                _workTimer.Start();
                //ThreadPool.QueueUserWorkItem(
                //    _ => throw error);
            }
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
    }
}
