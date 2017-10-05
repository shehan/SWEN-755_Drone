using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            var crashTimer = new Timer { Interval = 10000 };
            crashTimer.Elapsed += CrashTimer_Elapsed;
            crashTimer.Enabled = true;

            _workTimer = new Timer { Interval = 2000 };
            _workTimer.Elapsed += WorkTimer_Elapsed;
            _workTimer.Enabled = true;

            Console.ReadLine();
        }
        private static void WorkTimer_Elapsed(object sender, ElapsedEventArgs e)
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
