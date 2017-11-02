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
using System.Collections.Concurrent;

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

            for (int i = 0; i < 3; i++)
            {
                var threadWorkStream = new Thread(_p.StartWorkStream);
                threadWorkStream.IsBackground = true;
                threadWorkStream.Start();
            }

            //var crashTimer = new Timer { Interval = 5000 };
            //crashTimer.Elapsed += CrashTimer_Elapsed;
            //crashTimer.Enabled = true;

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

        private void StartWorkStream()
        {
            try
            {
                PipeSecurity ps = new PipeSecurity();
                ps.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.FullControl, AccessControlType.Allow));
                ps.AddAccessRule(new PipeAccessRule("CREATOR OWNER", PipeAccessRights.FullControl, AccessControlType.Allow));
                ps.AddAccessRule(new PipeAccessRule("SYSTEM", PipeAccessRights.FullControl, AccessControlType.Allow));
                ps.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.FullControl, AccessControlType.Allow));

                NamedPipeServerStream pipeStream = new NamedPipeServerStream("PipeTo" + "[Work]Telemetry", PipeDirection.InOut,
                    3, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 1024, 1024, ps);

                pipeStream.WaitForConnection();

                StreamReader streamReader = new StreamReader(pipeStream);
                string module = string.Empty, message = string.Empty, messageType = string.Empty, messageText = string.Empty;
                while ((message = streamReader.ReadLine()) != null)
                {
                    module = message.Split(';')[0];
                    messageType = message.Split(';')[1];
                    messageText = message.Split(';').Length == 3 ? message.Split(';')[2] : string.Empty;
                    switch (messageType)
                    {
                        case "Connected":
                            //   Console.WriteLine($"Connected: {module}", ConsoleColor.DarkCyan);
                            break;
                        case "Message":
                            //   Console.WriteLine($"Message Received: {messageText}", ConsoleColor.Magenta);
                            //_p.PerformTask(module, messageText);
                            try
                            {
                                var item = new QueueMessage(module, messageText);
                                MessageWorkItem.Add(item);
                                //MessageWorkItem.Enqueue(new QueueMessage(module,messageText));
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"Added to queue: {item.ToString()}", ConsoleColor.Yellow);
                            }
                            catch (Exception ee) { Console.WriteLine(ee); }
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


        private static void WorkTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // _workTimer.Stop();
            //Console.ForegroundColor = ConsoleColor.Yellow;
            // Console.WriteLine("Doing Work...");
            var random = new Random();
            var randomNumber = random.Next(0, 150);
            //if (randomNumber == 0)
            //{
            //    _workTimer.Stop();
            //    Console.ForegroundColor = ConsoleColor.DarkYellow;
            //    Console.WriteLine("Hanging mode active...");
            //    Thread.Sleep(5000);
            //    Console.ForegroundColor = ConsoleColor.DarkYellow;
            //    Console.WriteLine("Hanging mode de-active...");
            //    _workTimer.Start();
            //}

            _streamWriter.AutoFlush = true;
            var guid = Guid.NewGuid().ToString();
            //    Console.WriteLine($"[Sync] Message Sent: {guid}", ConsoleColor.DarkCyan);
            _streamWriter.WriteLine($"Telemetry;Message;{guid}");


            var t = new Thread(_p.Process);
            t.Start();


            _p.WorkBeat();

            //_workTimer.Start();
        }

        private void Process()
        {

            _workTimer.Stop();

            try
            {

                manualResetEvent = new ManualResetEvent(false);
                doneEvents.Add(manualResetEvent);

                ThreadPool.SetMaxThreads(10, 3);

                int c = MessageWorkItem.Count;
                for(int i = 0; i < c; i++)
                {
                    ThreadPool.QueueUserWorkItem(DoWork, manualResetEvent);
                }
                    

                //WaitHandle.WaitAny(doneEvents.ToArray());
                  WaitHandle.WaitAll(doneEvents.ToArray());

                int a, b;
                ThreadPool.GetAvailableThreads(out a, out b);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"[{DateTime.Now.ToString()}] Available threads (after) {a}");

                _workTimer.Start();
            }
            catch (Exception eee)
            {
                Console.WriteLine(eee);
            }
        }

        private void DoWork(object message)
        {
            QueueMessage item;
            lock (MessageWorkItem)
            {
                item = MessageWorkItem.Where(x => x.Status.Equals(QueueMessageStatus.NotStarted)).FirstOrDefault();
                if(item == null)
                {
                    ((ManualResetEvent)manualResetEvent).Set();
                    doneEvents.Remove((ManualResetEvent)manualResetEvent);
                    return;
                }
                Thread.CurrentThread.Priority = item.Module.Equals("SLM") ? ThreadPriority.Lowest : ThreadPriority.AboveNormal;
                item.Status = QueueMessageStatus.Started;
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[{DateTime.Now.ToString()}] Start Processing. {item.ToString()}", ConsoleColor.Magenta);
            }

            Worker.Calculate(new Random().Next(0, 50));

            lock (MessageWorkItem)
            {
                item.Status = QueueMessageStatus.Finished;
                MessageWorkItem.Remove(item);
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{DateTime.Now.ToString()}] End Processing. {item.ToString()}", ConsoleColor.Green);

            ((ManualResetEvent)manualResetEvent).Set();
            doneEvents.Remove((ManualResetEvent)manualResetEvent);
        }

        private ManualResetEvent manualResetEvent;
        private List<ManualResetEvent> doneEvents = new List<ManualResetEvent>();
        public List<QueueMessage> MessageWorkItem = new List<QueueMessage>();


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

    public class QueueMessage
    {
        public string Module;
        public string Message;
        public QueueMessageStatus Status;

        public QueueMessage(string Module, string Message)
        {
            this.Module = Module;
            this.Message = Message;
            this.Status = QueueMessageStatus.NotStarted;
        }

        public override string ToString()
        {
            return $"{this.Module};{this.Message};{this.Status}";
        }
    }
    public enum QueueMessageStatus
    {
        NotStarted,
        Started,
        Finished
    }
}
