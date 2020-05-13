using System;
using System.Collections.Generic;

namespace xFrame.Core
{
    public class EventBus : IEventBus
    {
        private static readonly ILog log = XFrameContext.GetLogger();

        public static readonly EventBus Default = new EventBus();

        private readonly Dictionary<string, Dictionary<Type, SubjectBase>> channelNotifiers =
            new Dictionary<string, Dictionary<Type, SubjectBase>>();

        private readonly Dictionary<Type, SubjectBase> notifiers = new Dictionary<Type, SubjectBase>();

        public virtual IDisposable Subscribe<T>(Action<T> action)
        {
            SubjectBase notifier;
            lock (notifiers)
            {
                if (!notifiers.TryGetValue(typeof(T), out notifier))
                {
                    notifier = new Subject<T>();
                    notifiers.Add(typeof(T), notifier);
                }
            }

            return (notifier as Subject<T>).Subscribe(action);
        }

        public virtual IDisposable Subscribe<T>(string channel, Action<T> action)
        {
            Dictionary<Type, SubjectBase> dict = null;
            SubjectBase notifier = null;
            lock (channelNotifiers)
            {
                if (!channelNotifiers.TryGetValue(channel, out dict))
                {
                    dict = new Dictionary<Type, SubjectBase>();
                    channelNotifiers.Add(channel, dict);
                }

                if (!dict.TryGetValue(typeof(T), out notifier))
                {
                    notifier = new Subject<T>();
                    dict.Add(typeof(T), notifier);
                }
            }

            return (notifier as Subject<T>).Subscribe(action);
        }

        public virtual void Publish<T>(T message)
        {
            if (message == null)
                return;

            var messageType = message.GetType();

            List<KeyValuePair<Type, SubjectBase>> list;

            lock (notifiers)
            {
                if (notifiers.Count <= 0)
                    return;

                list = new List<KeyValuePair<Type, SubjectBase>>(notifiers);
            }

            foreach (var kv in list)
                try
                {
                    if (kv.Key.IsAssignableFrom(messageType))
                        kv.Value.Publish(message);
                }
                catch (Exception e)
                {
                    log.Warn(e.ToString());
                }
        }

        public virtual void Publish<T>(string channel, T message)
        {
            if (string.IsNullOrEmpty(channel) || message == null)
                return;

            var messageType = message.GetType();
            Dictionary<Type, SubjectBase> dict = null;
            List<KeyValuePair<Type, SubjectBase>> list = null;

            lock (channelNotifiers)
            {
                if (!channelNotifiers.TryGetValue(channel, out dict) || dict.Count <= 0)
                    return;

                list = new List<KeyValuePair<Type, SubjectBase>>(dict);
            }

            foreach (var kv in list)
                try
                {
                    if (kv.Key.IsAssignableFrom(messageType))
                        kv.Value.Publish(message);
                }
                catch (Exception e)
                {
                    log.Warn(e.ToString());
                }
        }
    }
}