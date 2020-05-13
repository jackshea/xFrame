using System;
using System.Collections.Generic;

namespace xFrame.Core
{
    public abstract class SubjectBase
    {
        public abstract void Publish(object message);
    }

    public class Subject<T> : SubjectBase
    {
        private readonly object _lock = new object();
        private readonly List<Action<T>> actions = new List<Action<T>>();

        public bool IsEmpty()
        {
            lock (_lock)
            {
                return actions.Count <= 0;
            }
        }

        public override void Publish(object message)
        {
            Publish((T)message);
        }

        public void Publish(T message)
        {
            Action<T>[] array = null;
            lock (_lock)
            {
                if (actions.Count <= 0)
                    return;

                array = actions.ToArray();
            }

            foreach (var action in array)
            {
                action(message);
            }
        }

        public IDisposable Subscribe(Action<T> action)
        {
            Add(action);
            return new Subscription(this, action);
        }

        internal void Add(Action<T> action)
        {
            lock (_lock)
            {
                actions.Add(action);
            }
        }

        internal void Remove(Action<T> action)
        {
            lock (_lock)
            {
                actions.Remove(action);
            }
        }

        private class Subscription : IDisposable
        {
            private readonly object _lock = new object();
            private Action<T> action;
            private Subject<T> parent;

            public Subscription(Subject<T> parent, Action<T> action)
            {
                this.parent = parent;
                this.action = action;
            }

            #region IDisposable Support

            private bool disposed;

            protected virtual void Dispose(bool disposing)
            {
                if (disposed)
                    return;

                lock (_lock)
                {
                    try
                    {
                        if (disposed)
                            return;

                        if (parent != null)
                        {
                            parent.Remove(action);
                            action = null;
                            parent = null;
                        }
                    }
                    catch (Exception)
                    {
                    }

                    disposed = true;
                }
            }

            ~Subscription()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            #endregion
        }
    }
}