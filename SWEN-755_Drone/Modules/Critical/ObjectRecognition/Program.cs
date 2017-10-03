using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Common;
using Timer = System.Timers.Timer;

namespace ObjectRecognition
{
    class Program : Heartbeat
    {
        private static Program _p;
        private static NamedPipeClientStream _namedPipeClientStream;
        private static StreamWriter _streamWriter;

        static void Main(string[] args)
        {
            _p = new Program();

            if (args != null)
            {
                _p.StartBeating(args[0], "ObjectRecognition", ModuleType.Critical);
            }
            else
            {
                Console.WriteLine("The program cannot start without the required arguments");
                Console.ReadLine();
                return;
            }

            var crashTimer = new Timer { Interval = 8000 };
            crashTimer.Elapsed += CrashTimer_Elapsed;
            crashTimer.Enabled = true;

            var workTimer = new Timer { Interval = 2000 };
            workTimer.Elapsed += WorkTimer_Elapsed;
            workTimer.Enabled = true;

            Console.ReadLine();
        }


        private static void WorkTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_namedPipeClientStream == null)
            {
                _namedPipeClientStream = new NamedPipeClientStream("PipeTo" + "ObstacleAvoidance");
                _streamWriter = new StreamWriter(_namedPipeClientStream);
                _namedPipeClientStream.Connect();
                _streamWriter.WriteLine("ObjectRecognition;Connected;");
            }
            else
            {
                _streamWriter.WriteLine("ObjectRecognition;Message;" + Guid.NewGuid());
            }

            _streamWriter.AutoFlush = true;
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
    }
}
