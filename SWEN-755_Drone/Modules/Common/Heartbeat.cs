using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public abstract class Heartbeat
    {
        private string _parentProcessId;
        private string _module;
        private ModuleType _type;
        private NamedPipeClientStream _pipeStream;
        private StreamWriter _streamWriter;

        public void StartBeating(string parentPocessId, string module, ModuleType type)
        {
            _parentProcessId = parentPocessId;
            _module = module;
            _type = type;

            Initialize();
        }

        private void Initialize()
        {
            if (_pipeStream == null)
            {
                _pipeStream = new NamedPipeClientStream("PipeTo" + _parentProcessId + _type);
                _pipeStream.Connect();
                _streamWriter = new StreamWriter(_pipeStream);

                _streamWriter.AutoFlush = true;
                _streamWriter.WriteLine($"{_module};Connected");
            }
        }

        public void WorkBeat()
        {
            _streamWriter.AutoFlush = true;
            _streamWriter.WriteLine($"{_module};Alive");
        }

        public enum ModuleType
        {
            Critical,
            NonCritical
        }

    }
}
