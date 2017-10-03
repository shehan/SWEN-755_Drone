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

            var workTimer = new Timer { Interval = 1000 };
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
    }
}
