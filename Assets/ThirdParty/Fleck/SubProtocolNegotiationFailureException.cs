using System;

namespace Fleck
{
    public class SubProtocolNegotiationFailureException : Exception
    {
        public SubProtocolNegotiationFailureException()
        {
        }

        public SubProtocolNegotiationFailureException(string message) : base(message)
        {
        }

        public SubProtocolNegotiationFailureException(string message, Exception innerException) : base(message,
            innerException)
        {
        }
    }
}