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

namespace ObstacleAvoidance
{
    class Program : Heartbeat
    {
        private static Program _p;
        private static NamedPipeServerStream _namedPipeServerStream;
        private static StreamWriter _streamWriter;
        private static StreamReader _streamReader;
        private static Timer _workTimer;
        private static bool _clientConnected = false;

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

            //var crashTimer = new Timer { Interval = 8000 };
            //crashTimer.Elapsed += CrashTimer_Elapsed;           
            //crashTimer.Enabled = true;

            _workTimer = new Timer { Interval = 2000 };
            _workTimer.Elapsed += WorkTimer_Elapsed;
            _workTimer.Enabled = true;

            Console.ReadLine();
        }


        private static async void WorkTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _workTimer.Stop();
            try
            {
                if (_clientConnected)
                {
                    if (!_namedPipeServerStream.IsConnected)
                    {
                        _clientConnected = false;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Connection to ObjectRecognition lost!");
                    }
                }
                else
                {
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

                    var module = message.Split(';')[0];
                    var messageType = message.Split(';')[1];
                    var text = message.Split(';')[2];

                    if (module == "ObjectRecognition")
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
                    _streamWriter.AutoFlush = true;
                    _streamWriter.Write($"ObstacleAvoidance;Message;{Guid.NewGuid()}");
                }
                _p.WorkBeat();
                _workTimer.Start();
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
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
                    "PipeTo" + "ObstacleAvoidance",
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
    }
}
