using System;

namespace AntiSamy
{
    public class PolicyException : Exception
    {
        public PolicyException(string message)
            : base(message)
        {
        }

        public PolicyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
