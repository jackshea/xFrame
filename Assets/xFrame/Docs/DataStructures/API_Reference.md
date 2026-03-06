# LRU Cache API 参考文档

## 命名空间

```csharp
using xFrame.Core.DataStructures;
```

## 接口

### ILRUCache&lt;TKey, TValue&gt;

LRU缓存的核心接口，定义了所有基本操作。

#### 属性

| 属性 | 类型 | 描述 |
|------|------|------|
| `Capacity` | `int` | 缓存的最大容量 |
| `Count` | `int` | 当前缓存中的元素数量 |
| `Keys` | `IEnumerable<TKey>` | 所有键的集合（按最近使用顺序） |
| `Values` | `IEnumerable<TValue>` | 所有值的集合（按最近使用顺序） |

#### 方法

##### TryGet(TKey key, out TValue value)

安全地获取缓存值，不会抛出异常。

```csharp
bool TryGet(TKey key, out TValue value)
```

**参数：**
- `key`: 要查找的键
- `value`: 输出参数，如果找到则包含对应的值

**返回值：**
- `bool`: 如果找到键则返回 `true`，否则返回 `false`

**示例：**
```csharp
if (cache.TryGet("player_1", out PlayerData player))
{
    Console.WriteLine($"找到玩家: {player.Name}");
}
else
{
    Console.WriteLine("玩家不存在");
}
```

##### Get(TKey key)

获取指定键的值。

```csharp
TValue Get(TKey key)
```

**参数：**
- `key`: 要查找的键

**返回值：**
- `TValue`: 对应的值

**异常：**
- `KeyNotFoundException`: 当键不存在时抛出

**示例：**
```csharp
try
{
    string value = cache.Get("config_key");
    Console.WriteLine($"配置值: {value}");
}
catch (KeyNotFoundException)
{
    Console.WriteLine("配置不存在");
}
```

##### Put(TKey key, TValue value)

设置键值对。如果键已存在，则更新值；如果不存在，则添加新的键值对。

```csharp
void Put(TKey key, TValue value)
```

**参数：**
- `key`: 键
- `value`: 值

**行为：**
- 如果缓存已满，会自动淘汰最久未使用的项
- 更新现有键会将其移动到最近使用位置

**示例：**
```csharp
cache.Put("player_1", new PlayerData { Name = "张三", Level = 10 });
cache.Put("player_1", new PlayerData { Name = "张三", Level = 15 }); // 更新
```

##### ContainsKey(TKey key)

检查缓存中是否包含指定的键。

```csharp
bool ContainsKey(TKey key)
```

**参数：**
- `key`: 要检查的键

**返回值：**
- `bool`: 如果包含键则返回 `true`，否则返回 `false`

**注意：** 此操作不会影响LRU顺序。

**示例：**
```csharp
if (cache.ContainsKey("player_1"))
{
    Console.WriteLine("玩家1存在于缓存中");
}
```

##### Remove(TKey key)

从缓存中移除指定的键值对。

```csharp
bool Remove(TKey key)
```

**参数：**
- `key`: 要移除的键

**返回值：**
- `bool`: 如果成功移除则返回 `true`，如果键不存在则返回 `false`

**示例：**
```csharp
bool removed = cache.Remove("player_1");
if (removed)
{
    Console.WriteLine("玩家1已移除");
}
```

##### Clear()

清空缓存中的所有元素。

```csharp
void Clear()
```

**示例：**
```csharp
cache.Clear();
Console.WriteLine($"缓存已清空，当前元素数量: {cache.Count}");
```

---

## 实现类

### LRUCache&lt;TKey, TValue&gt;

基本的LRU缓存实现，线程不安全，适用于单线程环境。

#### 构造函数

```csharp
public LRUCache(int capacity)
```

**参数：**
- `capacity`: 缓存容量，必须大于0

**异常：**
- `ArgumentException`: 当容量小于等于0时抛出

**示例：**
```csharp
var cache = new LRUCache<string, int>(100);
```

#### 性能特性

- **时间复杂度**: Get和Put操作均为O(1)
- **空间复杂度**: O(capacity)
- **线程安全**: 否

---

### ThreadSafeLRUCache&lt;TKey, TValue&gt;

线程安全的LRU缓存实现，适用于多线程环境。

#### 构造函数

```csharp
public ThreadSafeLRUCache(int capacity)
```

**参数：**
- `capacity`: 缓存容量，必须大于0

**异常：**
- `ArgumentException`: 当容量小于等于0时抛出

**示例：**
```csharp
using (var cache = new ThreadSafeLRUCache<string, object>(200))
{
    // 多线程安全使用
    cache.Put("key", value);
}
```

#### 实现的接口

- `ILRUCache<TKey, TValue>`
- `IDisposable`

#### 线程安全机制

使用 `ReaderWriterLockSlim` 实现线程安全：
- **读操作** (Get, TryGet, ContainsKey, Count, Keys, Values): 使用读锁
- **写操作** (Put, Remove, Clear): 使用写锁

#### Dispose()

释放锁资源。

```csharp
public void Dispose()
```

**注意：** 使用 `using` 语句确保正确释放资源。

#### 性能特性

- **时间复杂度**: Get和Put操作均为O(1)（不包括锁开销）
- **空间复杂度**: O(capacity)
- **线程安全**: 是
- **性能开销**: 相比非线程安全版本有10-25%的性能开销

---

## 工厂类

### LRUCacheFactory

提供便捷的缓存创建方法。

#### Create&lt;TKey, TValue&gt;(int capacity)

创建基本的LRU缓存。

```csharp
public static ILRUCache<TKey, TValue> Create<TKey, TValue>(int capacity)
```

**参数：**
- `capacity`: 缓存容量

**返回值：**
- `ILRUCache<TKey, TValue>`: LRU缓存实例

**示例：**
```csharp
var cache = LRUCacheFactory.Create<int, string>(100);
```

#### CreateStringCache&lt;TValue&gt;(int capacity)

创建以字符串为键的缓存。

```csharp
public static ILRUCache<string, TValue> CreateStringCache<TValue>(int capacity)
```

**参数：**
- `capacity`: 缓存容量

**返回值：**
- `ILRUCache<string, TValue>`: 字符串键缓存实例

**示例：**
```csharp
var playerCache = LRUCacheFactory.CreateStringCache<PlayerData>(50);
```

#### CreateIntCache&lt;TValue&gt;(int capacity)

创建以整数为键的缓存。

```csharp
public static ILRUCache<int, TValue> CreateIntCache<TValue>(int capacity)
```

**参数：**
- `capacity`: 缓存容量

**返回值：**
- `ILRUCache<int, TValue>`: 整数键缓存实例

**示例：**
```csharp
var itemCache = LRUCacheFactory.CreateIntCache<GameItem>(200);
```

#### CreateStringToStringCache(int capacity)

创建字符串到字符串的缓存。

```csharp
public static ILRUCache<string, string> CreateStringToStringCache(int capacity)
```

**参数：**
- `capacity`: 缓存容量

**返回值：**
- `ILRUCache<string, string>`: 字符串缓存实例

**示例：**
```csharp
var configCache = LRUCacheFactory.CreateStringToStringCache(30);
```

#### CreateStringToObjectCache(int capacity)

创建字符串到对象的缓存。

```csharp
public static ILRUCache<string, object> CreateStringToObjectCache(int capacity)
```

**参数：**
- `capacity`: 缓存容量

**返回值：**
- `ILRUCache<string, object>`: 通用对象缓存实例

**示例：**
```csharp
var objectCache = LRUCacheFactory.CreateStringToObjectCache(100);
```

---

## 使用模式

### 1. 基本缓存模式

```csharp
public class DataService
{
    private readonly ILRUCache<int, Data> _cache;
    
    public DataService()
    {
        _cache = LRUCacheFactory.CreateIntCache<Data>(100);
    }
    
    public Data GetData(int id)
    {
        if (_cache.TryGet(id, out Data data))
        {
            return data; // 缓存命中
        }
        
        // 从数据源加载
        data = LoadFromDataSource(id);
        if (data != null)
        {
            _cache.Put(id, data);
        }
        
        return data;
    }
}
```

### 2. 写透缓存模式

```csharp
public class WriteThoughCache
{
    private readonly ILRUCache<string, object> _cache;
    
    public WriteThoughCache()
    {
        _cache = LRUCacheFactory.CreateStringToObjectCache(50);
    }
    
    public void SetData(string key, object value)
    {
        // 同时写入缓存和持久化存储
        _cache.Put(key, value);
        SaveToPersistentStorage(key, value);
    }
    
    public object GetData(string key)
    {
        if (_cache.TryGet(key, out object value))
        {
            return value;
        }
        
        value = LoadFromPersistentStorage(key);
        if (value != null)
        {
            _cache.Put(key, value);
        }
        
        return value;
    }
}
```

### 3. 延迟加载模式

```csharp
public class LazyLoadCache<TKey, TValue>
{
    private readonly ILRUCache<TKey, TValue> _cache;
    private readonly Func<TKey, TValue> _loader;
    
    public LazyLoadCache(int capacity, Func<TKey, TValue> loader)
    {
        _cache = LRUCacheFactory.Create<TKey, TValue>(capacity);
        _loader = loader;
    }
    
    public TValue Get(TKey key)
    {
        if (_cache.TryGet(key, out TValue value))
        {
            return value;
        }
        
        value = _loader(key);
        if (value != null)
        {
            _cache.Put(key, value);
        }
        
        return value;
    }
}
```

### 4. 多线程安全模式

```csharp
public class ThreadSafeDataCache : IDisposable
{
    private readonly ThreadSafeLRUCache<string, object> _cache;
    private bool _disposed;
    
    public ThreadSafeDataCache(int capacity)
    {
        _cache = new ThreadSafeLRUCache<string, object>(capacity);
    }
    
    public T Get<T>(string key)
    {
        if (_cache.TryGet(key, out object value) && value is T)
        {
            return (T)value;
        }
        
        return default(T);
    }
    
    public void Put<T>(string key, T value)
    {
        _cache.Put(key, value);
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
```

---

## 异常处理

### 常见异常

| 异常类型 | 触发条件 | 处理建议 |
|----------|----------|----------|
| `ArgumentException` | 构造函数传入无效容量 | 确保容量大于0 |
| `KeyNotFoundException` | 调用Get方法时键不存在 | 使用TryGet方法替代 |
| `ObjectDisposedException` | 在已释放的ThreadSafeLRUCache上操作 | 检查对象生命周期 |

### 异常处理示例

```csharp
public class SafeCacheWrapper<TKey, TValue>
{
    private readonly ILRUCache<TKey, TValue> _cache;
    
    public SafeCacheWrapper(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentException("容量必须大于0", nameof(capacity));
        }
        
        _cache = LRUCacheFactory.Create<TKey, TValue>(capacity);
    }
    
    public TValue SafeGet(TKey key, TValue defaultValue = default(TValue))
    {
        try
        {
            return _cache.TryGet(key, out TValue value) ? value : defaultValue;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"缓存获取失败: {ex.Message}");
            return defaultValue;
        }
    }
    
    public bool SafePut(TKey key, TValue value)
    {
        try
        {
            _cache.Put(key, value);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"缓存设置失败: {ex.Message}");
            return false;
        }
    }
}
```

---

## 性能考虑

### 时间复杂度

| 操作 | LRUCache | ThreadSafeLRUCache |
|------|----------|-------------------|
| Get | O(1) | O(1) + 锁开销 |
| Put | O(1) | O(1) + 锁开销 |
| Remove | O(1) | O(1) + 锁开销 |
| ContainsKey | O(1) | O(1) + 锁开销 |
| Clear | O(n) | O(n) + 锁开销 |

### 内存使用

- **基本开销**: 每个缓存项需要额外的链表节点开销
- **哈希表开销**: 内部使用Dictionary，有负载因子相关的内存开销
- **链表开销**: 双向链表的前后指针开销

### 性能优化建议

1. **选择合适的容量**：避免过度淘汰或内存浪费
2. **预热缓存**：提前加载热点数据
3. **监控命中率**：调整缓存策略
4. **合理使用线程安全版本**：只在必要时使用

---

## 版本兼容性

- **.NET Framework**: 4.7.1+
- **.NET Core**: 2.0+
- **.NET 5/6/7/8**: 完全支持
- **Unity**: 2021.3+ (IL2CPP兼容)

---

## 相关类型

### 内部类型

以下类型为内部实现，不应直接使用：

- `LRUCache<TKey, TValue>.Node`: 链表节点
- `ThreadSafeLRUCache<TKey, TValue>._lock`: 读写锁

### 依赖类型

- `System.Collections.Generic.Dictionary<TKey, TValue>`
- `System.Threading.ReaderWriterLockSlim`
- `System.Collections.Generic.IEnumerable<T>`
