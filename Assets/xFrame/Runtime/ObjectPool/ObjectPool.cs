using System;
using System.Collections.Generic;

namespace xFrame.Runtime.ObjectPool
{
    /// <summary>
    /// 通用对象池实现
    /// 支持线程安全、容量控制、自定义创建/销毁/重置逻辑
    /// </summary>
    /// <typeparam name="T">池化对象的类型</typeparam>
    public class ObjectPool<T> : IObjectPool<T>, IDisposable where T : class
    {
        private readonly Func<T> _createFunc;
        private readonly object _lock;
        private readonly int _maxSize;
        private readonly Action<T> _onDestroy;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Stack<T> _pool;
        private readonly bool _threadSafe;

        private int _countAll;
        private bool _disposed;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="createFunc">对象创建函数</param>
        /// <param name="onGet">获取对象时的回调</param>
        /// <param name="onRelease">释放对象时的回调</param>
        /// <param name="onDestroy">销毁对象时的回调</param>
        /// <param name="maxSize">池的最大容量，-1表示无限制</param>
        /// <param name="threadSafe">是否启用线程安全</param>
        public ObjectPool(
            Func<T> createFunc,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            int maxSize = -1,
            bool threadSafe = false)
        {
            if (createFunc == null)
                throw new ArgumentNullException(nameof(createFunc));

            _createFunc = createFunc;
            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _maxSize = maxSize;
            _threadSafe = threadSafe;
            _pool = new Stack<T>();
            _countAll = 0;
            _disposed = false;

            if (_threadSafe) _lock = new object();
        }

        /// <summary>
        /// 当前池中对象数量
        /// </summary>
        public int CountInPool
        {
            get
            {
                if (_threadSafe)
                    lock (_lock)
                    {
                        return _pool.Count;
                    }

                return _pool.Count;
            }
        }

        /// <summary>
        /// 已创建的对象总数
        /// </summary>
        public int CountAll
        {
            get
            {
                if (_threadSafe)
                    lock (_lock)
                    {
                        return _countAll;
                    }

                return _countAll;
            }
        }

        /// <summary>
        /// 从对象池中获取一个对象
        /// 如果池中没有对象，将创建一个新对象
        /// </summary>
        /// <returns>池化对象实例</returns>
        public T Get()
        {
            ThrowIfDisposed();

            T obj;

            if (_threadSafe)
                lock (_lock)
                {
                    obj = GetInternal();
                }
            else
                obj = GetInternal();

            _onGet?.Invoke(obj);
            return obj;
        }

        /// <summary>
        /// 将对象归还到对象池
        /// </summary>
        /// <param name="obj">要归还的对象</param>
        public void Release(T obj)
        {
            if (obj == null)
                return;

            ThrowIfDisposed();

            if (_threadSafe)
                lock (_lock)
                {
                    ReleaseInternal(obj);
                }
            else
                ReleaseInternal(obj);
        }

        /// <summary>
        /// 预热对象池，预先创建指定数量的对象
        /// </summary>
        /// <param name="count">要预创建的对象数量</param>
        public void WarmUp(int count)
        {
            if (count <= 0)
                return;

            ThrowIfDisposed();

            if (_threadSafe)
                lock (_lock)
                {
                    WarmUpInternal(count);
                }
            else
                WarmUpInternal(count);
        }

        /// <summary>
        /// 清空对象池，销毁所有池中的对象
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();

            if (_threadSafe)
                lock (_lock)
                {
                    ClearInternal();
                }
            else
                ClearInternal();
        }

        /// <summary>
        /// 销毁对象池，释放所有资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            Clear();
            _disposed = true;
        }

        /// <summary>
        /// 内部获取对象的实现
        /// </summary>
        /// <returns>池化对象实例</returns>
        private T GetInternal()
        {
            T obj;

            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
            }
            else
            {
                obj = _createFunc();
                _countAll++;
            }

            return obj;
        }

        /// <summary>
        /// 内部释放对象的实现
        /// </summary>
        /// <param name="obj">要释放的对象</param>
        private void ReleaseInternal(T obj)
        {
            // 防止重复回收同一个对象
            if (_pool.Contains(obj))
                return;

            _onRelease?.Invoke(obj);

            // 检查容量限制
            if (_maxSize > 0 && _pool.Count >= _maxSize)
            {
                // 超过容量限制，销毁对象
                _onDestroy?.Invoke(obj);
                _countAll--;
            }
            else
            {
                // 未超限，放入池中
                _pool.Push(obj);
            }
        }

        /// <summary>
        /// 内部预热的实现
        /// </summary>
        /// <param name="count">要预创建的对象数量</param>
        private void WarmUpInternal(int count)
        {
            for (var i = 0; i < count; i++)
            {
                // 检查是否超过最大容量
                if (_maxSize > 0 && _pool.Count >= _maxSize)
                    break;

                var obj = _createFunc();
                _countAll++;
                _pool.Push(obj);
            }
        }

        /// <summary>
        /// 内部清空的实现
        /// </summary>
        private void ClearInternal()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Pop();
                _onDestroy?.Invoke(obj);
                _countAll--;
            }
        }

        /// <summary>
        /// 检查对象池是否已被销毁
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ObjectPool<T>));
        }
    }
}