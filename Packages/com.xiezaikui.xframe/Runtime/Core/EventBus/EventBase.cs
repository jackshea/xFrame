using System;

namespace xFrame.Core
{
    public class EventBase : EventArgs
    {
        public EventBase(object sender)
        {
            Sender = sender;
        }

        public object Sender { get; protected set; }
    }
}