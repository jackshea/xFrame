using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace xFrame.Core.EventBus
{
    /// <summary>
    /// 线程安全的事件总线
    /// 使用读写锁和并发集合优化多线程性能
    /// </summary>
    public class ThreadSafeEventBus : IEventBus
    {
        private readonly EventBus _innerEventBus;
        private readonly ReaderWriterLockSlim _publishLock;
        private readonly ReaderWriterLockSlim _subscribeLock;
        private readonly SemaphoreSlim _asyncSemaphore;
        
        // 线程调度器，用于将事件调度到特定线程
        private readonly TaskScheduler _mainThreadScheduler;
        private readonly ConcurrentQueue<Action> _mainThreadActions;
        private volatile bool _isMainThreadProcessing;
        
        /// <summary>
        /// 已发布事件数量
        /// </summary>
        public long PublishedEventCount => _innerEventBus.PublishedEventCount;
        
        /// <summary>
        /// 已处理事件数量
        /// </summary>
        public long ProcessedEventCount => _innerEventBus.ProcessedEventCount;
        
        /// <summary>
        /// 失败事件数量
        /// </summary>
        public long FailedEventCount => _innerEventBus.FailedEventCount;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxConcurrentAsync">最大并发异步处理数量</param>
        /// <param name="maxHistorySize">最大历史记录数量</param>
        /// <param name="historyEnabled">是否启用历史记录</param>
        public ThreadSafeEventBus(int maxConcurrentAsync = 10, int maxHistorySize = 100, bool historyEnabled = true)
        {
            _innerEventBus = new EventBus(maxHistorySize, historyEnabled);
            _publishLock = new ReaderWriterLockSlim();
            _subscribeLock = new ReaderWriterLockSlim();
            _asyncSemaphore = new SemaphoreSlim(maxConcurrentAsync, maxConcurrentAsync);
            
            // 尝试获取主线程调度器（Unity环境下）
            try
            {
                _mainThreadScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            }
            catch
            {
                _mainThreadScheduler = TaskScheduler.Default;
            }
            
            _mainThreadActions = new ConcurrentQueue<Action>();
        }
        
        /// <summary>
        /// 订阅事件（线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        public void Subscribe<T>(IEventHandler<T> handler) where T : IEvent
        {
            _subscribeLock.EnterWriteLock();
            try
            {
                _innerEventBus.Subscribe(handler);
            }
            finally
            {
                _subscribeLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 订阅事件（使用委托，线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理委托</param>
        /// <param name="priority">处理优先级</param>
        /// <returns>订阅标识，用于取消订阅</returns>
        public string Subscribe<T>(Action<T> handler, int priority = 0) where T : IEvent
        {
            _subscribeLock.EnterWriteLock();
            try
            {
                return _innerEventBus.Subscribe(handler, priority);
            }
            finally
            {
                _subscribeLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 订阅异步事件（线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">异步事件处理器</param>
        public void SubscribeAsync<T>(IAsyncEventHandler<T> handler) where T : IEvent
        {
            _subscribeLock.EnterWriteLock();
            try
            {
                _innerEventBus.SubscribeAsync(handler);
            }
            finally
            {
                _subscribeLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 订阅异步事件（使用委托，线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">异步事件处理委托</param>
        /// <param name="priority">处理优先级</param>
        /// <returns>订阅标识，用于取消订阅</returns>
        public string SubscribeAsync<T>(Func<T, Task> handler, int priority = 0) where T : IEvent
        {
            _subscribeLock.EnterWriteLock();
            try
            {
                return _innerEventBus.SubscribeAsync(handler, priority);
            }
            finally
            {
                _subscribeLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 订阅事件并调度到主线程执行
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理委托</param>
        /// <param name="priority">处理优先级</param>
        /// <returns>订阅标识</returns>
        public string SubscribeOnMainThread<T>(Action<T> handler, int priority = 0) where T : IEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            return Subscribe<T>(eventData =>
            {
                // 将处理器调度到主线程执行
                ScheduleOnMainThread(() => handler(eventData));
            }, priority);
        }
        
        /// <summary>
        /// 取消订阅事件（线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        public void Unsubscribe<T>(IEventHandler<T> handler) where T : IEvent
        {
            _subscribeLock.EnterWriteLock();
            try
            {
                _innerEventBus.Unsubscribe(handler);
            }
            finally
            {
                _subscribeLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 取消订阅事件（使用订阅标识，线程安全）
        /// </summary>
        /// <param name="subscriptionId">订阅标识</param>
        public void Unsubscribe(string subscriptionId)
        {
            _subscribeLock.EnterWriteLock();
            try
            {
                _innerEventBus.Unsubscribe(subscriptionId);
            }
            finally
            {
                _subscribeLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 取消所有订阅（线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        public void UnsubscribeAll<T>() where T : IEvent
        {
            _subscribeLock.EnterWriteLock();
            try
            {
                _innerEventBus.UnsubscribeAll<T>();
            }
            finally
            {
                _subscribeLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 发布事件（同步，线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        public void Publish<T>(T eventData) where T : IEvent
        {
            _publishLock.EnterReadLock();
            try
            {
                _innerEventBus.Publish(eventData);
            }
            finally
            {
                _publishLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 发布事件（异步，线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        /// <returns>发布任务</returns>
        public async Task PublishAsync<T>(T eventData) where T : IEvent
        {
            await _asyncSemaphore.WaitAsync();
            
            try
            {
                _publishLock.EnterReadLock();
                try
                {
                    await _innerEventBus.PublishAsync(eventData);
                }
                finally
                {
                    _publishLock.ExitReadLock();
                }
            }
            finally
            {
                _asyncSemaphore.Release();
            }
        }
        
        /// <summary>
        /// 发布事件到主线程
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        /// <returns>发布任务</returns>
        public Task PublishOnMainThread<T>(T eventData) where T : IEvent
        {
            var tcs = new TaskCompletionSource<bool>();
            
            ScheduleOnMainThread(() =>
            {
                try
                {
                    Publish(eventData);
                    tcs.SetResult(true);
                }
                catch (Exception)
                {
                    tcs.SetException(new Exception());
                }
            });
            
            return tcs.Task;
        }
        
        /// <summary>
        /// 批量发布事件（线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="events">事件列表</param>
        public void PublishBatch<T>(IEnumerable<T> events) where T : IEvent
        {
            _publishLock.EnterReadLock();
            try
            {
                _innerEventBus.PublishBatch(events);
            }
            finally
            {
                _publishLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 延迟发布事件（线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        /// <param name="delay">延迟时间（毫秒）</param>
        public void PublishDelayed<T>(T eventData, int delay) where T : IEvent
        {
            _publishLock.EnterReadLock();
            try
            {
                _innerEventBus.PublishDelayed(eventData, delay);
            }
            finally
            {
                _publishLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 添加事件过滤器（线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="filter">事件过滤器</param>
        public void AddFilter<T>(IEventFilter<T> filter) where T : IEvent
        {
            _subscribeLock.EnterWriteLock();
            try
            {
                _innerEventBus.AddFilter(filter);
            }
            finally
            {
                _subscribeLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 移除事件过滤器（线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="filter">事件过滤器</param>
        public void RemoveFilter<T>(IEventFilter<T> filter) where T : IEvent
        {
            _subscribeLock.EnterWriteLock();
            try
            {
                _innerEventBus.RemoveFilter(filter);
            }
            finally
            {
                _subscribeLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 添加事件拦截器（线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="interceptor">事件拦截器</param>
        public void AddInterceptor<T>(IEventInterceptor<T> interceptor) where T : IEvent
        {
            _subscribeLock.EnterWriteLock();
            try
            {
                _innerEventBus.AddInterceptor(interceptor);
            }
            finally
            {
                _subscribeLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 移除事件拦截器（线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="interceptor">事件拦截器</param>
        public void RemoveInterceptor<T>(IEventInterceptor<T> interceptor) where T : IEvent
        {
            _subscribeLock.EnterWriteLock();
            try
            {
                _innerEventBus.RemoveInterceptor(interceptor);
            }
            finally
            {
                _subscribeLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 获取订阅者数量（线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>订阅者数量</returns>
        public int GetSubscriberCount<T>() where T : IEvent
        {
            _subscribeLock.EnterReadLock();
            try
            {
                return _innerEventBus.GetSubscriberCount<T>();
            }
            finally
            {
                _subscribeLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 检查是否有订阅者（线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>true表示有订阅者，false表示没有</returns>
        public bool HasSubscribers<T>() where T : IEvent
        {
            _subscribeLock.EnterReadLock();
            try
            {
                return _innerEventBus.HasSubscribers<T>();
            }
            finally
            {
                _subscribeLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 清空所有订阅（线程安全）
        /// </summary>
        public void Clear()
        {
            _publishLock.EnterWriteLock();
            _subscribeLock.EnterWriteLock();
            
            try
            {
                _innerEventBus.Clear();
                
                // 清空主线程队列
                while (_mainThreadActions.TryDequeue(out _))
                {
                    // 清空队列
                }
            }
            finally
            {
                _subscribeLock.ExitWriteLock();
                _publishLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 获取事件历史记录（线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="count">获取数量</param>
        /// <returns>事件历史列表</returns>
        public IEnumerable<T> GetEventHistory<T>(int count = 10) where T : IEvent
        {
            _publishLock.EnterReadLock();
            try
            {
                return _innerEventBus.GetEventHistory<T>(count);
            }
            finally
            {
                _publishLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 启用或禁用事件历史记录（线程安全）
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetHistoryEnabled(bool enabled)
        {
            _publishLock.EnterWriteLock();
            try
            {
                _innerEventBus.SetHistoryEnabled(enabled);
            }
            finally
            {
                _publishLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 将操作调度到主线程执行
        /// </summary>
        /// <param name="action">要执行的操作</param>
        private void ScheduleOnMainThread(Action action)
        {
            if (action == null)
                return;
            
            _mainThreadActions.Enqueue(action);
            
            // 如果当前就在主线程，直接处理
            if (SynchronizationContext.Current != null)
            {
                ProcessMainThreadActions();
            }
            else
            {
                // 调度到主线程处理
                Task.Factory.StartNew(ProcessMainThreadActions, 
                    CancellationToken.None, 
                    TaskCreationOptions.None, 
                    _mainThreadScheduler);
            }
        }
        
        /// <summary>
        /// 处理主线程队列中的操作
        /// </summary>
        private void ProcessMainThreadActions()
        {
            if (_isMainThreadProcessing)
                return;
            
            _isMainThreadProcessing = true;
            
            try
            {
                while (_mainThreadActions.TryDequeue(out var action))
                {
                    try
                    {
                        action();
                    }
                    catch
                    {
                        // 记录异常但不影响其他操作
                        // 可以在这里添加日志记录
                    }
                }
            }
            finally
            {
                _isMainThreadProcessing = false;
            }
        }
        
        /// <summary>
        /// 手动处理主线程队列（在Unity的Update中调用）
        /// </summary>
        public void ProcessMainThreadQueue()
        {
            ProcessMainThreadActions();
        }
        
        /// <summary>
        /// 获取线程安全事件总线统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public string GetStatistics()
        {
            var innerStats = _innerEventBus.GetStatistics();
            var mainThreadQueueCount = _mainThreadActions.Count;
            var availableAsyncSlots = _asyncSemaphore.CurrentCount;
            
            return $"ThreadSafeEventBus - {innerStats}, " +
                   $"MainThreadQueue: {mainThreadQueueCount}, " +
                   $"AvailableAsyncSlots: {availableAsyncSlots}";
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Clear();
            
            _innerEventBus?.Dispose();
            _publishLock?.Dispose();
            _subscribeLock?.Dispose();
            _asyncSemaphore?.Dispose();
        }
    }
}
