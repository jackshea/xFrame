using System;

namespace xFrame.Infrastructure
{
    #if !(NETFX_CORE || NET_4_6)
    public delegate void NotifyCollectionChangedEventHandler(Object sender, NotifyCollectionChangedEventArgs changeArgs);
    #endif
}