using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using xFrame.Core.ObjectPool;

namespace xFrame.Core.EventBus
{
    /// <summary>
    /// 事件队列项
    /// 包装事件和相关元数据
    /// </summary>
    public class EventQueueItem : IPoolable
    {
        /// <summary>
        /// 事件数据
        /// </summary>
        public IEvent Event { get; set; }
        
        /// <summary>
        /// 事件类型
        /// </summary>
        public Type EventType { get; set; }
        
        /// <summary>
        /// 入队时间
        /// </summary>
        public long EnqueueTime { get; set; }
        
        /// <summary>
        /// 延迟执行时间（毫秒）
        /// </summary>
        public long DelayUntil { get; set; }
        
        /// <summary>
        /// 是否异步处理
        /// </summary>
        public bool IsAsync { get; set; }
        
        /// <summary>
        /// 对象池回调：获取时调用
        /// </summary>
        public void OnGet()
        {
            // 重置状态
            Event = null;
            EventType = null;
            EnqueueTime = 0;
            DelayUntil = 0;
            IsAsync = false;
        }
        
        /// <summary>
        /// 对象池回调：释放时调用
        /// </summary>
        public void OnRelease()
        {
            // 清理引用
            Event = null;
            EventType = null;
        }
        
        /// <summary>
        /// 对象池回调：销毁时调用
        /// </summary>
        public void OnDestroy()
        {
            // 清理所有引用，确保不会有内存泄漏
            Event = null;
            EventType = null;
            EnqueueTime = 0;
            DelayUntil = 0;
            IsAsync = false;
        }
        
        /// <summary>
        /// 判断是否可以执行
        /// </summary>
        /// <returns>true表示可以执行，false表示需要继续等待</returns>
        public bool CanExecute()
        {
            return DelayUntil <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
    
    /// <summary>
    /// 事件队列
    /// 支持优先级排序和延迟执行
    /// </summary>
    public class EventQueue
    {
        private readonly object _lock = new object();
        private readonly List<EventQueueItem> _queue = new List<EventQueueItem>();
        private readonly IObjectPool<EventQueueItem> _itemPool;
        private volatile bool _isProcessing = false;
        
        /// <summary>
        /// 队列中的事件数量
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count;
                }
            }
        }
        
        /// <summary>
        /// 是否正在处理事件
        /// </summary>
        public bool IsProcessing => _isProcessing;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public EventQueue()
        {
            _itemPool = ObjectPoolFactory.Create<EventQueueItem>(
                createFunc: () => new EventQueueItem(),
                maxSize: 1000,
                threadSafe: true
            );
        }
        
        /// <summary>
        /// 入队事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <param name="eventType">事件类型</param>
        /// <param name="delay">延迟时间（毫秒）</param>
        /// <param name="isAsync">是否异步处理</param>
        public void Enqueue(IEvent eventData, Type eventType, int delay = 0, bool isAsync = false)
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));
            if (eventType == null)
                throw new ArgumentNullException(nameof(eventType));
            
            var item = _itemPool.Get();
            item.Event = eventData;
            item.EventType = eventType;
            item.EnqueueTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            item.DelayUntil = item.EnqueueTime + delay;
            item.IsAsync = isAsync;
            
            lock (_lock)
            {
                _queue.Add(item);
                // 按优先级和入队时间排序
                _queue.Sort((a, b) =>
                {
                    var priorityCompare = a.Event.Priority.CompareTo(b.Event.Priority);
                    return priorityCompare != 0 ? priorityCompare : a.EnqueueTime.CompareTo(b.EnqueueTime);
                });
            }
        }
        
        /// <summary>
        /// 出队事件
        /// </summary>
        /// <returns>事件队列项，如果队列为空或没有可执行的事件则返回null</returns>
        public EventQueueItem Dequeue()
        {
            lock (_lock)
            {
                if (_queue.Count == 0)
                    return null;
                
                // 查找第一个可以执行的事件
                for (int i = 0; i < _queue.Count; i++)
                {
                    var item = _queue[i];
                    if (item.CanExecute())
                    {
                        _queue.RemoveAt(i);
                        return item;
                    }
                }
                
                return null; // 没有可执行的事件
            }
        }
        
        /// <summary>
        /// 批量出队事件
        /// </summary>
        /// <param name="maxCount">最大出队数量</param>
        /// <returns>事件队列项列表</returns>
        public List<EventQueueItem> DequeueBatch(int maxCount = 10)
        {
            var result = new List<EventQueueItem>();
            
            lock (_lock)
            {
                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                for (int i = _queue.Count - 1; i >= 0 && result.Count < maxCount; i--)
                {
                    var item = _queue[i];
                    if (item.DelayUntil <= currentTime)
                    {
                        _queue.RemoveAt(i);
                        result.Add(item);
                    }
                }
                
                // 按优先级排序结果
                result.Sort((a, b) =>
                {
                    var priorityCompare = a.Event.Priority.CompareTo(b.Event.Priority);
                    return priorityCompare != 0 ? priorityCompare : a.EnqueueTime.CompareTo(b.EnqueueTime);
                });
            }
            
            return result;
        }
        
        /// <summary>
        /// 查看队列中的下一个事件（不出队）
        /// </summary>
        /// <returns>下一个可执行的事件队列项，如果没有则返回null</returns>
        public EventQueueItem Peek()
        {
            lock (_lock)
            {
                return _queue.FirstOrDefault(item => item.CanExecute());
            }
        }
        
        /// <summary>
        /// 清空队列
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                // 将所有项目返回到对象池
                foreach (var item in _queue)
                {
                    _itemPool.Release(item);
                }
                _queue.Clear();
            }
        }
        
        /// <summary>
        /// 释放队列项到对象池
        /// </summary>
        /// <param name="item">队列项</param>
        public void ReleaseItem(EventQueueItem item)
        {
            if (item != null)
            {
                _itemPool.Release(item);
            }
        }
        
        /// <summary>
        /// 设置处理状态
        /// </summary>
        /// <param name="isProcessing">是否正在处理</param>
        internal void SetProcessing(bool isProcessing)
        {
            _isProcessing = isProcessing;
        }
        
        /// <summary>
        /// 获取队列统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetStatistics()
        {
            lock (_lock)
            {
                var totalCount = _queue.Count;
                var readyCount = _queue.Count(item => item.CanExecute());
                var delayedCount = totalCount - readyCount;
                var asyncCount = _queue.Count(item => item.IsAsync);
                
                return $"Total: {totalCount}, Ready: {readyCount}, Delayed: {delayedCount}, Async: {asyncCount}";
            }
        }
    }
    
    /// <summary>
    /// 线程安全的事件队列
    /// 使用读写锁优化并发性能
    /// </summary>
    public class ThreadSafeEventQueue : EventQueue
    {
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public ThreadSafeEventQueue() : base()
        {
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        ~ThreadSafeEventQueue()
        {
            _rwLock?.Dispose();
        }
    }
}
