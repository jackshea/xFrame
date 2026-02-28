# LRU Cache 快速开始指南

本指南将帮助您在5分钟内快速上手使用xFrame LRU Cache系统。

## 第一步：引入命名空间

```csharp
using xFrame.Core.DataStructures;
```

## 第二步：创建您的第一个缓存

### 方式1：使用工厂方法（推荐）

```csharp
// 创建一个容量为100的字符串到整数的缓存
var cache = LRUCacheFactory.Create<string, int>(100);
```

### 方式2：直接实例化

```csharp
// 创建基本LRU缓存
var cache = new LRUCache<string, int>(100);

// 或创建线程安全版本
var threadSafeCache = new ThreadSafeLRUCache<string, int>(100);
```

## 第三步：基本操作

### 添加数据

```csharp
cache.Put("apple", 5);
cache.Put("banana", 3);
cache.Put("orange", 8);
```

### 获取数据

```csharp
// 方式1：直接获取（推荐用于确定存在的数据）
int appleCount = cache.Get("apple");
Console.WriteLine($"苹果数量: {appleCount}");

// 方式2：安全获取（推荐用于可能不存在的数据）
if (cache.TryGet("grape", out int grapeCount))
{
    Console.WriteLine($"葡萄数量: {grapeCount}");
}
else
{
    Console.WriteLine("没有葡萄");
}
```

### 检查和移除

```csharp
// 检查是否存在
if (cache.ContainsKey("banana"))
{
    Console.WriteLine("有香蕉库存");
}

// 移除数据
bool removed = cache.Remove("orange");
if (removed)
{
    Console.WriteLine("橙子已售完");
}
```

## 第四步：完整示例

让我们创建一个简单的商品库存管理系统：

```csharp
using System;
using xFrame.Core.DataStructures;

public class InventoryManager
{
    private readonly ILRUCache<string, int> _inventory;
    
    public InventoryManager()
    {
        // 创建容量为50的库存缓存
        _inventory = LRUCacheFactory.CreateStringCache<int>(50);
        
        // 初始化一些商品
        InitializeInventory();
    }
    
    private void InitializeInventory()
    {
        _inventory.Put("苹果", 100);
        _inventory.Put("香蕉", 50);
        _inventory.Put("橙子", 75);
        _inventory.Put("葡萄", 30);
    }
    
    public void AddStock(string product, int quantity)
    {
        if (_inventory.TryGet(product, out int currentStock))
        {
            _inventory.Put(product, currentStock + quantity);
            Console.WriteLine($"{product} 库存增加 {quantity}，当前库存: {currentStock + quantity}");
        }
        else
        {
            _inventory.Put(product, quantity);
            Console.WriteLine($"新商品 {product} 入库，数量: {quantity}");
        }
    }
    
    public bool SellProduct(string product, int quantity)
    {
        if (_inventory.TryGet(product, out int currentStock))
        {
            if (currentStock >= quantity)
            {
                _inventory.Put(product, currentStock - quantity);
                Console.WriteLine($"售出 {product} {quantity} 个，剩余库存: {currentStock - quantity}");
                return true;
            }
            else
            {
                Console.WriteLine($"{product} 库存不足，当前库存: {currentStock}，需要: {quantity}");
                return false;
            }
        }
        else
        {
            Console.WriteLine($"商品 {product} 不存在");
            return false;
        }
    }
    
    public void CheckStock(string product)
    {
        if (_inventory.TryGet(product, out int stock))
        {
            Console.WriteLine($"{product} 当前库存: {stock}");
        }
        else
        {
            Console.WriteLine($"{product} 暂无库存");
        }
    }
    
    public void ShowAllInventory()
    {
        Console.WriteLine("=== 当前库存情况 ===");
        foreach (string product in _inventory.Keys)
        {
            int stock = _inventory.Get(product);
            Console.WriteLine($"{product}: {stock}");
        }
        Console.WriteLine($"总商品种类: {_inventory.Count}");
    }
}

// 使用示例
class Program
{
    static void Main()
    {
        var inventory = new InventoryManager();
        
        // 查看初始库存
        inventory.ShowAllInventory();
        
        // 售卖商品
        inventory.SellProduct("苹果", 20);
        inventory.SellProduct("香蕉", 10);
        
        // 补充库存
        inventory.AddStock("苹果", 50);
        inventory.AddStock("草莓", 25);
        
        // 检查特定商品
        inventory.CheckStock("葡萄");
        inventory.CheckStock("西瓜");
        
        // 查看最终库存
        inventory.ShowAllInventory();
    }
}
```

运行结果：
```
=== 当前库存情况 ===
苹果: 100
香蕉: 50
橙子: 75
葡萄: 30
总商品种类: 4
售出 苹果 20 个，剩余库存: 80
售出 香蕉 10 个，剩余库存: 40
苹果 库存增加 50，当前库存: 130
新商品 草莓 入库，数量: 25
葡萄 当前库存: 30
西瓜 暂无库存
=== 当前库存情况 ===
苹果: 130
香蕉: 40
橙子: 75
葡萄: 30
草莓: 25
总商品种类: 5
```

## 第五步：线程安全使用

如果您的应用是多线程的，请使用线程安全版本：

```csharp
using System;
using System.Threading.Tasks;
using xFrame.Core.DataStructures;

public class ThreadSafeExample
{
    private readonly ThreadSafeLRUCache<string, int> _cache;
    
    public ThreadSafeExample()
    {
        _cache = new ThreadSafeLRUCache<string, int>(100);
    }
    
    public void RunConcurrentOperations()
    {
        // 启动多个并发任务
        var tasks = new Task[]
        {
            Task.Run(() => ProducerTask("Producer1")),
            Task.Run(() => ProducerTask("Producer2")),
            Task.Run(() => ConsumerTask("Consumer1")),
            Task.Run(() => ConsumerTask("Consumer2"))
        };
        
        Task.WaitAll(tasks);
        
        Console.WriteLine($"最终缓存大小: {_cache.Count}");
    }
    
    private void ProducerTask(string name)
    {
        for (int i = 0; i < 50; i++)
        {
            string key = $"{name}_item_{i}";
            _cache.Put(key, i);
            Console.WriteLine($"{name} 添加: {key} = {i}");
            
            // 模拟一些处理时间
            Task.Delay(10).Wait();
        }
    }
    
    private void ConsumerTask(string name)
    {
        var random = new Random();
        
        for (int i = 0; i < 30; i++)
        {
            string key = $"Producer{random.Next(1, 3)}_item_{random.Next(50)}";
            
            if (_cache.TryGet(key, out int value))
            {
                Console.WriteLine($"{name} 读取: {key} = {value}");
            }
            else
            {
                Console.WriteLine($"{name} 未找到: {key}");
            }
            
            Task.Delay(15).Wait();
        }
    }
    
    // 确保释放资源
    public void Dispose()
    {
        _cache?.Dispose();
    }
}

// 使用示例
class ThreadSafeProgram
{
    static void Main()
    {
        var example = new ThreadSafeExample();
        
        try
        {
            example.RunConcurrentOperations();
        }
        finally
        {
            example.Dispose();
        }
    }
}
```

## 第六步：常用工厂方法

为了更方便地创建常用类型的缓存，可以使用以下工厂方法：

```csharp
// 字符串键的缓存
var playerCache = LRUCacheFactory.CreateStringCache<PlayerData>(100);
var configCache = LRUCacheFactory.CreateStringToStringCache(50);

// 整数键的缓存
var itemCache = LRUCacheFactory.CreateIntCache<GameItem>(200);

// 通用对象缓存
var objectCache = LRUCacheFactory.CreateStringToObjectCache(150);

// 使用示例
playerCache.Put("player_001", new PlayerData { Name = "张三", Level = 10 });
configCache.Put("max_players", "100");
itemCache.Put(1001, new GameItem { Name = "铁剑", Damage = 25 });
objectCache.Put("temp_data", new { Value = 42, Timestamp = DateTime.Now });
```

## 第七步：监控缓存状态

了解缓存的使用情况对于性能优化很重要：

```csharp
public class CacheMonitor
{
    private readonly ILRUCache<string, object> _cache;
    
    public CacheMonitor(int capacity)
    {
        _cache = LRUCacheFactory.CreateStringToObjectCache(capacity);
    }
    
    public void ShowCacheStatus()
    {
        Console.WriteLine("=== 缓存状态 ===");
        Console.WriteLine($"容量: {_cache.Capacity}");
        Console.WriteLine($"当前大小: {_cache.Count}");
        Console.WriteLine($"使用率: {(double)_cache.Count / _cache.Capacity:P2}");
        
        if (_cache.Count > 0)
        {
            Console.WriteLine("最近使用的键:");
            int showCount = Math.Min(5, _cache.Count);
            int index = 0;
            foreach (string key in _cache.Keys)
            {
                Console.WriteLine($"  {index + 1}. {key}");
                if (++index >= showCount) break;
            }
        }
    }
    
    public void TestCachePerformance()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // 写入测试
        for (int i = 0; i < 1000; i++)
        {
            _cache.Put($"key_{i}", $"value_{i}");
        }
        
        long writeTime = stopwatch.ElapsedMilliseconds;
        stopwatch.Restart();
        
        // 读取测试
        for (int i = 0; i < 1000; i++)
        {
            _cache.TryGet($"key_{i}", out object value);
        }
        
        long readTime = stopwatch.ElapsedMilliseconds;
        stopwatch.Stop();
        
        Console.WriteLine($"写入1000项耗时: {writeTime}ms");
        Console.WriteLine($"读取1000项耗时: {readTime}ms");
    }
}
```

## 第八步：最佳实践

### 1. 选择合适的容量

```csharp
// 根据实际需求设置容量
// 经验法则：设置为预期热点数据量的1.2-1.5倍
var cache = LRUCacheFactory.Create<string, object>(120); // 100个热点数据 * 1.2
```

### 2. 使用合适的键类型

```csharp
// 好的做法：使用简单、可比较的键类型
var goodCache = LRUCacheFactory.Create<string, PlayerData>(100);
var alsoGoodCache = LRUCacheFactory.Create<int, GameItem>(100);

// 避免：复杂对象作为键（除非实现了正确的GetHashCode和Equals）
// var badCache = LRUCacheFactory.Create<ComplexObject, string>(100);
```

### 3. 异常处理

```csharp
public T SafeGetFromCache<T>(ILRUCache<string, T> cache, string key, Func<string, T> fallback)
{
    try
    {
        if (cache.TryGet(key, out T value))
        {
            return value;
        }
        
        // 缓存未命中，使用回退方法
        value = fallback(key);
        if (value != null)
        {
            cache.Put(key, value);
        }
        
        return value;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"缓存操作失败: {ex.Message}");
        return fallback(key);
    }
}
```

## 下一步

现在您已经掌握了LRU Cache的基本使用方法！接下来可以：

1. 查看 [API参考文档](API_Reference.md) 了解详细的方法说明
2. 阅读 [完整文档](README.md) 了解高级特性和最佳实践
3. 查看 `Assets/xFrame/Examples/LRUCacheExample.cs` 了解更多实际使用场景
4. 运行单元测试了解系统的各种功能

## 常见问题

**Q: 什么时候使用线程安全版本？**
A: 当您的应用有多个线程同时访问同一个缓存时，使用 `ThreadSafeLRUCache`。

**Q: 如何选择合适的容量？**
A: 分析您的数据访问模式，设置为热点数据量的1.2-1.5倍。

**Q: 缓存满了会怎样？**
A: 会自动淘汰最久未使用的数据，为新数据腾出空间。

**Q: 可以存储null值吗？**
A: 可以，但建议避免，因为可能会与"键不存在"的情况混淆。

祝您使用愉快！如有问题，请查看完整文档或联系开发团队。
