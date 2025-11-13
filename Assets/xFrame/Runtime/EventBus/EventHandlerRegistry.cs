using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using xFrame.Core.DataStructures;

namespace xFrame.Core.EventBus
{
    /// <summary>
    /// 事件处理器注册表
    /// 管理事件类型与处理器的映射关系
    /// </summary>
    public class EventHandlerRegistry
    {
        // 同步处理器映射表：事件类型 -> 处理器列表
        private readonly ConcurrentDictionary<Type, List<IEventHandler>> _syncHandlers;
        
        // 异步处理器映射表：事件类型 -> 异步处理器列表（使用 object 存储以避免泛型协变问题）
        private readonly ConcurrentDictionary<Type, List<object>> _asyncHandlers;
        
        // 事件过滤器映射表：事件类型 -> 过滤器列表
        private readonly ConcurrentDictionary<Type, List<object>> _filters;
        
        // 事件拦截器映射表：事件类型 -> 拦截器列表
        private readonly ConcurrentDictionary<Type, List<object>> _interceptors;
        
        // 订阅ID映射表：订阅ID -> (事件类型, 处理器)
        private readonly ConcurrentDictionary<string, (Type EventType, object Handler)> _subscriptionMap;
        
        // 处理器缓存（使用LRU缓存优化性能）
        private readonly ILRUCache<Type, List<IEventHandler>> _syncHandlerCache;
        private readonly ILRUCache<Type, List<object>> _asyncHandlerCache;
        
        // 读写锁，优化并发性能
        private readonly ReaderWriterLockSlim _syncLock = new ReaderWriterLockSlim();
        private readonly ReaderWriterLockSlim _asyncLock = new ReaderWriterLockSlim();
        private readonly ReaderWriterLockSlim _filterLock = new ReaderWriterLockSlim();
        private readonly ReaderWriterLockSlim _interceptorLock = new ReaderWriterLockSlim();
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public EventHandlerRegistry()
        {
            _syncHandlers = new ConcurrentDictionary<Type, List<IEventHandler>>();
            _asyncHandlers = new ConcurrentDictionary<Type, List<object>>();
            _filters = new ConcurrentDictionary<Type, List<object>>();
            _interceptors = new ConcurrentDictionary<Type, List<object>>();
            _subscriptionMap = new ConcurrentDictionary<string, (Type, object)>();
            
            // 初始化LRU缓存，缓存最近使用的处理器列表
            _syncHandlerCache = LRUCacheFactory.Create<Type, List<IEventHandler>>(capacity: 100);
            _asyncHandlerCache = LRUCacheFactory.Create<Type, List<object>>(capacity: 100);
        }
        
        /// <summary>
        /// 注册同步事件处理器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        /// <returns>订阅ID</returns>
        public string RegisterSyncHandler<T>(IEventHandler<T> handler) where T : IEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            var eventType = typeof(T);
            var subscriptionId = Guid.NewGuid().ToString();
            
            _syncLock.EnterWriteLock();
            try
            {
                var handlers = _syncHandlers.GetOrAdd(eventType, _ => new List<IEventHandler>());
                handlers.Add(handler);
                
                // 按优先级排序
                handlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                
                // 清除缓存
                _syncHandlerCache.Remove(eventType);
                
                // 记录订阅映射
                _subscriptionMap[subscriptionId] = (eventType, handler);
            }
            finally
            {
                _syncLock.ExitWriteLock();
            }
            
            return subscriptionId;
        }
        
        /// <summary>
        /// 注册异步事件处理器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">异步事件处理器</param>
        /// <returns>订阅ID</returns>
        public string RegisterAsyncHandler<T>(IAsyncEventHandler<T> handler) where T : IEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            var eventType = typeof(T);
            var subscriptionId = Guid.NewGuid().ToString();
            
            _asyncLock.EnterWriteLock();
            try
            {
                var handlers = _asyncHandlers.GetOrAdd(eventType, _ => new List<object>());
                handlers.Add(handler);
                
                // 按优先级排序
                handlers.Sort((a, b) => ((IAsyncEventHandler<IEvent>)a).Priority.CompareTo(((IAsyncEventHandler<IEvent>)b).Priority));
                
                // 清除缓存
                _asyncHandlerCache.Remove(eventType);
                
                // 记录订阅映射
                _subscriptionMap[subscriptionId] = (eventType, handler);
            }
            finally
            {
                _asyncLock.ExitWriteLock();
            }
            
            return subscriptionId;
        }
        
        /// <summary>
        /// 取消注册同步事件处理器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        /// <returns>true表示成功取消，false表示处理器不存在</returns>
        public bool UnregisterSyncHandler<T>(IEventHandler<T> handler) where T : IEvent
        {
            if (handler == null)
                return false;
            
            var eventType = typeof(T);
            
            _syncLock.EnterWriteLock();
            try
            {
                if (_syncHandlers.TryGetValue(eventType, out var handlers))
                {
                    var removed = handlers.Remove(handler);
                    if (removed)
                    {
                        // 清除缓存
                        _syncHandlerCache.Remove(eventType);
                        
                        // 移除订阅映射
                        var subscriptionToRemove = _subscriptionMap
                            .Where(kvp => kvp.Value.EventType == eventType && ReferenceEquals(kvp.Value.Handler, handler))
                            .Select(kvp => kvp.Key)
                            .FirstOrDefault();
                        
                        if (subscriptionToRemove != null)
                        {
                            _subscriptionMap.TryRemove(subscriptionToRemove, out _);
                        }
                    }
                    return removed;
                }
                return false;
            }
            finally
            {
                _syncLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 取消注册异步事件处理器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">异步事件处理器</param>
        /// <returns>true表示成功取消，false表示处理器不存在</returns>
        public bool UnregisterAsyncHandler<T>(IAsyncEventHandler<T> handler) where T : IEvent
        {
            if (handler == null)
                return false;
            
            var eventType = typeof(T);
            
            _asyncLock.EnterWriteLock();
            try
            {
                if (_asyncHandlers.TryGetValue(eventType, out var handlers))
                {
                    var removed = handlers.Remove(handler);
                    if (removed)
                    {
                        // 清除缓存
                        _asyncHandlerCache.Remove(eventType);
                        
                        // 移除订阅映射
                        var subscriptionToRemove = _subscriptionMap
                            .Where(kvp => kvp.Value.EventType == eventType && ReferenceEquals(kvp.Value.Handler, handler))
                            .Select(kvp => kvp.Key)
                            .FirstOrDefault();
                        
                        if (subscriptionToRemove != null)
                        {
                            _subscriptionMap.TryRemove(subscriptionToRemove, out _);
                        }
                    }
                    return removed;
                }
                return false;
            }
            finally
            {
                _asyncLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 根据订阅ID取消注册
        /// </summary>
        /// <param name="subscriptionId">订阅ID</param>
        /// <returns>true表示成功取消，false表示订阅不存在</returns>
        public bool UnregisterBySubscriptionId(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId))
                return false;
            
            if (_subscriptionMap.TryRemove(subscriptionId, out var subscription))
            {
                var eventType = subscription.EventType;
                var handler = subscription.Handler;
                
                // 根据处理器类型选择相应的取消注册方法
                if (handler is IEventHandler syncHandler)
                {
                    _syncLock.EnterWriteLock();
                    try
                    {
                        if (_syncHandlers.TryGetValue(eventType, out var handlers))
                        {
                            handlers.Remove(syncHandler);
                            _syncHandlerCache.Remove(eventType);
                        }
                    }
                    finally
                    {
                        _syncLock.ExitWriteLock();
                    }
                }
                else if (handler is IAsyncEventHandler<IEvent> asyncHandler)
                {
                    _asyncLock.EnterWriteLock();
                    try
                    {
                        if (_asyncHandlers.TryGetValue(eventType, out var handlers))
                        {
                            handlers.Remove(asyncHandler);
                            _asyncHandlerCache.Remove(eventType);
                        }
                    }
                    finally
                    {
                        _asyncLock.ExitWriteLock();
                    }
                }
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取同步事件处理器列表
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>处理器列表</returns>
        public List<IEventHandler<T>> GetSyncHandlers<T>() where T : IEvent
        {
            var eventType = typeof(T);
            
            // 先尝试从缓存获取
            if (_syncHandlerCache.TryGet(eventType, out var cachedHandlers))
            {
                return cachedHandlers.Cast<IEventHandler<T>>().ToList();
            }
            
            _syncLock.EnterReadLock();
            try
            {
                if (_syncHandlers.TryGetValue(eventType, out var handlers))
                {
                    var result = handlers.Where(h => h.IsActive).ToList();
                    
                    // 缓存结果
                    _syncHandlerCache.Put(eventType, result);
                    
                    return result.Cast<IEventHandler<T>>().ToList();
                }
                
                return new List<IEventHandler<T>>();
            }
            finally
            {
                _syncLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 获取异步事件处理器列表
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>异步处理器列表</returns>
        public List<IAsyncEventHandler<T>> GetAsyncHandlers<T>() where T : IEvent
        {
            var eventType = typeof(T);
            
            // 先尝试从缓存获取
            if (_asyncHandlerCache.TryGet(eventType, out var cachedHandlers))
            {
                return cachedHandlers.Select(h => (IAsyncEventHandler<T>)h).ToList();
            }
            
            _asyncLock.EnterReadLock();
            try
            {
                if (_asyncHandlers.TryGetValue(eventType, out var handlers))
                {
                    var result = handlers.Where(h => ((IAsyncEventHandler<IEvent>)h).IsActive).Select(h => (IAsyncEventHandler<T>)h).ToList();
                    
                    // 缓存结果（转换为 object 列表）
                    _asyncHandlerCache.Put(eventType, handlers.Where(h => ((IAsyncEventHandler<IEvent>)h).IsActive).ToList());
                    
                    return result;
                }
                
                return new List<IAsyncEventHandler<T>>();
            }
            finally
            {
                _asyncLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 添加事件过滤器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="filter">事件过滤器</param>
        public void AddFilter<T>(IEventFilter<T> filter) where T : IEvent
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            
            var eventType = typeof(T);
            
            _filterLock.EnterWriteLock();
            try
            {
                var filters = _filters.GetOrAdd(eventType, _ => new List<object>());
                filters.Add(filter);
            }
            finally
            {
                _filterLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 移除事件过滤器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="filter">事件过滤器</param>
        /// <returns>true表示成功移除，false表示过滤器不存在</returns>
        public bool RemoveFilter<T>(IEventFilter<T> filter) where T : IEvent
        {
            if (filter == null)
                return false;
            
            var eventType = typeof(T);
            
            _filterLock.EnterWriteLock();
            try
            {
                if (_filters.TryGetValue(eventType, out var filters))
                {
                    return filters.Remove(filter);
                }
                return false;
            }
            finally
            {
                _filterLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 获取事件过滤器列表
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>过滤器列表</returns>
        public List<IEventFilter<T>> GetFilters<T>() where T : IEvent
        {
            var eventType = typeof(T);
            
            _filterLock.EnterReadLock();
            try
            {
                if (_filters.TryGetValue(eventType, out var filters))
                {
                    return filters.Cast<IEventFilter<T>>().ToList();
                }
                return new List<IEventFilter<T>>();
            }
            finally
            {
                _filterLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 添加事件拦截器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="interceptor">事件拦截器</param>
        public void AddInterceptor<T>(IEventInterceptor<T> interceptor) where T : IEvent
        {
            if (interceptor == null)
                throw new ArgumentNullException(nameof(interceptor));
            
            var eventType = typeof(T);
            
            _interceptorLock.EnterWriteLock();
            try
            {
                var interceptors = _interceptors.GetOrAdd(eventType, _ => new List<object>());
                interceptors.Add(interceptor);
                
                // 按优先级排序
                interceptors.Sort((a, b) => ((IEventInterceptor<T>)a).Priority.CompareTo(((IEventInterceptor<T>)b).Priority));
            }
            finally
            {
                _interceptorLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 移除事件拦截器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="interceptor">事件拦截器</param>
        /// <returns>true表示成功移除，false表示拦截器不存在</returns>
        public bool RemoveInterceptor<T>(IEventInterceptor<T> interceptor) where T : IEvent
        {
            if (interceptor == null)
                return false;
            
            var eventType = typeof(T);
            
            _interceptorLock.EnterWriteLock();
            try
            {
                if (_interceptors.TryGetValue(eventType, out var interceptors))
                {
                    return interceptors.Remove(interceptor);
                }
                return false;
            }
            finally
            {
                _interceptorLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 获取事件拦截器列表
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>拦截器列表</returns>
        public List<IEventInterceptor<T>> GetInterceptors<T>() where T : IEvent
        {
            var eventType = typeof(T);
            
            _interceptorLock.EnterReadLock();
            try
            {
                if (_interceptors.TryGetValue(eventType, out var interceptors))
                {
                    return interceptors.Cast<IEventInterceptor<T>>().ToList();
                }
                return new List<IEventInterceptor<T>>();
            }
            finally
            {
                _interceptorLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 获取订阅者数量
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>订阅者数量</returns>
        public int GetSubscriberCount<T>() where T : IEvent
        {
            var eventType = typeof(T);
            var syncCount = 0;
            var asyncCount = 0;
            
            _syncLock.EnterReadLock();
            try
            {
                if (_syncHandlers.TryGetValue(eventType, out var syncHandlers))
                {
                    syncCount = syncHandlers.Count(h => h.IsActive);
                }
            }
            finally
            {
                _syncLock.ExitReadLock();
            }
            
            _asyncLock.EnterReadLock();
            try
            {
                if (_asyncHandlers.TryGetValue(eventType, out var asyncHandlers))
                {
                    asyncCount = asyncHandlers.Count(h => ((IAsyncEventHandler<IEvent>)h).IsActive);
                }
            }
            finally
            {
                _asyncLock.ExitReadLock();
            }
            
            return syncCount + asyncCount;
        }
        
        /// <summary>
        /// 检查是否有订阅者
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>true表示有订阅者，false表示没有</returns>
        public bool HasSubscribers<T>() where T : IEvent
        {
            return GetSubscriberCount<T>() > 0;
        }
        
        /// <summary>
        /// 清空所有注册信息
        /// </summary>
        public void Clear()
        {
            _syncLock.EnterWriteLock();
            _asyncLock.EnterWriteLock();
            _filterLock.EnterWriteLock();
            _interceptorLock.EnterWriteLock();
            
            try
            {
                _syncHandlers.Clear();
                _asyncHandlers.Clear();
                _filters.Clear();
                _interceptors.Clear();
                _subscriptionMap.Clear();
                
                _syncHandlerCache.Clear();
                _asyncHandlerCache.Clear();
            }
            finally
            {
                _interceptorLock.ExitWriteLock();
                _filterLock.ExitWriteLock();
                _asyncLock.ExitWriteLock();
                _syncLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 获取注册表统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public string GetStatistics()
        {
            var syncHandlerCount = _syncHandlers.Values.Sum(list => list.Count);
            var asyncHandlerCount = _asyncHandlers.Values.Sum(list => list.Count);
            var filterCount = _filters.Values.Sum(list => list.Count);
            var interceptorCount = _interceptors.Values.Sum(list => list.Count);
            var subscriptionCount = _subscriptionMap.Count;
            
            return $"SyncHandlers: {syncHandlerCount}, AsyncHandlers: {asyncHandlerCount}, " +
                   $"Filters: {filterCount}, Interceptors: {interceptorCount}, Subscriptions: {subscriptionCount}";
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Clear();
            
            _syncLock?.Dispose();
            _asyncLock?.Dispose();
            _filterLock?.Dispose();
            _interceptorLock?.Dispose();
        }
    }
}
