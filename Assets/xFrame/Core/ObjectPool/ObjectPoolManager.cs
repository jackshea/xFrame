using System;
using System.Collections.Generic;

namespace xFrame.Core.ObjectPool
{
    /// <summary>
    /// 对象池管理器
    /// 用于统一管理多个对象池实例
    /// </summary>
    public class ObjectPoolManager : IDisposable
    {
        private readonly Dictionary<Type, object> _pools;
        private readonly object _lock;
        private readonly bool _threadSafe;
        private bool _disposed;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="threadSafe">是否启用线程安全</param>
        public ObjectPoolManager(bool threadSafe = false)
        {
            _pools = new Dictionary<Type, object>();
            _threadSafe = threadSafe;
            _disposed = false;

            if (_threadSafe)
            {
                _lock = new object();
            }
        }

        /// <summary>
        /// 注册一个对象池
        /// </summary>
        /// <typeparam name="T">池化对象的类型</typeparam>
        /// <param name="pool">对象池实例</param>
        public void RegisterPool<T>(IObjectPool<T> pool) where T : class
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));

            ThrowIfDisposed();

            Type type = typeof(T);

            if (_threadSafe)
            {
                lock (_lock)
                {
                    _pools[type] = pool;
                }
            }
            else
            {
                _pools[type] = pool;
            }
        }

        /// <summary>
        /// 获取指定类型的对象池
        /// </summary>
        /// <typeparam name="T">池化对象的类型</typeparam>
        /// <returns>对象池实例，如果不存在则返回null</returns>
        public IObjectPool<T> GetPool<T>() where T : class
        {
            ThrowIfDisposed();

            Type type = typeof(T);

            if (_threadSafe)
            {
                lock (_lock)
                {
                    return _pools.TryGetValue(type, out object pool) ? (IObjectPool<T>)pool : null;
                }
            }
            else
            {
                return _pools.TryGetValue(type, out object pool) ? (IObjectPool<T>)pool : null;
            }
        }

        /// <summary>
        /// 获取或创建指定类型的对象池
        /// </summary>
        /// <typeparam name="T">池化对象的类型</typeparam>
        /// <param name="createFunc">对象创建函数</param>
        /// <param name="maxSize">池的最大容量</param>
        /// <returns>对象池实例</returns>
        public IObjectPool<T> GetOrCreatePool<T>(Func<T> createFunc, int maxSize = -1) where T : class
        {
            var pool = GetPool<T>();
            if (pool != null)
                return pool;

            pool = ObjectPoolFactory.Create(createFunc, maxSize, _threadSafe);
            RegisterPool(pool);
            return pool;
        }

        /// <summary>
        /// 获取或创建默认构造函数的对象池
        /// </summary>
        /// <typeparam name="T">池化对象的类型，必须有无参构造函数</typeparam>
        /// <param name="maxSize">池的最大容量</param>
        /// <returns>对象池实例</returns>
        public IObjectPool<T> GetOrCreateDefaultPool<T>(int maxSize = -1) where T : class, new()
        {
            var pool = GetPool<T>();
            if (pool != null)
                return pool;

            pool = ObjectPoolFactory.CreateDefault<T>(maxSize, _threadSafe);
            RegisterPool(pool);
            return pool;
        }

        /// <summary>
        /// 从指定类型的对象池中获取对象
        /// </summary>
        /// <typeparam name="T">池化对象的类型</typeparam>
        /// <returns>池化对象实例，如果池不存在则返回null</returns>
        public T Get<T>() where T : class
        {
            var pool = GetPool<T>();
            return pool?.Get();
        }

        /// <summary>
        /// 将对象释放回对应类型的对象池
        /// </summary>
        /// <typeparam name="T">池化对象的类型</typeparam>
        /// <param name="obj">要释放的对象</param>
        public void Release<T>(T obj) where T : class
        {
            if (obj == null)
                return;

            var pool = GetPool<T>();
            pool?.Release(obj);
        }

        /// <summary>
        /// 预热指定类型的对象池
        /// </summary>
        /// <typeparam name="T">池化对象的类型</typeparam>
        /// <param name="count">要预创建的对象数量</param>
        public void WarmUp<T>(int count) where T : class
        {
            var pool = GetPool<T>();
            pool?.WarmUp(count);
        }

        /// <summary>
        /// 清空指定类型的对象池
        /// </summary>
        /// <typeparam name="T">池化对象的类型</typeparam>
        public void Clear<T>() where T : class
        {
            var pool = GetPool<T>();
            pool?.Clear();
        }

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        public void ClearAll()
        {
            ThrowIfDisposed();

            if (_threadSafe)
            {
                lock (_lock)
                {
                    ClearAllInternal();
                }
            }
            else
            {
                ClearAllInternal();
            }
        }

        /// <summary>
        /// 内部清空所有对象池的实现
        /// </summary>
        private void ClearAllInternal()
        {
            foreach (var pool in _pools.Values)
            {
                if (pool is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _pools.Clear();
        }

        /// <summary>
        /// 销毁对象池管理器，释放所有资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            ClearAll();
            _disposed = true;
        }

        /// <summary>
        /// 检查管理器是否已被销毁
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ObjectPoolManager));
        }
    }
}
