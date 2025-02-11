using System;

namespace Xerxes
{
    public class Xerxes_Exception : Exception
    {
        private const string Xerxes_Engine_Exception__STRING = "Xerxes_Engine Runtime Exception: ";


        public Log_Message Exception__MESSAGE { get; }

        internal Xerxes_Exception
        (
            Log_Message message
        )
        : base
        (
            message.Log_Message__MESSAGE
        )
        {
            Exception__MESSAGE = message;
        }

        public override string ToString()
        {
            return String.Format
            (
                "{0} {{ {1} }}\n{2}",
                Xerxes_Engine_Exception__STRING,
                Exception__MESSAGE.ToString() ?? ""
            );
        }
    }
}
