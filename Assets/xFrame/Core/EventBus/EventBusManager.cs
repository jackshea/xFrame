using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace xFrame.Core.EventBus
{
    /// <summary>
    /// 事件总线管理器
    /// 提供全局的事件总线管理功能，支持多个命名的事件总线实例
    /// </summary>
    public class EventBusManager
    {
        private static readonly Lazy<EventBusManager> _instance = new Lazy<EventBusManager>(() => new EventBusManager());
        private readonly ConcurrentDictionary<string, IEventBus> _eventBuses;
        private readonly object _lock = new object();
        
        /// <summary>
        /// 默认事件总线名称
        /// </summary>
        public const string DefaultBusName = "Default";
        
        /// <summary>
        /// 全局事件总线名称
        /// </summary>
        public const string GlobalBusName = "Global";
        
        /// <summary>
        /// UI事件总线名称
        /// </summary>
        public const string UIBusName = "UI";
        
        /// <summary>
        /// 游戏事件总线名称
        /// </summary>
        public const string GameBusName = "Game";
        
        /// <summary>
        /// 网络事件总线名称
        /// </summary>
        public const string NetworkBusName = "Network";
        
        /// <summary>
        /// 单例实例
        /// </summary>
        public static EventBusManager Instance => _instance.Value;
        
        /// <summary>
        /// 默认事件总线
        /// </summary>
        public IEventBus Default => GetOrCreateEventBus(DefaultBusName);
        
        /// <summary>
        /// 全局事件总线
        /// </summary>
        public IEventBus Global => GetOrCreateEventBus(GlobalBusName);
        
        /// <summary>
        /// UI事件总线
        /// </summary>
        public IEventBus UI => GetOrCreateEventBus(UIBusName);
        
        /// <summary>
        /// 游戏事件总线
        /// </summary>
        public IEventBus Game => GetOrCreateEventBus(GameBusName);
        
        /// <summary>
        /// 网络事件总线
        /// </summary>
        public IEventBus Network => GetOrCreateEventBus(NetworkBusName);
        
        /// <summary>
        /// 已注册的事件总线数量
        /// </summary>
        public int Count => _eventBuses.Count;
        
        /// <summary>
        /// 所有事件总线名称
        /// </summary>
        public IEnumerable<string> BusNames => _eventBuses.Keys.ToList();
        
        /// <summary>
        /// 构造函数（私有，单例模式）
        /// </summary>
        private EventBusManager()
        {
            _eventBuses = new ConcurrentDictionary<string, IEventBus>();
            
            // 初始化默认事件总线
            InitializeDefaultBuses();
        }
        
        /// <summary>
        /// 初始化默认事件总线
        /// </summary>
        private void InitializeDefaultBuses()
        {
            // 创建默认事件总线（线程安全）
            RegisterEventBus(DefaultBusName, EventBusFactory.CreateThreadSafe());
            
            // 创建全局事件总线（高性能模式）
            RegisterEventBus(GlobalBusName, EventBusFactory.CreateHighPerformance());
            
            // 创建UI事件总线（主线程安全）
            RegisterEventBus(UIBusName, EventBusFactory.CreateThreadSafe(maxConcurrentAsync: 5));
            
            // 创建游戏事件总线（标准配置）
            RegisterEventBus(GameBusName, EventBusFactory.CreateThreadSafe());
            
            // 创建网络事件总线（高并发）
            RegisterEventBus(NetworkBusName, EventBusFactory.CreateThreadSafe(maxConcurrentAsync: 20));
        }
        
        /// <summary>
        /// 注册事件总线
        /// </summary>
        /// <param name="name">事件总线名称</param>
        /// <param name="eventBus">事件总线实例</param>
        /// <returns>true表示注册成功，false表示名称已存在</returns>
        public bool RegisterEventBus(string name, IEventBus eventBus)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Event bus name cannot be null or empty", nameof(name));
            if (eventBus == null)
                throw new ArgumentNullException(nameof(eventBus));
            
            return _eventBuses.TryAdd(name, eventBus);
        }
        
        /// <summary>
        /// 注册事件总线（使用配置）
        /// </summary>
        /// <param name="name">事件总线名称</param>
        /// <param name="config">事件总线配置</param>
        /// <returns>true表示注册成功，false表示名称已存在</returns>
        public bool RegisterEventBus(string name, EventBusConfig config)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Event bus name cannot be null or empty", nameof(name));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            var eventBus = new EventBusBuilder(config).Build();
            return RegisterEventBus(name, eventBus);
        }
        
        /// <summary>
        /// 注册事件总线（使用构建器）
        /// </summary>
        /// <param name="name">事件总线名称</param>
        /// <param name="builderAction">构建器配置操作</param>
        /// <returns>true表示注册成功，false表示名称已存在</returns>
        public bool RegisterEventBus(string name, Action<EventBusBuilder> builderAction)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Event bus name cannot be null or empty", nameof(name));
            if (builderAction == null)
                throw new ArgumentNullException(nameof(builderAction));
            
            var builder = new EventBusBuilder();
            builderAction(builder);
            var eventBus = builder.Build();
            
            return RegisterEventBus(name, eventBus);
        }
        
        /// <summary>
        /// 取消注册事件总线
        /// </summary>
        /// <param name="name">事件总线名称</param>
        /// <returns>true表示取消成功，false表示事件总线不存在</returns>
        public bool UnregisterEventBus(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            
            if (_eventBuses.TryRemove(name, out var eventBus))
            {
                // 清理事件总线资源
                try
                {
                    eventBus.Clear();
                    if (eventBus is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch 
                {
                    // 忽略异常
                }
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取事件总线
        /// </summary>
        /// <param name="name">事件总线名称</param>
        /// <returns>事件总线实例，如果不存在则返回null</returns>
        public IEventBus GetEventBus(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            
            _eventBuses.TryGetValue(name, out var eventBus);
            return eventBus;
        }
        
        /// <summary>
        /// 获取或创建事件总线
        /// </summary>
        /// <param name="name">事件总线名称</param>
        /// <param name="factory">事件总线工厂方法（可选）</param>
        /// <returns>事件总线实例</returns>
        public IEventBus GetOrCreateEventBus(string name, Func<IEventBus> factory = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Event bus name cannot be null or empty", nameof(name));
            
            return _eventBuses.GetOrAdd(name, _ =>
            {
                return factory?.Invoke() ?? EventBusFactory.CreateThreadSafe();
            });
        }
        
        /// <summary>
        /// 检查事件总线是否存在
        /// </summary>
        /// <param name="name">事件总线名称</param>
        /// <returns>true表示存在，false表示不存在</returns>
        public bool ContainsEventBus(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            
            return _eventBuses.ContainsKey(name);
        }
        
        /// <summary>
        /// 广播事件到所有事件总线
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        /// <param name="excludeBuses">排除的事件总线名称</param>
        public void BroadcastToAll<T>(T eventData, params string[] excludeBuses) where T : IEvent
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));
            
            var excludeSet = excludeBuses?.ToHashSet() ?? new HashSet<string>();
            
            foreach (var kvp in _eventBuses)
            {
                if (!excludeSet.Contains(kvp.Key))
                {
                    try
                    {
                        kvp.Value.Publish(eventData);
                    }
                    catch 
                    {
                        // 忽略异常
                    }
                }
            }
        }
        
        /// <summary>
        /// 异步广播事件到所有事件总线
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        /// <param name="excludeBuses">排除的事件总线名称</param>
        public async System.Threading.Tasks.Task BroadcastToAllAsync<T>(T eventData, params string[] excludeBuses) where T : IEvent
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));
            
            var excludeSet = excludeBuses?.ToHashSet() ?? new HashSet<string>();
            var tasks = new List<System.Threading.Tasks.Task>();
            
            foreach (var kvp in _eventBuses)
            {
                if (!excludeSet.Contains(kvp.Key))
                {
                    var task = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            await kvp.Value.PublishAsync(eventData);
                        }
                        catch 
                        {
                            // 忽略异常
                        }
                    });
                    tasks.Add(task);
                }
            }
            
            await System.Threading.Tasks.Task.WhenAll(tasks);
        }
        
        /// <summary>
        /// 清空所有事件总线
        /// </summary>
        public void ClearAll()
        {
            foreach (var eventBus in _eventBuses.Values)
            {
                try
                {
                    eventBus.Clear();
                }
                catch 
                {
                    // 忽略异常
                }
            }
        }
        
        /// <summary>
        /// 获取所有事件总线的统计信息
        /// </summary>
        /// <returns>统计信息字典</returns>
        public Dictionary<string, string> GetAllStatistics()
        {
            var statistics = new Dictionary<string, string>();
            
            foreach (var kvp in _eventBuses)
            {
                try
                {
                    if (kvp.Value is EventBus eventBus)
                    {
                        statistics[kvp.Key] = eventBus.GetStatistics();
                    }
                    else if (kvp.Value is ThreadSafeEventBus threadSafeEventBus)
                    {
                        statistics[kvp.Key] = threadSafeEventBus.GetStatistics();
                    }
                    else
                    {
                        statistics[kvp.Key] = "Statistics not available";
                    }
                }
                catch 
                {
                    statistics[kvp.Key] = "Error getting statistics";
                }
            }
            
            return statistics;
        }
        
        /// <summary>
        /// 获取管理器统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public string GetManagerStatistics()
        {
            var totalPublished = 0L;
            var totalProcessed = 0L;
            var totalFailed = 0L;
            
            foreach (var eventBus in _eventBuses.Values)
            {
                try
                {
                    if (eventBus is EventBus eb)
                    {
                        totalPublished += eb.PublishedEventCount;
                        totalProcessed += eb.ProcessedEventCount;
                        totalFailed += eb.FailedEventCount;
                    }
                    else if (eventBus is ThreadSafeEventBus tseb)
                    {
                        totalPublished += tseb.PublishedEventCount;
                        totalProcessed += tseb.ProcessedEventCount;
                        totalFailed += tseb.FailedEventCount;
                    }
                }
                catch 
                {
                    // 忽略统计异常
                }
            }
            
            return $"EventBusManager - Buses: {_eventBuses.Count}, " +
                   $"Total Published: {totalPublished}, " +
                   $"Total Processed: {totalProcessed}, " +
                   $"Total Failed: {totalFailed}";
        }
        
        /// <summary>
        /// 释放所有资源
        /// </summary>
        public void Dispose()
        {
            foreach (var kvp in _eventBuses)
            {
                try
                {
                    kvp.Value.Clear();
                    if (kvp.Value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch 
                {
                    // 忽略异常
                }
            }
            
            _eventBuses.Clear();
        }
    }
    
    /// <summary>
    /// 事件总线管理器扩展方法
    /// 提供便捷的访问方式
    /// </summary>
    public static class EventBusManagerExtensions
    {
        /// <summary>
        /// 发布事件到指定的事件总线
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="manager">事件总线管理器</param>
        /// <param name="busName">事件总线名称</param>
        /// <param name="eventData">事件数据</param>
        public static void Publish<T>(this EventBusManager manager, string busName, T eventData) where T : IEvent
        {
            var eventBus = manager.GetEventBus(busName);
            eventBus?.Publish(eventData);
        }
        
        /// <summary>
        /// 异步发布事件到指定的事件总线
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="manager">事件总线管理器</param>
        /// <param name="busName">事件总线名称</param>
        /// <param name="eventData">事件数据</param>
        public static async System.Threading.Tasks.Task PublishAsync<T>(this EventBusManager manager, string busName, T eventData) where T : IEvent
        {
            var eventBus = manager.GetEventBus(busName);
            if (eventBus != null)
            {
                await eventBus.PublishAsync(eventData);
            }
        }
        
        /// <summary>
        /// 订阅事件到指定的事件总线
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="manager">事件总线管理器</param>
        /// <param name="busName">事件总线名称</param>
        /// <param name="handler">事件处理委托</param>
        /// <param name="priority">处理优先级</param>
        /// <returns>订阅标识</returns>
        public static string Subscribe<T>(this EventBusManager manager, string busName, Action<T> handler, int priority = 0) where T : IEvent
        {
            var eventBus = manager.GetEventBus(busName);
            return eventBus?.Subscribe(handler, priority);
        }
        
        /// <summary>
        /// 取消订阅指定事件总线的事件
        /// </summary>
        /// <param name="manager">事件总线管理器</param>
        /// <param name="busName">事件总线名称</param>
        /// <param name="subscriptionId">订阅标识</param>
        public static void Unsubscribe(this EventBusManager manager, string busName, string subscriptionId)
        {
            var eventBus = manager.GetEventBus(busName);
            eventBus?.Unsubscribe(subscriptionId);
        }
    }
}
