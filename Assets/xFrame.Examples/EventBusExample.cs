using System;
using System.Threading.Tasks;
using UnityEngine;
using xFrame.Core.EventBus;
using xFrame.Core.EventBus.Events;

namespace xFrame.Examples
{
    /// <summary>
    /// 事件总线使用示例
    /// 演示事件总线的各种功能和使用场景
    /// </summary>
    public class EventBusExample : MonoBehaviour
    {
        private IEventBus _eventBus;
        private string _playerHealthSubscriptionId;
        private string _gameStateSubscriptionId;
        
        /// <summary>
        /// 初始化示例
        /// </summary>
        void Start()
        {
            Debug.Log("=== 事件总线示例开始 ===");
            
            // 创建事件总线实例
            CreateEventBusExample();
            
            // 基础订阅和发布示例
            BasicSubscribeAndPublishExample();
            
            // 异步事件处理示例
            AsyncEventHandlingExample();
            
            // 事件过滤和拦截示例
            FilterAndInterceptorExample();
            
            // 批量事件处理示例
            BatchEventProcessingExample();
            
            // 延迟事件发布示例
            DelayedEventPublishingExample();
            
            // 事件总线管理器示例
            EventBusManagerExample();
            
            // 性能监控示例
            PerformanceMonitoringExample();
            
            Debug.Log("=== 事件总线示例完成 ===");
        }
        
        /// <summary>
        /// 创建事件总线示例
        /// </summary>
        private void CreateEventBusExample()
        {
            Debug.Log("--- 创建事件总线示例 ---");
            
            // 方式1：使用工厂方法创建
            var standardBus = EventBusFactory.Create();
            var threadSafeBus = EventBusFactory.CreateThreadSafe();
            var highPerformanceBus = EventBusFactory.CreateHighPerformance();
            var debugBus = EventBusFactory.CreateDebug();
            var lightweightBus = EventBusFactory.CreateLightweight();
            
            // 方式2：使用构建器创建
            var customBus = new EventBusBuilder()
                .WithThreadSafety(true)
                .WithMaxConcurrentAsync(20)
                .WithHistory(true, 200)
                .WithPerformanceMonitoring(true)
                .WithDebugMode(false)
                .Build();
            
            // 方式3：使用预设配置创建
            var highPerfBus2 = new EventBusBuilder(EventBusConfig.HighPerformance).Build();
            var debugBus2 = new EventBusBuilder(EventBusConfig.Debug).Build();
            
            // 使用线程安全的事件总线作为主要示例
            _eventBus = threadSafeBus;
            
            Debug.Log("事件总线创建完成");
        }
        
        /// <summary>
        /// 基础订阅和发布示例
        /// </summary>
        private void BasicSubscribeAndPublishExample()
        {
            Debug.Log("--- 基础订阅和发布示例 ---");
            
            // 订阅玩家生命值变化事件
            _playerHealthSubscriptionId = _eventBus.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged, priority: 1);
            
            // 订阅游戏状态变化事件
            _gameStateSubscriptionId = _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged, priority: 0);
            
            // 使用处理器类订阅事件
            var playerDeathHandler = new PlayerDeathEventHandler();
            _eventBus.Subscribe<PlayerDeathEvent>(playerDeathHandler);
            
            // 发布事件
            _eventBus.Publish(new PlayerHealthChangedEvent("Player1", 100, 80));
            _eventBus.Publish(new GameStateChangedEvent(GameState.Playing, GameState.Paused));
            _eventBus.Publish(new PlayerDeathEvent("Player1", "敌人攻击"));
            
            Debug.Log($"订阅者数量 - PlayerHealthChanged: {_eventBus.GetSubscriberCount<PlayerHealthChangedEvent>()}");
            Debug.Log($"订阅者数量 - GameStateChanged: {_eventBus.GetSubscriberCount<GameStateChangedEvent>()}");
        }
        
        /// <summary>
        /// 异步事件处理示例
        /// </summary>
        private async void AsyncEventHandlingExample()
        {
            Debug.Log("--- 异步事件处理示例 ---");
            
            // 订阅异步事件处理器
            var asyncHandler = new AsyncPlayerActionHandler();
            _eventBus.SubscribeAsync<PlayerActionEvent>(asyncHandler);
            
            // 使用委托订阅异步事件
            _eventBus.SubscribeAsync<PlayerActionEvent>(async (eventData) =>
            {
                await Task.Delay(100); // 模拟异步操作
                Debug.Log($"异步处理玩家操作: {eventData.Data.ActionType}");
            });
            
            // 发布异步事件
            var playerAction = new PlayerActionEvent("Jump", "Player1", new { Height = 5.0f });
            await _eventBus.PublishAsync(playerAction);
            
            Debug.Log("异步事件处理完成");
        }
        
        /// <summary>
        /// 事件过滤和拦截示例
        /// </summary>
        private void FilterAndInterceptorExample()
        {
            Debug.Log("--- 事件过滤和拦截示例 ---");
            
            // 添加事件过滤器
            var healthFilter = new PlayerHealthFilter(minHealth: 20);
            _eventBus.AddFilter<PlayerHealthChangedEvent>(healthFilter);
            
            // 添加事件拦截器
            var loggingInterceptor = new LoggingInterceptor<PlayerHealthChangedEvent>();
            _eventBus.AddInterceptor<PlayerHealthChangedEvent>(loggingInterceptor);
            
            // 发布事件测试过滤和拦截
            _eventBus.Publish(new PlayerHealthChangedEvent("Player1", 50, 10)); // 会被过滤器过滤掉
            _eventBus.Publish(new PlayerHealthChangedEvent("Player1", 80, 60)); // 正常处理
            
            Debug.Log("过滤和拦截示例完成");
        }
        
        /// <summary>
        /// 批量事件处理示例
        /// </summary>
        private void BatchEventProcessingExample()
        {
            Debug.Log("--- 批量事件处理示例 ---");
            
            // 创建批量事件
            var batchEvents = new[]
            {
                new LogEvent("系统启动", LogLevel.Info, "System"),
                new LogEvent("加载配置", LogLevel.Info, "Config"),
                new LogEvent("初始化完成", LogLevel.Info, "System")
            };
            
            // 订阅日志事件
            _eventBus.Subscribe<LogEvent>(OnLogEvent);
            
            // 批量发布事件
            _eventBus.PublishBatch(batchEvents);
            
            Debug.Log("批量事件处理完成");
        }
        
        /// <summary>
        /// 延迟事件发布示例
        /// </summary>
        private void DelayedEventPublishingExample()
        {
            Debug.Log("--- 延迟事件发布示例 ---");
            
            // 订阅延迟事件
            _eventBus.Subscribe<DelayedActionEvent>(OnDelayedAction);
            
            // 发布延迟事件（2秒后执行）
            var delayedEvent = new DelayedActionEvent("定时保存", 2000);
            _eventBus.PublishDelayed(delayedEvent, 2000);
            
            Debug.Log("延迟事件已发布，将在2秒后执行");
        }
        
        /// <summary>
        /// 事件总线管理器示例
        /// </summary>
        private void EventBusManagerExample()
        {
            Debug.Log("--- 事件总线管理器示例 ---");
            
            var manager = EventBusManager.Instance;
            
            // 使用预定义的事件总线
            manager.Default.Subscribe<SystemStartupEvent>(OnSystemStartup);
            manager.UI.Subscribe<UIClickEvent>(OnUIClick);
            manager.Game.Subscribe<GameEvent>(OnGameEvent);
            
            // 注册自定义事件总线
            manager.RegisterEventBus("Custom", builder =>
                builder.WithThreadSafety(true)
                       .WithMaxConcurrentAsync(5)
                       .WithHistory(false));
            
            // 发布事件到不同的总线
            manager.Default.Publish(new SystemStartupEvent());
            manager.UI.Publish(new UIClickEvent("MainMenuButton"));
            manager.Game.Publish(new GameEvent("LevelComplete"));
            
            // 广播事件到所有总线
            manager.BroadcastToAll(new ErrorEvent("测试错误", ErrorLevel.Warning));
            
            // 获取统计信息
            Debug.Log($"管理器统计: {manager.GetManagerStatistics()}");
            
            Debug.Log("事件总线管理器示例完成");
        }
        
        /// <summary>
        /// 性能监控示例
        /// </summary>
        private void PerformanceMonitoringExample()
        {
            Debug.Log("--- 性能监控示例 ---");
            
            // 订阅性能事件
            _eventBus.Subscribe<PerformanceEvent>(OnPerformanceMetric);
            
            // 发布性能指标
            _eventBus.Publish(new PerformanceEvent("FPS", 60.0, "frames/second"));
            _eventBus.Publish(new PerformanceEvent("MemoryUsage", 512.5, "MB"));
            _eventBus.Publish(new PerformanceEvent("LoadTime", 1.25, "seconds"));
            
            // 获取事件总线统计信息
            if (_eventBus is ThreadSafeEventBus threadSafeBus)
            {
                Debug.Log($"事件总线统计: {threadSafeBus.GetStatistics()}");
            }
            
            Debug.Log("性能监控示例完成");
        }
        
        #region 事件处理方法
        
        /// <summary>
        /// 处理玩家生命值变化事件
        /// </summary>
        private void OnPlayerHealthChanged(PlayerHealthChangedEvent eventData)
        {
            Debug.Log($"玩家 {eventData.Data.PlayerId} 生命值变化: {eventData.Data.OldHealth} -> {eventData.Data.NewHealth}");
        }
        
        /// <summary>
        /// 处理游戏状态变化事件
        /// </summary>
        private void OnGameStateChanged(GameStateChangedEvent eventData)
        {
            Debug.Log($"游戏状态变化: {eventData.Data.OldState} -> {eventData.Data.NewState}");
        }
        
        /// <summary>
        /// 处理日志事件
        /// </summary>
        private void OnLogEvent(LogEvent eventData)
        {
            Debug.Log($"[{eventData.Level}] {eventData.Source}: {eventData.Message}");
        }
        
        /// <summary>
        /// 处理延迟动作事件
        /// </summary>
        private void OnDelayedAction(DelayedActionEvent eventData)
        {
            Debug.Log($"执行延迟动作: {eventData.Data.ActionName} (延迟: {eventData.Data.DelayMs}ms)");
        }
        
        /// <summary>
        /// 处理系统启动事件
        /// </summary>
        private void OnSystemStartup(SystemStartupEvent eventData)
        {
            Debug.Log($"系统启动: {eventData.StartupTime}");
        }
        
        /// <summary>
        /// 处理UI点击事件
        /// </summary>
        private void OnUIClick(UIClickEvent eventData)
        {
            Debug.Log($"UI点击: {eventData.Data.ButtonName}");
        }
        
        /// <summary>
        /// 处理游戏事件
        /// </summary>
        private void OnGameEvent(GameEvent eventData)
        {
            Debug.Log($"游戏事件: {eventData.Data.EventType}");
        }
        
        /// <summary>
        /// 处理性能指标事件
        /// </summary>
        private void OnPerformanceMetric(PerformanceEvent eventData)
        {
            Debug.Log($"性能指标 - {eventData.Data.MetricName}: {eventData.Data.Value} {eventData.Data.Unit}");
        }
        
        #endregion
        
        /// <summary>
        /// 清理资源
        /// </summary>
        void OnDestroy()
        {
            // 取消订阅
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe(_playerHealthSubscriptionId);
                _eventBus.Unsubscribe(_gameStateSubscriptionId);
                
                // 清理事件总线
                _eventBus.Clear();
                
                if (_eventBus is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            
            // 清理管理器
            EventBusManager.Instance.Dispose();
            
            Debug.Log("事件总线示例资源已清理");
        }
    }
    
    #region 示例事件类型
    
    /// <summary>
    /// 玩家生命值变化事件
    /// </summary>
    public class PlayerHealthChangedEvent : BaseEvent<PlayerHealthData>
    {
        public PlayerHealthChangedEvent(string playerId, int oldHealth, int newHealth)
        {
            Data = new PlayerHealthData
            {
                PlayerId = playerId,
                OldHealth = oldHealth,
                NewHealth = newHealth,
                ChangeTime = DateTime.Now
            };
        }
    }
    
    /// <summary>
    /// 玩家生命值数据
    /// </summary>
    public class PlayerHealthData
    {
        public string PlayerId { get; set; }
        public int OldHealth { get; set; }
        public int NewHealth { get; set; }
        public DateTime ChangeTime { get; set; }
    }
    
    /// <summary>
    /// 游戏状态变化事件
    /// </summary>
    public class GameStateChangedEvent : BaseEvent<GameStateData>
    {
        public GameStateChangedEvent(GameState oldState, GameState newState)
        {
            Data = new GameStateData
            {
                OldState = oldState,
                NewState = newState,
                ChangeTime = DateTime.Now
            };
        }
    }
    
    /// <summary>
    /// 游戏状态数据
    /// </summary>
    public class GameStateData
    {
        public GameState OldState { get; set; }
        public GameState NewState { get; set; }
        public DateTime ChangeTime { get; set; }
    }
    
    /// <summary>
    /// 游戏状态枚举
    /// </summary>
    public enum GameState
    {
        Menu,
        Loading,
        Playing,
        Paused,
        GameOver
    }
    
    /// <summary>
    /// 玩家死亡事件
    /// </summary>
    public class PlayerDeathEvent : BaseEvent<PlayerDeathData>
    {
        public PlayerDeathEvent(string playerId, string cause)
        {
            Data = new PlayerDeathData
            {
                PlayerId = playerId,
                Cause = cause,
                DeathTime = DateTime.Now
            };
        }
    }
    
    /// <summary>
    /// 玩家死亡数据
    /// </summary>
    public class PlayerDeathData
    {
        public string PlayerId { get; set; }
        public string Cause { get; set; }
        public DateTime DeathTime { get; set; }
    }
    
    /// <summary>
    /// 玩家操作事件
    /// </summary>
    public class PlayerActionEvent : BaseEvent<PlayerActionData>
    {
        public PlayerActionEvent(string actionType, string playerId, object parameters = null)
        {
            Data = new PlayerActionData
            {
                ActionType = actionType,
                PlayerId = playerId,
                Parameters = parameters,
                ActionTime = DateTime.Now
            };
        }
    }
    
    /// <summary>
    /// 玩家操作数据
    /// </summary>
    public class PlayerActionData
    {
        public string ActionType { get; set; }
        public string PlayerId { get; set; }
        public object Parameters { get; set; }
        public DateTime ActionTime { get; set; }
    }
    
    /// <summary>
    /// 延迟动作事件
    /// </summary>
    public class DelayedActionEvent : BaseEvent<DelayedActionData>
    {
        public DelayedActionEvent(string actionName, int delayMs)
        {
            Data = new DelayedActionData
            {
                ActionName = actionName,
                DelayMs = delayMs,
                ScheduleTime = DateTime.Now
            };
        }
    }
    
    /// <summary>
    /// 延迟动作数据
    /// </summary>
    public class DelayedActionData
    {
        public string ActionName { get; set; }
        public int DelayMs { get; set; }
        public DateTime ScheduleTime { get; set; }
    }
    
    /// <summary>
    /// UI点击事件
    /// </summary>
    public class UIClickEvent : BaseEvent<UIClickData>
    {
        public UIClickEvent(string buttonName)
        {
            Data = new UIClickData
            {
                ButtonName = buttonName,
                ClickTime = DateTime.Now
            };
        }
    }
    
    /// <summary>
    /// UI点击数据
    /// </summary>
    public class UIClickData
    {
        public string ButtonName { get; set; }
        public DateTime ClickTime { get; set; }
    }
    
    /// <summary>
    /// 游戏事件
    /// </summary>
    public class GameEvent : BaseEvent<GameEventData>
    {
        public GameEvent(string eventType)
        {
            Data = new GameEventData
            {
                EventType = eventType,
                EventTime = DateTime.Now
            };
        }
    }
    
    /// <summary>
    /// 游戏事件数据
    /// </summary>
    public class GameEventData
    {
        public string EventType { get; set; }
        public DateTime EventTime { get; set; }
    }
    
    #endregion
    
    #region 示例处理器类
    
    /// <summary>
    /// 玩家死亡事件处理器
    /// </summary>
    public class PlayerDeathEventHandler : BaseEventHandler<PlayerDeathEvent>
    {
        public override int Priority => 10; // 高优先级
        
        public override void Handle(PlayerDeathEvent eventData)
        {
            Debug.Log($"[高优先级] 玩家 {eventData.Data.PlayerId} 死亡，原因: {eventData.Data.Cause}");
            
            // 可以在这里添加复杂的处理逻辑
            // 例如：保存游戏状态、显示死亡界面、重置玩家等
        }
    }
    
    /// <summary>
    /// 异步玩家操作处理器
    /// </summary>
    public class AsyncPlayerActionHandler : BaseAsyncEventHandler<PlayerActionEvent>
    {
        public override async Task HandleAsync(PlayerActionEvent eventData)
        {
            Debug.Log($"开始异步处理玩家操作: {eventData.Data.ActionType}");
            
            // 模拟异步操作
            await Task.Delay(500);
            
            Debug.Log($"异步处理完成: {eventData.Data.ActionType}");
        }
    }
    
    /// <summary>
    /// 玩家生命值过滤器
    /// </summary>
    public class PlayerHealthFilter : IEventFilter<PlayerHealthChangedEvent>
    {
        private readonly int _minHealth;
        
        public PlayerHealthFilter(int minHealth)
        {
            _minHealth = minHealth;
        }
        
        public bool ShouldHandle(PlayerHealthChangedEvent eventData)
        {
            // 只处理生命值大于最小值的事件
            bool shouldHandle = eventData.Data.NewHealth >= _minHealth;
            
            if (!shouldHandle)
            {
                Debug.Log($"事件被过滤: 玩家生命值 {eventData.Data.NewHealth} 低于最小值 {_minHealth}");
            }
            
            return shouldHandle;
        }
    }
    
    /// <summary>
    /// 日志拦截器
    /// </summary>
    public class LoggingInterceptor<T> : IEventInterceptor<T> where T : IEvent
    {
        public int Priority => 0;
        
        public bool OnBeforeHandle(T eventData)
        {
            Debug.Log($"[拦截器] 开始处理事件: {eventData.GetType().Name} [{eventData.EventId}]");
            return true; // 继续处理
        }
        
        public void OnAfterHandle(T eventData)
        {
            Debug.Log($"[拦截器] 完成处理事件: {eventData.GetType().Name} [{eventData.EventId}]");
        }
        
        public bool OnException(T eventData, Exception exception)
        {
            Debug.LogError($"[拦截器] 事件处理异常: {eventData.GetType().Name} [{eventData.EventId}] - {exception.Message}");
            return false; // 不处理异常，继续抛出
        }
    }
    
    #endregion
}
