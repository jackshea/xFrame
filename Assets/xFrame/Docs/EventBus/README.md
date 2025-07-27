# xFrame 事件总线系统

xFrame 事件总线系统是一个高性能、线程安全的发布-订阅模式实现，为游戏开发提供松耦合的组件间通信机制。

## 🚀 快速开始

### 基础使用

```csharp
using xFrame.Core.EventBus;

// 1. 创建事件总线
var eventBus = EventBusFactory.CreateThreadSafe();

// 2. 定义事件
public class PlayerHealthChangedEvent : BaseEvent<PlayerHealthData>
{
    public PlayerHealthChangedEvent(string playerId, int oldHealth, int newHealth)
    {
        Data = new PlayerHealthData
        {
            PlayerId = playerId,
            OldHealth = oldHealth,
            NewHealth = newHealth
        };
    }
}

// 3. 订阅事件
eventBus.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);

// 4. 发布事件
eventBus.Publish(new PlayerHealthChangedEvent("Player1", 100, 80));

// 事件处理方法
private void OnPlayerHealthChanged(PlayerHealthChangedEvent eventData)
{
    Debug.Log($"玩家 {eventData.Data.PlayerId} 生命值变化: {eventData.Data.OldHealth} -> {eventData.Data.NewHealth}");
}
```

## 📋 核心概念

### 事件 (Event)
事件是系统中发生的动作或状态变化的表示。所有事件都实现 `IEvent` 接口。

```csharp
// 简单事件
public class GameStartEvent : BaseEvent
{
    public GameStartEvent()
    {
        // 自动设置时间戳和事件ID
    }
}

// 带数据的事件
public class PlayerActionEvent : BaseEvent<PlayerActionData>
{
    public PlayerActionEvent(string actionType, string playerId)
    {
        Data = new PlayerActionData
        {
            ActionType = actionType,
            PlayerId = playerId,
            ActionTime = DateTime.Now
        };
    }
}
```

### 事件处理器 (Event Handler)
处理器负责响应特定类型的事件。

```csharp
// 使用委托订阅
eventBus.Subscribe<PlayerActionEvent>(eventData =>
{
    Debug.Log($"玩家操作: {eventData.Data.ActionType}");
});

// 使用处理器类
public class PlayerActionHandler : BaseEventHandler<PlayerActionEvent>
{
    public override int Priority => 10; // 高优先级
    
    public override void Handle(PlayerActionEvent eventData)
    {
        // 处理逻辑
        Debug.Log($"处理玩家操作: {eventData.Data.ActionType}");
    }
}

var handler = new PlayerActionHandler();
eventBus.Subscribe(handler);
```

## 🏗️ 创建事件总线

### 使用工厂方法

```csharp
// 标准事件总线
var standardBus = EventBusFactory.Create();

// 线程安全事件总线
var threadSafeBus = EventBusFactory.CreateThreadSafe();

// 高性能事件总线（禁用历史记录，增加并发数）
var highPerfBus = EventBusFactory.CreateHighPerformance();

// 调试模式事件总线（详细日志和历史记录）
var debugBus = EventBusFactory.CreateDebug();

// 轻量级事件总线（最小内存占用）
var lightweightBus = EventBusFactory.CreateLightweight();
```

### 使用构建器模式

```csharp
var customBus = new EventBusBuilder()
    .WithThreadSafety(true)                    // 启用线程安全
    .WithMaxConcurrentAsync(20)                // 最大并发异步处理数
    .WithHistory(true, 200)                    // 启用历史记录，最多200条
    .WithPerformanceMonitoring(true)           // 启用性能监控
    .WithDebugMode(false)                      // 禁用调试模式
    .Build();
```

### 使用预设配置

```csharp
// 高性能配置
var highPerfBus = new EventBusBuilder(EventBusConfig.HighPerformance).Build();

// 调试配置
var debugBus = new EventBusBuilder(EventBusConfig.Debug).Build();

// 轻量级配置
var lightBus = new EventBusBuilder(EventBusConfig.Lightweight).Build();
```

## 🌐 事件总线管理器

管理器提供全局的事件总线管理，支持多个命名的事件总线实例。

```csharp
var manager = EventBusManager.Instance;

// 使用预定义的事件总线
manager.Default.Subscribe<SystemStartupEvent>(OnSystemStartup);
manager.UI.Subscribe<UIClickEvent>(OnUIClick);
manager.Game.Subscribe<GameEvent>(OnGameEvent);
manager.Network.Subscribe<NetworkEvent>(OnNetworkEvent);

// 注册自定义事件总线
manager.RegisterEventBus("Audio", EventBusFactory.CreateHighPerformance());

// 使用自定义事件总线
manager.GetEventBus("Audio").Subscribe<AudioEvent>(OnAudioEvent);

// 广播事件到所有总线
manager.BroadcastToAll(new ErrorEvent("系统错误", ErrorLevel.Warning));

// 排除特定总线的广播
manager.BroadcastToAll(new LogEvent("调试信息"), EventBusManager.UIBusName);
```

## ⚡ 高级功能

### 异步事件处理

```csharp
// 订阅异步事件
eventBus.SubscribeAsync<PlayerActionEvent>(async eventData =>
{
    await SavePlayerActionToDatabase(eventData.Data);
    Debug.Log("玩家操作已保存到数据库");
});

// 异步发布事件
await eventBus.PublishAsync(new PlayerActionEvent("Jump", "Player1"));

// 异步处理器类
public class AsyncPlayerActionHandler : BaseAsyncEventHandler<PlayerActionEvent>
{
    public override async Task HandleAsync(PlayerActionEvent eventData)
    {
        await ProcessPlayerActionAsync(eventData.Data);
    }
}
```

### 事件优先级

```csharp
// 订阅时指定优先级（数值越小优先级越高）
eventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged_Critical, priority: -10);
eventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged_Normal, priority: 0);
eventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged_Low, priority: 10);

// 事件本身也可以有优先级
public class CriticalEvent : BaseEvent
{
    public override int Priority => -100; // 最高优先级
}
```

### 事件过滤器

```csharp
// 创建过滤器
public class HealthThresholdFilter : IEventFilter<PlayerHealthChangedEvent>
{
    private readonly int _minHealth;
    
    public HealthThresholdFilter(int minHealth)
    {
        _minHealth = minHealth;
    }
    
    public bool ShouldHandle(PlayerHealthChangedEvent eventData)
    {
        return eventData.Data.NewHealth >= _minHealth;
    }
}

// 添加过滤器
var filter = new HealthThresholdFilter(20);
eventBus.AddFilter<PlayerHealthChangedEvent>(filter);
```

### 事件拦截器

```csharp
// 创建拦截器
public class LoggingInterceptor<T> : IEventInterceptor<T> where T : IEvent
{
    public int Priority => 0;
    
    public bool OnBeforeHandle(T eventData)
    {
        Debug.Log($"开始处理事件: {eventData.GetType().Name}");
        return true; // 继续处理
    }
    
    public void OnAfterHandle(T eventData)
    {
        Debug.Log($"完成处理事件: {eventData.GetType().Name}");
    }
    
    public bool OnException(T eventData, Exception exception)
    {
        Debug.LogError($"事件处理异常: {exception.Message}");
        return false; // 不处理异常，继续抛出
    }
}

// 添加拦截器
var interceptor = new LoggingInterceptor<PlayerHealthChangedEvent>();
eventBus.AddInterceptor<PlayerHealthChangedEvent>(interceptor);
```

### 批量事件处理

```csharp
// 创建批量事件
var batchEvents = new[]
{
    new LogEvent("系统启动", LogLevel.Info),
    new LogEvent("加载配置", LogLevel.Info),
    new LogEvent("初始化完成", LogLevel.Info)
};

// 批量发布
eventBus.PublishBatch(batchEvents);
```

### 延迟事件发布

```csharp
// 延迟2秒发布事件
var delayedEvent = new PlayerActionEvent("AutoSave", "System");
eventBus.PublishDelayed(delayedEvent, 2000);
```

### 事件历史记录

```csharp
// 启用历史记录
eventBus.SetHistoryEnabled(true);

// 获取最近的事件历史
var recentEvents = eventBus.GetEventHistory<PlayerActionEvent>(10);
foreach (var evt in recentEvents)
{
    Debug.Log($"历史事件: {evt.Data.ActionType} at {evt.Timestamp}");
}
```

## 🧵 线程安全

### 基础线程安全

```csharp
// 创建线程安全的事件总线
var threadSafeBus = EventBusFactory.CreateThreadSafe();

// 多线程环境下安全使用
Task.Run(() => threadSafeBus.Publish(new GameEvent("BackgroundTask")));
Task.Run(() => threadSafeBus.Subscribe<GameEvent>(OnGameEvent));
```

### 主线程调度

```csharp
var threadSafeBus = new ThreadSafeEventBus();

// 订阅到主线程执行的事件（Unity UI更新等）
threadSafeBus.SubscribeOnMainThread<UIUpdateEvent>(eventData =>
{
    // 这里的代码会在主线程执行
    UpdateUI(eventData.Data);
});

// 在Unity的Update方法中处理主线程队列
void Update()
{
    threadSafeBus.ProcessMainThreadQueue();
}

// 发布事件到主线程
await threadSafeBus.PublishOnMainThread(new UIUpdateEvent("PlayerHealth", 80));
```

## 📊 性能优化

### 对象池集成

事件总线自动集成了xFrame的对象池系统，减少GC压力：

```csharp
// 事件队列项会自动使用对象池
// 无需手动管理，系统会自动复用对象
```

### LRU缓存优化

事件处理器注册表使用LRU缓存优化查找性能：

```csharp
// 频繁使用的处理器会被缓存
// 提高事件分发效率
```

### 性能监控

```csharp
// 获取性能统计
var stats = eventBus.GetStatistics();
Debug.Log($"事件总线统计: {stats}");

// 管理器统计
var managerStats = EventBusManager.Instance.GetManagerStatistics();
Debug.Log($"管理器统计: {managerStats}");
```

## 🎮 Unity 集成示例

### MonoBehaviour 集成

```csharp
public class GameManager : MonoBehaviour
{
    private IEventBus _eventBus;
    private string _healthSubscriptionId;
    
    void Start()
    {
        // 使用全局管理器
        _eventBus = EventBusManager.Instance.Game;
        
        // 订阅事件
        _healthSubscriptionId = _eventBus.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        
        // 订阅系统事件
        _eventBus.Subscribe<SystemPauseEvent>(OnSystemPause);
        _eventBus.Subscribe<SystemResumeEvent>(OnSystemResume);
    }
    
    void OnDestroy()
    {
        // 清理订阅
        _eventBus?.Unsubscribe(_healthSubscriptionId);
    }
    
    private void OnPlayerHealthChanged(PlayerHealthChangedEvent eventData)
    {
        // 更新UI
        UpdateHealthBar(eventData.Data.NewHealth);
    }
    
    private void OnSystemPause(SystemPauseEvent eventData)
    {
        Time.timeScale = 0;
    }
    
    private void OnSystemResume(SystemResumeEvent eventData)
    {
        Time.timeScale = 1;
    }
}
```

### 玩家控制器示例

```csharp
public class PlayerController : MonoBehaviour
{
    private IEventBus _eventBus;
    
    void Start()
    {
        _eventBus = EventBusManager.Instance.Game;
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 发布跳跃事件
            _eventBus.Publish(new PlayerActionEvent("Jump", gameObject.name));
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            // 发布交互事件
            _eventBus.Publish(new PlayerActionEvent("Interact", gameObject.name));
        }
    }
    
    public void TakeDamage(int damage)
    {
        var newHealth = currentHealth - damage;
        
        // 发布生命值变化事件
        _eventBus.Publish(new PlayerHealthChangedEvent(gameObject.name, currentHealth, newHealth));
        
        currentHealth = newHealth;
        
        if (currentHealth <= 0)
        {
            // 发布死亡事件
            _eventBus.Publish(new PlayerDeathEvent(gameObject.name, "敌人攻击"));
        }
    }
}
```

## 🔧 常用事件类型

系统提供了一些常用的事件类型：

```csharp
// 系统事件
new SystemStartupEvent(args);
new SystemShutdownEvent("正常关闭");
new SystemPauseEvent("用户暂停");
new SystemResumeEvent("用户恢复");

// 错误和日志事件
new ErrorEvent("发生错误", ErrorLevel.Error, "PlayerController");
new LogEvent("调试信息", LogLevel.Debug, "GameManager");

// 配置变更事件
new ConfigChangedEvent("Graphics.Quality", "High", "Ultra");

// 用户操作事件
new UserActionEvent("ButtonClick", "MainMenu", new { ButtonName = "Start" });

// 性能监控事件
new PerformanceEvent("FPS", 60.0, "frames/second");
```

## 🚨 最佳实践

### 1. 事件命名规范

```csharp
// 使用描述性的名称，以Event结尾
public class PlayerHealthChangedEvent : BaseEvent<PlayerHealthData> { }
public class GameStateChangedEvent : BaseEvent<GameStateData> { }
public class UIButtonClickedEvent : BaseEvent<UIButtonData> { }
```

### 2. 避免循环依赖

```csharp
// ❌ 错误：在事件处理器中发布相同类型的事件可能导致无限循环
eventBus.Subscribe<PlayerHealthChangedEvent>(eventData =>
{
    eventBus.Publish(new PlayerHealthChangedEvent(...)); // 危险！
});

// ✅ 正确：使用不同的事件类型或添加条件检查
eventBus.Subscribe<PlayerHealthChangedEvent>(eventData =>
{
    if (eventData.Data.NewHealth <= 0)
    {
        eventBus.Publish(new PlayerDeathEvent(...)); // 安全
    }
});
```

### 3. 及时清理订阅

```csharp
public class GameComponent : MonoBehaviour
{
    private string _subscriptionId;
    
    void Start()
    {
        _subscriptionId = EventBusManager.Instance.Game.Subscribe<GameEvent>(OnGameEvent);
    }
    
    void OnDestroy()
    {
        // 重要：清理订阅避免内存泄漏
        EventBusManager.Instance.Game.Unsubscribe(_subscriptionId);
    }
}
```

### 4. 合理使用优先级

```csharp
// 关键系统使用高优先级（负数）
eventBus.Subscribe<ErrorEvent>(OnCriticalError, priority: -100);

// 普通逻辑使用默认优先级（0）
eventBus.Subscribe<GameEvent>(OnGameEvent);

// 非关键功能使用低优先级（正数）
eventBus.Subscribe<LogEvent>(OnLogEvent, priority: 100);
```

### 5. 异步处理耗时操作

```csharp
// ✅ 对于耗时操作使用异步处理
eventBus.SubscribeAsync<PlayerDataChangedEvent>(async eventData =>
{
    await SavePlayerDataToDatabase(eventData.Data);
});

// ❌ 避免在同步处理器中执行耗时操作
eventBus.Subscribe<PlayerDataChangedEvent>(eventData =>
{
    SavePlayerDataToDatabase(eventData.Data); // 会阻塞主线程
});
```

## 🧪 测试

系统提供了完整的单元测试，确保功能正确性：

```csharp
// 运行测试
// Unity Test Runner -> EditMode Tests -> xFrame.Tests.EditMode.EventBusTests

// 测试覆盖：
// - EventBusTests: 核心功能测试
// - ThreadSafeEventBusTests: 线程安全测试  
// - EventBusManagerTests: 管理器测试
```

## 📈 性能指标

- **事件发布**: O(n) 其中n为订阅者数量
- **事件订阅**: O(log n) 由于优先级排序
- **处理器查找**: O(1) 得益于LRU缓存优化
- **内存使用**: 通过对象池优化，减少GC压力
- **并发性能**: 使用读写锁优化，支持高并发场景

## 🔗 相关文档

- [对象池系统文档](../ObjectPool/README.md)
- [LRU缓存系统文档](../DataStructures/README.md)
- [xFrame框架总览](../../README.md)

## 📝 更新日志

### v1.0.0
- 初始版本发布
- 实现核心事件总线功能
- 支持线程安全操作
- 集成对象池和LRU缓存优化
- 提供完整的单元测试覆盖

---

**xFrame 事件总线系统** - 为你的游戏提供强大的事件驱动架构支持！
