using System;

namespace xFrame.Core
{
    public interface IEventBus
    {
        IDisposable Subscribe<T>(Action<T> action);

        IDisposable Subscribe<T>(string channel, Action<T> action);

        void Publish<T>(T message);

        void Publish<T>(string channel, T message);
    }
}