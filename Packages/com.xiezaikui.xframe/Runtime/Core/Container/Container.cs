using System.Collections.Generic;

namespace xFrame.Core
{
    /// 简单的IOC容器
    public class Container : IContainer
    {
        private readonly Dictionary<string, object> items = new Dictionary<string, object>();

        public virtual void Register(string name, object obj)
        {
            items[name] = obj;
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
            items.TryGetValue(name, out var obj);
            return obj;
        }

        public virtual void Unregister(string name)
        {
            items.Remove(name);
        }

        public virtual void Unregister<T>()
        {
            Unregister(typeof(T).Name);
        }
    }
}