using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public class InterCommunication
    {
        private PipeStream _pipeStream;
        private StreamWriter _streamWriter;
        private StreamReader _streamReader;
        private readonly string _pipeName;
        private readonly PipeType _pipeType;
        private bool _clientConnected;

        public InterCommunication(string pipeName, PipeType pipeType)
        {
            _pipeName = pipeName;
            _pipeType = pipeType;

            var pipedServerThread = new Thread(Initialize);
            pipedServerThread.IsBackground = true;
            pipedServerThread.Start();
        }

        private void Initialize()
        {
            if (_pipeType.Equals(PipeType.SERVER))
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


                _pipeStream = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    1, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 1024, 1024, ps);

                ((NamedPipeServerStream)_pipeStream).WaitForConnection();
                _clientConnected = true;

            }
            else
            {
                _pipeStream = new NamedPipeClientStream(_pipeName);
                ((NamedPipeClientStream)_pipeStream).Connect();
                _clientConnected = true;
            }

            _streamReader = new StreamReader(_pipeStream);
            _streamWriter = new StreamWriter(_pipeStream);
        }

        public void SendMessage(InterProcessMessage message)
        {
            _streamWriter.AutoFlush = true;
            _streamWriter.WriteLine(message.ToString());

        }

        public InterProcessMessage ReceieveMessage()
        {
            InterProcessMessage message = null;

            char[] buf = new char[300];

            int count =  _streamReader.ReadBlock(buf, 0, 300);

            if (0 < count)
            {
                message = new InterProcessMessage(new string(buf, 0, count));
            }

            return message;
        }

        public bool IsConnected()
        {
            return _pipeStream.IsConnected;
        }


        public enum PipeType
        {
            SERVER,
            CLIENT
        }
    }
}
