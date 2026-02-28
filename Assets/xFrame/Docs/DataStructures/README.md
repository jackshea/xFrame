# xFrame LRU Cache 系统

## 概述

xFrame LRU Cache 系统是一个高性能的缓存实现，采用最近最少使用（Least Recently Used）算法进行数据淘汰。该系统完全使用纯 C# 实现，支持泛型键值类型，提供 O(1) 时间复杂度的操作效率。

## 设计目标

- **高性能**：Get 和 Put 操作均为 O(1) 时间复杂度
- **内存高效**：自动淘汰最久未使用的数据，控制内存使用
- **线程安全**：提供可选的线程安全版本
- **易用性**：简洁的 API 设计，支持多种创建方式
- **泛型支持**：支持任意键值类型组合

## 核心组件

### 1. ILRUCache&lt;TKey, TValue&gt; 接口

LRU缓存的核心接口，定义了所有基本操作。

```csharp
public interface ILRUCache<TKey, TValue>
{
    int Capacity { get; }           // 缓存容量
    int Count { get; }              // 当前元素数量
    
    bool TryGet(TKey key, out TValue value);  // 安全获取
    TValue Get(TKey key);                     // 获取值
    void Put(TKey key, TValue value);         // 设置值
    bool ContainsKey(TKey key);               // 检查键是否存在
    bool Remove(TKey key);                    // 移除键值对
    void Clear();                             // 清空缓存
    
    IEnumerable<TKey> Keys { get; }           // 所有键
    IEnumerable<TValue> Values { get; }       // 所有值
}
```

### 2. LRUCache&lt;TKey, TValue&gt; 核心实现

使用双向链表和哈希表实现的高性能LRU缓存。

**特性：**
- O(1) 的 Get 和 Put 操作
- 自动 LRU 淘汰机制
- 支持容量控制
- 线程不安全（单线程使用）

### 3. ThreadSafeLRUCache&lt;TKey, TValue&gt; 线程安全版本

提供完全线程安全的LRU缓存实现。

**特性：**
- 使用读写锁保证线程安全
- 支持并发读写操作
- 实现 IDisposable 接口
- 适用于多线程环境

### 4. LRUCacheFactory 工厂类

提供便捷的缓存创建方法。

```csharp
// 基本创建方法
LRUCacheFactory.Create<TKey, TValue>(capacity)

// 常用类型的快速创建
LRUCacheFactory.CreateStringCache<TValue>(capacity)
LRUCacheFactory.CreateIntCache<TValue>(capacity)
LRUCacheFactory.CreateStringToStringCache(capacity)
```

## 快速开始

### 基本使用

```csharp
using xFrame.Core.DataStructures;

// 1. 创建LRU缓存
var cache = LRUCacheFactory.Create<int, string>(100);

// 2. 添加数据
cache.Put(1, "第一个值");
cache.Put(2, "第二个值");
cache.Put(3, "第三个值");

// 3. 获取数据
string value1 = cache.Get(1);              // 直接获取
bool found = cache.TryGet(2, out string value2);  // 安全获取

// 4. 检查和移除
bool exists = cache.ContainsKey(3);         // 检查是否存在
bool removed = cache.Remove(3);             // 移除数据

// 5. 清空缓存
cache.Clear();
```

### 线程安全使用

```csharp
// 创建线程安全的缓存
using (var threadSafeCache = new ThreadSafeLRUCache<string, object>(200))
{
    // 可以在多线程环境中安全使用
    threadSafeCache.Put("player_1", playerData);
    threadSafeCache.Put("config_timeout", 30);
    
    // 多线程并发访问
    Task.Run(() => {
        var player = threadSafeCache.Get("player_1");
        // 处理玩家数据
    });
    
    Task.Run(() => {
        threadSafeCache.Put("player_2", newPlayerData);
    });
}
```

## 详细使用指南

### 1. 创建缓存

#### 使用工厂方法（推荐）

```csharp
// 通用缓存
var cache = LRUCacheFactory.Create<int, PlayerData>(100);

// 字符串键缓存
var stringCache = LRUCacheFactory.CreateStringCache<ConfigData>(50);

// 整数键缓存
var intCache = LRUCacheFactory.CreateIntCache<GameItem>(200);

// 字符串到字符串缓存
var configCache = LRUCacheFactory.CreateStringToStringCache(30);

// 通用对象缓存
var objectCache = LRUCacheFactory.CreateStringToObjectCache(150);
```

#### 直接创建

```csharp
// 基本LRU缓存
var basicCache = new LRUCache<string, int>(100);

// 线程安全LRU缓存
var threadSafeCache = new ThreadSafeLRUCache<int, string>(200);
```

### 2. 数据操作

#### 添加和更新数据

```csharp
var cache = LRUCacheFactory.Create<string, PlayerData>(100);

// 添加新数据
var player1 = new PlayerData { Id = 1, Name = "玩家1", Level = 10 };
cache.Put("player_1", player1);

// 更新现有数据（会将该项移到最近使用位置）
var updatedPlayer = new PlayerData { Id = 1, Name = "玩家1", Level = 15 };
cache.Put("player_1", updatedPlayer);
```

#### 获取数据

```csharp
// 方法1：直接获取（可能抛出异常）
try
{
    PlayerData player = cache.Get("player_1");
    Console.WriteLine($"获取到玩家: {player.Name}");
}
catch (KeyNotFoundException)
{
    Console.WriteLine("玩家不存在");
}

// 方法2：安全获取（推荐）
if (cache.TryGet("player_1", out PlayerData player))
{
    Console.WriteLine($"获取到玩家: {player.Name}");
}
else
{
    Console.WriteLine("玩家不存在");
}
```

#### 检查和移除数据

```csharp
// 检查键是否存在
if (cache.ContainsKey("player_1"))
{
    Console.WriteLine("玩家1存在于缓存中");
}

// 移除数据
bool removed = cache.Remove("player_1");
if (removed)
{
    Console.WriteLine("玩家1已从缓存中移除");
}

// 清空所有数据
cache.Clear();
Console.WriteLine("缓存已清空");
```

### 3. 遍历缓存

```csharp
var cache = LRUCacheFactory.CreateStringCache<int>(10);

// 添加一些数据
cache.Put("a", 1);
cache.Put("b", 2);
cache.Put("c", 3);

// 访问某个数据，改变其在LRU中的位置
cache.Get("a");

// 遍历所有键（按最近使用顺序）
Console.WriteLine("所有键（按最近使用顺序）:");
foreach (string key in cache.Keys)
{
    Console.WriteLine($"  {key}");
}

// 遍历所有值（按最近使用顺序）
Console.WriteLine("所有值（按最近使用顺序）:");
foreach (int value in cache.Values)
{
    Console.WriteLine($"  {value}");
}

// 遍历键值对
foreach (string key in cache.Keys)
{
    int value = cache.Get(key);
    Console.WriteLine($"  {key} = {value}");
}
```

## 实际应用场景

### 1. 玩家数据缓存

```csharp
public class PlayerDataCache
{
    private readonly ILRUCache<int, PlayerData> _cache;
    
    public PlayerDataCache(int capacity = 1000)
    {
        _cache = LRUCacheFactory.CreateIntCache<PlayerData>(capacity);
    }
    
    public PlayerData GetPlayer(int playerId)
    {
        if (_cache.TryGet(playerId, out PlayerData player))
        {
            return player;
        }
        
        // 从数据库加载
        player = LoadPlayerFromDatabase(playerId);
        if (player != null)
        {
            _cache.Put(playerId, player);
        }
        
        return player;
    }
    
    public void UpdatePlayer(PlayerData player)
    {
        // 更新数据库
        SavePlayerToDatabase(player);
        
        // 更新缓存
        _cache.Put(player.Id, player);
    }
    
    private PlayerData LoadPlayerFromDatabase(int playerId)
    {
        // 模拟数据库加载
        Console.WriteLine($"从数据库加载玩家 {playerId}");
        return new PlayerData { Id = playerId, Name = $"Player_{playerId}" };
    }
    
    private void SavePlayerToDatabase(PlayerData player)
    {
        // 模拟数据库保存
        Console.WriteLine($"保存玩家 {player.Id} 到数据库");
    }
}
```

### 2. 配置缓存

```csharp
public class ConfigCache
{
    private readonly ILRUCache<string, string> _cache;
    
    public ConfigCache()
    {
        _cache = LRUCacheFactory.CreateStringToStringCache(100);
        LoadDefaultConfigs();
    }
    
    public string GetConfig(string key, string defaultValue = null)
    {
        if (_cache.TryGet(key, out string value))
        {
            return value;
        }
        
        // 从配置文件加载
        value = LoadConfigFromFile(key);
        if (value != null)
        {
            _cache.Put(key, value);
            return value;
        }
        
        return defaultValue;
    }
    
    public void SetConfig(string key, string value)
    {
        _cache.Put(key, value);
        SaveConfigToFile(key, value);
    }
    
    private void LoadDefaultConfigs()
    {
        _cache.Put("game.version", "1.0.0");
        _cache.Put("player.max_health", "100");
        _cache.Put("world.gravity", "-9.81");
    }
    
    private string LoadConfigFromFile(string key)
    {
        // 模拟从配置文件加载
        Console.WriteLine($"从配置文件加载: {key}");
        return null;
    }
    
    private void SaveConfigToFile(string key, string value)
    {
        // 模拟保存到配置文件
        Console.WriteLine($"保存配置: {key} = {value}");
    }
}
```

### 3. 资源缓存

```csharp
public class ResourceCache<T> where T : class
{
    private readonly ILRUCache<string, T> _cache;
    private readonly Func<string, T> _loader;
    
    public ResourceCache(int capacity, Func<string, T> loader)
    {
        _cache = LRUCacheFactory.CreateStringCache<T>(capacity);
        _loader = loader;
    }
    
    public T GetResource(string path)
    {
        if (_cache.TryGet(path, out T resource))
        {
            Console.WriteLine($"缓存命中: {path}");
            return resource;
        }
        
        Console.WriteLine($"加载资源: {path}");
        resource = _loader(path);
        
        if (resource != null)
        {
            _cache.Put(path, resource);
        }
        
        return resource;
    }
    
    public void PreloadResources(IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            GetResource(path);
        }
    }
    
    public void ClearCache()
    {
        _cache.Clear();
        Console.WriteLine("资源缓存已清空");
    }
    
    public string GetCacheStatus()
    {
        return $"资源缓存: {_cache.Count}/{_cache.Capacity}";
    }
}

// 使用示例
var textureCache = new ResourceCache<Texture2D>(50, path => 
{
    // 实际的纹理加载逻辑
    return Resources.Load<Texture2D>(path);
});

var audioCache = new ResourceCache<AudioClip>(30, path => 
{
    return Resources.Load<AudioClip>(path);
});
```

### 4. 多线程环境使用

```csharp
public class ThreadSafeDataCache
{
    private readonly ThreadSafeLRUCache<string, object> _cache;
    
    public ThreadSafeDataCache(int capacity = 500)
    {
        _cache = new ThreadSafeLRUCache<string, object>(capacity);
    }
    
    public void StartBackgroundTasks()
    {
        // 启动多个后台任务
        Task.Run(() => ProducerTask());
        Task.Run(() => ConsumerTask());
        Task.Run(() => CleanupTask());
    }
    
    private void ProducerTask()
    {
        for (int i = 0; i < 1000; i++)
        {
            string key = $"data_{i}";
            object value = new { Id = i, Timestamp = DateTime.Now };
            
            _cache.Put(key, value);
            Thread.Sleep(10);
        }
    }
    
    private void ConsumerTask()
    {
        var random = new Random();
        
        for (int i = 0; i < 500; i++)
        {
            string key = $"data_{random.Next(1000)}";
            
            if (_cache.TryGet(key, out object value))
            {
                Console.WriteLine($"消费数据: {key}");
            }
            
            Thread.Sleep(20);
        }
    }
    
    private void CleanupTask()
    {
        while (true)
        {
            Thread.Sleep(5000);
            
            int count = _cache.Count;
            if (count > _cache.Capacity * 0.8)
            {
                Console.WriteLine($"缓存使用率较高: {count}/{_cache.Capacity}");
            }
        }
    }
    
    public void Dispose()
    {
        _cache?.Dispose();
    }
}
```

## 性能优化建议

### 1. 选择合适的容量

```csharp
// 根据实际需求设置容量
// 太小：频繁淘汰，缓存命中率低
// 太大：占用过多内存

// 推荐设置为预期并发访问量的 1.2-1.5 倍
var playerCache = LRUCacheFactory.CreateIntCache<PlayerData>(1200); // 1000个玩家 * 1.2

// 监控缓存使用情况
Console.WriteLine($"缓存使用率: {cache.Count}/{cache.Capacity} ({cache.Count * 100.0 / cache.Capacity:F1}%)");
```

### 2. 预热缓存

```csharp
public void WarmUpCache()
{
    // 预加载热点数据
    string[] hotConfigs = { "game.version", "player.max_health", "world.gravity" };
    
    foreach (string config in hotConfigs)
    {
        configCache.GetConfig(config);
    }
    
    // 预加载常用玩家数据
    int[] activePlayerIds = GetActivePlayerIds();
    foreach (int playerId in activePlayerIds)
    {
        playerCache.GetPlayer(playerId);
    }
}
```

### 3. 监控缓存效果

```csharp
public class CacheMonitor
{
    private int _hitCount;
    private int _missCount;
    
    public void RecordHit()
    {
        Interlocked.Increment(ref _hitCount);
    }
    
    public void RecordMiss()
    {
        Interlocked.Increment(ref _missCount);
    }
    
    public double GetHitRate()
    {
        int total = _hitCount + _missCount;
        return total > 0 ? (double)_hitCount / total : 0;
    }
    
    public void PrintStatistics()
    {
        Console.WriteLine($"缓存命中率: {GetHitRate():P2} (命中: {_hitCount}, 未命中: {_missCount})");
    }
}
```

## 最佳实践

### 1. 选择合适的缓存类型

```csharp
// 单线程环境：使用基本LRU缓存
var singleThreadCache = LRUCacheFactory.Create<string, object>(100);

// 多线程环境：使用线程安全版本
using (var multiThreadCache = new ThreadSafeLRUCache<string, object>(100))
{
    // 多线程操作
}
```

### 2. 正确处理异常

```csharp
public T GetCachedData<T>(string key, Func<string, T> loader)
{
    try
    {
        if (cache.TryGet(key, out T value))
        {
            return value;
        }
        
        value = loader(key);
        if (value != null)
        {
            cache.Put(key, value);
        }
        
        return value;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"缓存操作失败: {ex.Message}");
        return default(T);
    }
}
```

### 3. 资源管理

```csharp
// 对于线程安全缓存，确保正确释放资源
public class CacheManager : IDisposable
{
    private readonly ThreadSafeLRUCache<string, object> _cache;
    private bool _disposed;
    
    public CacheManager(int capacity)
    {
        _cache = new ThreadSafeLRUCache<string, object>(capacity);
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _cache?.Dispose();
            _disposed = true;
        }
    }
}

// 使用using语句确保资源释放
using (var cacheManager = new CacheManager(100))
{
    // 使用缓存
}
```

## 常见问题

### Q: 什么时候使用LRU缓存？

A: 以下场景适合使用LRU缓存：
- 频繁访问的数据（如玩家信息、配置数据）
- 加载成本较高的资源（如纹理、音频文件）
- 需要控制内存使用的场景
- 存在明显热点数据的应用

### Q: 如何选择合适的缓存容量？

A: 容量选择建议：
- 分析数据访问模式，确定热点数据量
- 设置为热点数据量的 1.2-1.5 倍
- 监控缓存命中率，调整容量大小
- 考虑内存限制，避免过度占用

### Q: 线程安全版本的性能开销有多大？

A: 线程安全版本使用读写锁，会有一定性能开销：
- 读操作：约 10-20% 的性能开销
- 写操作：约 15-25% 的性能开销
- 只在多线程环境中使用线程安全版本

### Q: 如何提高缓存命中率？

A: 提高命中率的方法：
- 合理设置缓存容量
- 预热热点数据
- 分析访问模式，优化数据结构
- 使用合适的键设计

## 版本历史

- **v1.0.0**: 初始版本
  - 基本LRU缓存实现
  - 线程安全版本
  - 工厂类支持
  - 完整的单元测试

## 许可证

本LRU Cache系统是 xFrame 框架的一部分，遵循 xFrame 的许可证协议。
