using System.Collections;
using System.Collections.Generic;

namespace xFrame.Core
{
    /// 上下文
    public class Context
    {
        private static readonly Dictionary<string, Context> contexts = new Dictionary<string, Context>();
        private readonly Dictionary<string, object> variables;
        private readonly IContainer container;
        private readonly Context contextBase; // 基于被嵌套的上下文

        public Context() : this(null, null)
        {
        }

        public Context(IContainer container, Context contextBase)
        {
            variables = new Dictionary<string, object>();
            this.contextBase = contextBase;
            this.container = container ?? new Container();
        }

        public static Context GetContext(string key)
        {
            contexts.TryGetValue(key, out var context);
            return context;
        }

        public static T GetContext<T>(string key) where T : Context
        {
            return (T)GetContext(key);
        }

        public static void AddContext(string key, Context context)
        {
            contexts.Add(key, context);
        }

        public static void RemoveContext(string key)
        {
            contexts.Remove(key);
        }

        public virtual bool Contains(string name, bool cascade = true)
        {
            if (variables.ContainsKey(name))
                return true;

            if (cascade && contextBase != null)
                return contextBase.Contains(name, cascade);

            return false;
        }

        public virtual object Get(string name, bool cascade = true)
        {
            return Get<object>(name, cascade);
        }

        public virtual T Get<T>(string name, bool cascade = true)
        {
            object v;
            if (variables.TryGetValue(name, out v))
                return (T)v;

            if (cascade && contextBase != null)
                return contextBase.Get<T>(name, cascade);

            return default;
        }

        public virtual void Set(string name, object value)
        {
            Set<object>(name, value);
        }

        public virtual void Set<T>(string name, T value)
        {
            variables[name] = value;
        }

        public virtual object Remove(string name)
        {
            return Remove<object>(name);
        }

        public virtual T Remove<T>(string name)
        {
            if (!variables.ContainsKey(name))
                return default;

            var v = variables[name];
            variables.Remove(name);
            return (T)v;
        }

        public virtual IEnumerator GetEnumerator()
        {
            return variables.GetEnumerator();
        }

        public virtual IContainer GetContainer()
        {
            return container;
        }

        public virtual object GetService(string name)
        {
            var result = container.Resolve(name);
            if (result != null)
                return result;

            if (contextBase != null)
                return contextBase.GetService(name);

            return null;
        }

        public virtual T GetService<T>()
        {
            var result = container.Resolve<T>();
            if (result != null)
                return result;

            if (contextBase != null)
                return contextBase.GetService<T>();

            return default;
        }
    }
}