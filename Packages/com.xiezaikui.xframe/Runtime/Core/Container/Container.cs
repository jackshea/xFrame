using System;
using System.Collections.Generic;

namespace xFrame.Container
{
    /// 简单的IOC容器
    public class Container : IContainer, IDisposable
    {
        private readonly Dictionary<string, object> services = new Dictionary<string, object>();

        public virtual void Register(string name, object obj)
        {
            services[name] = obj;
        }

        public virtual void Register<T>(T obj)
        {
            Register(typeof(T).Name, obj);
        }

        public virtual T Resolve<T>()
        {
            return (T)Resolve(typeof(T).Name);
        }

        public virtual object Resolve(string name)
        {
            object obj = null;
            services.TryGetValue(name, out obj);
            return obj;
        }

        public virtual void Unregister(string name)
        {
            services.Remove(name);
        }

        public virtual void Unregister<T>()
        {
            Unregister(typeof(T).Name);
        }

        #region IDisposable Support

        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing) services.Clear();
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}