using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace xFrame.Runtime.DataStructures
{
    /// <summary>
    /// 线程安全的LRU缓存实现
    /// 使用读写锁来保证线程安全性
    /// </summary>
    /// <typeparam name="TKey">键的类型</typeparam>
    /// <typeparam name="TValue">值的类型</typeparam>
    public class ThreadSafeLRUCache<TKey, TValue> : ILRUCache<TKey, TValue>, IDisposable
    {
        private readonly LRUCache<TKey, TValue> _innerCache;
        private readonly ReaderWriterLockSlim _lock;
        private bool _disposed;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="capacity">缓存的最大容量</param>
        public ThreadSafeLRUCache(int capacity)
        {
            _innerCache = new LRUCache<TKey, TValue>(capacity);
            _lock = new ReaderWriterLockSlim();
            _disposed = false;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _lock?.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// 缓存的最大容量
        /// </summary>
        public int Capacity => _innerCache.Capacity;

        /// <summary>
        /// 当前缓存中的元素数量
        /// </summary>
        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _innerCache.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// 获取所有键的集合（按最近使用顺序排列）
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    // 创建一个完整的副本，确保线程安全
                    return _innerCache.Keys.ToList();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// 获取所有值的集合（按最近使用顺序排列）
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    // 创建一个完整的副本，确保线程安全
                    return _innerCache.Values.ToList();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// 获取指定键对应的值
        /// </summary>
        /// <param name="key">要查找的键</param>
        /// <param name="value">输出参数，如果找到则包含对应的值</param>
        /// <returns>如果找到键则返回true，否则返回false</returns>
        public bool TryGet(TKey key, out TValue value)
        {
            ThrowIfDisposed();

            _lock.EnterWriteLock(); // 需要写锁，因为访问会改变节点位置
            try
            {
                return _innerCache.TryGet(key, out value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 获取指定键对应的值
        /// </summary>
        /// <param name="key">要查找的键</param>
        /// <returns>如果找到键则返回对应的值，否则抛出异常</returns>
        public TValue Get(TKey key)
        {
            ThrowIfDisposed();

            _lock.EnterWriteLock(); // 需要写锁，因为访问会改变节点位置
            try
            {
                return _innerCache.Get(key);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 设置指定键的值
        /// </summary>
        /// <param name="key">要设置的键</param>
        /// <param name="value">要设置的值</param>
        public void Put(TKey key, TValue value)
        {
            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                _innerCache.Put(key, value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 检查缓存中是否包含指定的键
        /// </summary>
        /// <param name="key">要检查的键</param>
        /// <returns>如果包含键则返回true，否则返回false</returns>
        public bool ContainsKey(TKey key)
        {
            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                return _innerCache.ContainsKey(key);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 从缓存中移除指定的键
        /// </summary>
        /// <param name="key">要移除的键</param>
        /// <returns>如果成功移除则返回true，否则返回false</returns>
        public bool Remove(TKey key)
        {
            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                return _innerCache.Remove(key);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 清空缓存中的所有元素
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                _innerCache.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 获取键值对集合的快照
        /// </summary>
        /// <returns>键值对的列表</returns>
        public KeyValuePair<IEnumerable<TKey>, IEnumerable<TValue>> GetKeyValueSnapshot()
        {
            _lock.EnterReadLock();
            try
            {
                var keys = _innerCache.Keys.ToList();
                var values = _innerCache.Values.ToList();
                return new KeyValuePair<IEnumerable<TKey>, IEnumerable<TValue>>(keys, values);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 检查对象是否已被释放
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ThreadSafeLRUCache<TKey, TValue>));
        }

        /// <summary>
        /// 获取缓存的统计信息
        /// </summary>
        /// <returns>包含容量、当前数量等信息的字符串</returns>
        public override string ToString()
        {
            _lock.EnterReadLock();
            try
            {
                return $"ThreadSafeLRUCache[Count={_innerCache.Count}, Capacity={_innerCache.Capacity}]";
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}