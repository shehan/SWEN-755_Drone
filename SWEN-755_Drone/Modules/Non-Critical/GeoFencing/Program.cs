﻿using System;
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

namespace GeoFencing
{
    class Program : Heartbeat

    {
        private static Program _p;
        private static Timer _workTimer;
        private static NamedPipeClientStream _namedPipeClientStream;
        private static StreamWriter _streamWriter;
        private static StreamReader _streamReader;
        private static bool _clientConnected = false;


        static void Main(string[] args)
        {
            _p = new Program();

            if (args != null)
            {
                _p.StartBeating(args[0], "GeoFencing", ModuleType.NonCritical);
            }
            else
            {
                Console.WriteLine("The program cannot start without the required arguments");
                Console.ReadLine();
                return;
            }

            Thread thread = new Thread(_p.Initialize);
            thread.IsBackground = true;
            thread.Start();

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
            _namedPipeClientStream = new NamedPipeClientStream("PipeTo" + "MissionPlanning");
            _namedPipeClientStream.Connect();
            _clientConnected = true;
            _streamReader = new StreamReader(_namedPipeClientStream);
            _streamWriter = new StreamWriter(_namedPipeClientStream);
            _streamWriter.AutoFlush = true;
            _streamWriter.WriteLine($"GeoFencing;Connected;{Process.GetCurrentProcess().Id.ToString()}");
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
                    if (!_namedPipeClientStream.IsConnected)
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


                string text = string.Empty;
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
                    text = message.Split(';')[2];

                    if (module == "MissionPlanning")
                    {
                        switch (messageType)
                        {
                            case "Message":
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.WriteLine($"Message: {((text))}");
                                Console.WriteLine($"Message received: {(byte.Parse(text))}");
                                break;
                        }
                    }
                }

                if (null != _streamWriter)
                {
                    _streamWriter.AutoFlush = true;
                    _streamWriter.Write($"GeoFencing;Ack;{text}");
                }


                _p.WorkBeat();
                _workTimer.Start();
            }
            catch (Exception error)
            {
                _namedPipeClientStream.Close();

                ThreadPool.QueueUserWorkItem(
                    _ => throw error);
            }
        }
        private static void CrashTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var random = new Random();
                var randomNumber = random.Next(0, 30);
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
