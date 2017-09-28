using System;
using System.Diagnostics;
using System.Timers;
using Common;

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

            var crashTimer = new Timer { Interval = 5000 };
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
                Process.GetCurrentProcess().Kill();
            }
        }
    }
}
