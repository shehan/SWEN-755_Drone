using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class InterProcessMessage
    {
        public InterProcessMessage(string module, string message, string messageType)
        {
            Module = module;
            Message = message;
            MessageType = messageType;
        }

        public InterProcessMessage(string concatenatedMessage)
        {
            Module = concatenatedMessage.Split(';')[0];
            Message = concatenatedMessage.Split(';')[1];
            MessageType = concatenatedMessage.Split(';')[2];
        }

        public string Module { get; }

        public string Message { get; }

        public string MessageType { get; }

        public override string ToString()
        {
            return $"{Module};{MessageType};{Message}";
        }

    }
}
