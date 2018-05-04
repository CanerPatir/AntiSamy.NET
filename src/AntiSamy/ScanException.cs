using System;

namespace AntiSamy
{
    public class ScanException : Exception
    {
        public ScanException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ScanException(string message)
            : base(message)
        {
        }
    }
}
