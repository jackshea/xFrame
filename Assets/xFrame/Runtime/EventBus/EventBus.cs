using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using xFrame.Core.ObjectPool;
using xFrame.Core.DataStructures;

namespace xFrame.Core.EventBus
{
    /// <summary>
    /// 事件总线核心实现
    /// 提供完整的事件发布订阅功能
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly EventHandlerRegistry _registry;
        private readonly EventQueue _eventQueue;
        private readonly IObjectPool<BaseEvent> _eventPool;
        
        // 事件历史记录
        private readonly ConcurrentDictionary<Type, Queue<IEvent>> _eventHistory;
        private readonly int _maxHistorySize;
        private bool _historyEnabled;
        
        // 性能统计
        private long _publishedEventCount;
        private long _processedEventCount;
        private long _failedEventCount;
        
        /// <summary>
        /// 已发布事件数量
        /// </summary>
        public long PublishedEventCount => _publishedEventCount;
        
        /// <summary>
        /// 已处理事件数量
        /// </summary>
        public long ProcessedEventCount => _processedEventCount;
        
        /// <summary>
        /// 失败事件数量
        /// </summary>
        public long FailedEventCount => _failedEventCount;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxHistorySize">最大历史记录数量</param>
        /// <param name="historyEnabled">是否启用历史记录</param>
        public EventBus(int maxHistorySize = 100, bool historyEnabled = true)
        {
            _registry = new EventHandlerRegistry();
            _eventQueue = new EventQueue();
            _eventPool = ObjectPoolFactory.Create<BaseEvent>(
                createFunc: () => new BaseEvent<object>(),
                maxSize: 1000,
                threadSafe: false
            );
            
            _eventHistory = new ConcurrentDictionary<Type, Queue<IEvent>>();
            _maxHistorySize = maxHistorySize;
            _historyEnabled = historyEnabled;
        }
        
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        public void Subscribe<T>(IEventHandler<T> handler) where T : IEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            _registry.RegisterSyncHandler(handler);
        }
        
        /// <summary>
        /// 订阅事件（使用委托）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理委托</param>
        /// <param name="priority">处理优先级</param>
        /// <returns>订阅标识，用于取消订阅</returns>
        public string Subscribe<T>(Action<T> handler, int priority = 0) where T : IEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            var delegateHandler = new DelegateEventHandler<T>(handler, priority);
            return _registry.RegisterSyncHandler(delegateHandler);
        }
        
        /// <summary>
        /// 订阅异步事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">异步事件处理器</param>
        public void SubscribeAsync<T>(IAsyncEventHandler<T> handler) where T : IEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            _registry.RegisterAsyncHandler(handler);
        }
        
        /// <summary>
        /// 订阅异步事件（使用委托）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">异步事件处理委托</param>
        /// <param name="priority">处理优先级</param>
        /// <returns>订阅标识，用于取消订阅</returns>
        public string SubscribeAsync<T>(Func<T, Task> handler, int priority = 0) where T : IEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            var delegateHandler = new DelegateAsyncEventHandler<T>(handler, priority);
            return _registry.RegisterAsyncHandler(delegateHandler);
        }
        
        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        public void Unsubscribe<T>(IEventHandler<T> handler) where T : IEvent
        {
            if (handler == null)
                return;
            
            _registry.UnregisterSyncHandler(handler);
        }
        
        /// <summary>
        /// 取消订阅事件（使用订阅标识）
        /// </summary>
        /// <param name="subscriptionId">订阅标识</param>
        public void Unsubscribe(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId))
                return;
            
            _registry.UnregisterBySubscriptionId(subscriptionId);
        }
        
        /// <summary>
        /// 取消所有订阅
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        public void UnsubscribeAll<T>() where T : IEvent
        {
            // 获取所有处理器并逐一取消订阅
            var syncHandlers = _registry.GetSyncHandlers<T>();
            foreach (var handler in syncHandlers)
            {
                _registry.UnregisterSyncHandler(handler);
            }
            
            var asyncHandlers = _registry.GetAsyncHandlers<T>();
            foreach (var handler in asyncHandlers)
            {
                _registry.UnregisterAsyncHandler(handler);
            }
        }
        
        /// <summary>
        /// 发布事件（同步）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        public void Publish<T>(T eventData) where T : IEvent
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));
            
            System.Threading.Interlocked.Increment(ref _publishedEventCount);
            
            try
            {
                // 记录事件历史
                RecordEventHistory(eventData);
                
                // 应用过滤器
                if (!ApplyFilters(eventData))
                {
                    return;
                }
                
                // 获取拦截器
                var interceptors = _registry.GetInterceptors<T>();
                
                // 执行前置拦截
                foreach (var interceptor in interceptors)
                {
                    if (!interceptor.OnBeforeHandle(eventData))
                    {
                        return; // 被拦截器阻止
                    }
                }
                
                // 获取同步处理器
                var syncHandlers = _registry.GetSyncHandlers<T>();
                
                // 处理同步事件
                foreach (var handler in syncHandlers)
                {
                    try
                    {
                        if (handler is BaseEventHandler<T> baseHandler)
                        {
                            baseHandler.SafeHandle(eventData);
                        }
                        else
                        {
                            handler.Handle(eventData);
                        }
                        
                        System.Threading.Interlocked.Increment(ref _processedEventCount);
                        
                        // 如果事件被标记为已处理，可以选择是否继续处理其他处理器
                        if (eventData.IsHandled)
                        {
                            // 可以在这里添加配置来决定是否继续处理
                        }
                        
                        if (eventData.IsCancelled)
                        {
                            break; // 事件被取消，停止处理
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Threading.Interlocked.Increment(ref _failedEventCount);
                        
                        // 尝试通过拦截器处理异常
                        bool handled = false;
                        foreach (var interceptor in interceptors)
                        {
                            if (interceptor.OnException(eventData, ex))
                            {
                                handled = true;
                                break;
                            }
                        }
                        
                        if (!handled)
                        {
                            // 如果拦截器没有处理异常，则重新抛出
                            throw;
                        }
                    }
                }
                
                // 执行后置拦截
                foreach (var interceptor in interceptors)
                {
                    interceptor.OnAfterHandle(eventData);
                }
            }
            catch (Exception ex)
            {
                System.Threading.Interlocked.Increment(ref _failedEventCount);
                throw new EventBusException($"Failed to publish event of type {typeof(T).Name}", ex);
            }
        }
        
        /// <summary>
        /// 发布事件（异步）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        /// <returns>发布任务</returns>
        public async Task PublishAsync<T>(T eventData) where T : IEvent
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));
            
            System.Threading.Interlocked.Increment(ref _publishedEventCount);
            
            try
            {
                // 记录事件历史
                RecordEventHistory(eventData);
                
                // 应用过滤器
                if (!ApplyFilters(eventData))
                {
                    return;
                }
                
                // 获取拦截器
                var interceptors = _registry.GetInterceptors<T>();
                
                // 执行前置拦截
                foreach (var interceptor in interceptors)
                {
                    if (!interceptor.OnBeforeHandle(eventData))
                    {
                        return; // 被拦截器阻止
                    }
                }
                
                // 获取异步处理器
                var asyncHandlers = _registry.GetAsyncHandlers<T>();
                
                // 创建处理任务列表
                var tasks = new List<Task>();
                
                foreach (var handler in asyncHandlers)
                {
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            if (handler is BaseAsyncEventHandler<T> baseHandler)
                            {
                                await baseHandler.SafeHandleAsync(eventData);
                            }
                            else
                            {
                                await handler.HandleAsync(eventData);
                            }
                            
                            System.Threading.Interlocked.Increment(ref _processedEventCount);
                        }
                        catch (Exception ex)
                        {
                            System.Threading.Interlocked.Increment(ref _failedEventCount);
                            
                            // 尝试通过拦截器处理异常
                            bool handled = false;
                            foreach (var interceptor in interceptors)
                            {
                                if (interceptor.OnException(eventData, ex))
                                {
                                    handled = true;
                                    break;
                                }
                            }
                            
                            if (!handled)
                            {
                                throw;
                            }
                        }
                    });
                    
                    tasks.Add(task);
                }
                
                // 等待所有异步处理器完成
                await Task.WhenAll(tasks);
                
                // 执行后置拦截
                foreach (var interceptor in interceptors)
                {
                    interceptor.OnAfterHandle(eventData);
                }
            }
            catch (Exception ex)
            {
                System.Threading.Interlocked.Increment(ref _failedEventCount);
                throw new EventBusException($"Failed to publish async event of type {typeof(T).Name}", ex);
            }
        }
        
        /// <summary>
        /// 批量发布事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="events">事件列表</param>
        public void PublishBatch<T>(IEnumerable<T> events) where T : IEvent
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));
            
            var eventList = events.ToList();
            if (eventList.Count == 0)
                return;
            
            // 按优先级排序
            eventList.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            
            foreach (var eventData in eventList)
            {
                try
                {
                    Publish(eventData);
                }
                catch
                {
                    // 批量处理中的单个事件失败不应该影响其他事件
                    // 可以在这里添加日志记录
                    System.Threading.Interlocked.Increment(ref _failedEventCount);
                }
            }
        }
        
        /// <summary>
        /// 延迟发布事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        /// <param name="delay">延迟时间（毫秒）</param>
        public void PublishDelayed<T>(T eventData, int delay) where T : IEvent
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));
            
            if (delay <= 0)
            {
                Publish(eventData);
                return;
            }
            
            // 将事件加入延迟队列
            _eventQueue.Enqueue(eventData, typeof(T), delay);
            
            // 启动延迟处理任务
            Task.Run(async () =>
            {
                await Task.Delay(delay);
                
                var item = _eventQueue.Dequeue();
                if (item != null && ReferenceEquals(item.Event, eventData))
                {
                    try
                    {
                        Publish((T)item.Event);
                    }
                    finally
                    {
                        _eventQueue.ReleaseItem(item);
                    }
                }
            });
        }
        
        /// <summary>
        /// 添加事件过滤器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="filter">事件过滤器</param>
        public void AddFilter<T>(IEventFilter<T> filter) where T : IEvent
        {
            _registry.AddFilter(filter);
        }
        
        /// <summary>
        /// 移除事件过滤器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="filter">事件过滤器</param>
        public void RemoveFilter<T>(IEventFilter<T> filter) where T : IEvent
        {
            _registry.RemoveFilter(filter);
        }
        
        /// <summary>
        /// 添加事件拦截器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="interceptor">事件拦截器</param>
        public void AddInterceptor<T>(IEventInterceptor<T> interceptor) where T : IEvent
        {
            _registry.AddInterceptor(interceptor);
        }
        
        /// <summary>
        /// 移除事件拦截器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="interceptor">事件拦截器</param>
        public void RemoveInterceptor<T>(IEventInterceptor<T> interceptor) where T : IEvent
        {
            _registry.RemoveInterceptor(interceptor);
        }
        
        /// <summary>
        /// 获取订阅者数量
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>订阅者数量</returns>
        public int GetSubscriberCount<T>() where T : IEvent
        {
            return _registry.GetSubscriberCount<T>();
        }
        
        /// <summary>
        /// 检查是否有订阅者
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>true表示有订阅者，false表示没有</returns>
        public bool HasSubscribers<T>() where T : IEvent
        {
            return _registry.HasSubscribers<T>();
        }
        
        /// <summary>
        /// 清空所有订阅
        /// </summary>
        public void Clear()
        {
            _registry.Clear();
            _eventQueue.Clear();
            _eventHistory.Clear();
            
            // 重置统计计数器
            System.Threading.Interlocked.Exchange(ref _publishedEventCount, 0);
            System.Threading.Interlocked.Exchange(ref _processedEventCount, 0);
            System.Threading.Interlocked.Exchange(ref _failedEventCount, 0);
        }
        
        /// <summary>
        /// 获取事件历史记录
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="count">获取数量</param>
        /// <returns>事件历史列表</returns>
        public IEnumerable<T> GetEventHistory<T>(int count = 10) where T : IEvent
        {
            if (!_historyEnabled)
                return Enumerable.Empty<T>();
            
            var eventType = typeof(T);
            if (_eventHistory.TryGetValue(eventType, out var history))
            {
                return history.Cast<T>().Take(count);
            }
            
            return Enumerable.Empty<T>();
        }
        
        /// <summary>
        /// 启用或禁用事件历史记录
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetHistoryEnabled(bool enabled)
        {
            _historyEnabled = enabled;
            
            if (!enabled)
            {
                _eventHistory.Clear();
            }
        }
        
        /// <summary>
        /// 应用事件过滤器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        /// <returns>true表示通过过滤，false表示被过滤掉</returns>
        private bool ApplyFilters<T>(T eventData) where T : IEvent
        {
            var filters = _registry.GetFilters<T>();
            
            foreach (var filter in filters)
            {
                if (!filter.ShouldHandle(eventData))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 记录事件历史
        /// </summary>
        /// <param name="eventData">事件数据</param>
        private void RecordEventHistory(IEvent eventData)
        {
            if (!_historyEnabled)
                return;
            
            var eventType = eventData.GetType();
            var history = _eventHistory.GetOrAdd(eventType, _ => new Queue<IEvent>());
            
            lock (history)
            {
                history.Enqueue(eventData);
                
                // 保持历史记录大小限制
                while (history.Count > _maxHistorySize)
                {
                    history.Dequeue();
                }
            }
        }
        
        /// <summary>
        /// 获取事件总线统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public string GetStatistics()
        {
            var registryStats = _registry.GetStatistics();
            var queueStats = _eventQueue.GetStatistics();
            
            return $"EventBus Statistics - Published: {_publishedEventCount}, " +
                   $"Processed: {_processedEventCount}, Failed: {_failedEventCount}, " +
                   $"Registry: [{registryStats}], Queue: [{queueStats}]";
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Clear();
            _registry?.Dispose();
        }
    }
    
    /// <summary>
    /// 事件总线异常
    /// </summary>
    public class EventBusException : Exception
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">异常消息</param>
        public EventBusException(string message) : base(message)
        {
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">异常消息</param>
        /// <param name="innerException">内部异常</param>
        public EventBusException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
