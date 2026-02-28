# xFrame 对象池系统 API 参考

## 命名空间

```csharp
using xFrame.Core.ObjectPool;
```

## 接口定义

### IObjectPool&lt;T&gt;

对象池的核心接口定义。

```csharp
public interface IObjectPool<T> where T : class
```

#### 属性

| 属性 | 类型 | 描述 |
|------|------|------|
| `CountInPool` | `int` | 当前池中可用对象的数量 |
| `CountAll` | `int` | 已创建的对象总数（包括池中和使用中的） |

#### 方法

| 方法 | 返回类型 | 描述 |
|------|----------|------|
| `Get()` | `T` | 从对象池中获取一个对象 |
| `Release(T obj)` | `void` | 将对象释放回对象池 |
| `WarmUp(int count)` | `void` | 预热对象池，预先创建指定数量的对象 |
| `Clear()` | `void` | 清空对象池，销毁所有池中的对象 |
| `Dispose()` | `void` | 销毁对象池，释放所有资源 |

### IPoolable

池化对象生命周期接口。

```csharp
public interface IPoolable
```

#### 方法

| 方法 | 描述 |
|------|------|
| `OnGet()` | 当对象从池中获取时调用，用于重置对象状态 |
| `OnRelease()` | 当对象被释放回池中时调用，用于清理对象状态 |
| `OnDestroy()` | 当对象被销毁时调用，用于释放对象持有的资源 |

## 核心类

### ObjectPool&lt;T&gt;

对象池的主要实现类。

```csharp
public class ObjectPool<T> : IObjectPool<T>, IDisposable where T : class
```

#### 构造函数

```csharp
public ObjectPool(
    Func<T> createFunc,
    Action<T> onGet = null,
    Action<T> onRelease = null,
    Action<T> onDestroy = null,
    int maxSize = -1,
    bool threadSafe = false)
```

**参数说明：**

| 参数 | 类型 | 必需 | 默认值 | 描述 |
|------|------|------|--------|------|
| `createFunc` | `Func<T>` | ✓ | - | 对象创建函数 |
| `onGet` | `Action<T>` | ✗ | `null` | 获取对象时的回调 |
| `onRelease` | `Action<T>` | ✗ | `null` | 释放对象时的回调 |
| `onDestroy` | `Action<T>` | ✗ | `null` | 销毁对象时的回调 |
| `maxSize` | `int` | ✗ | `-1` | 池的最大容量，-1表示无限制 |
| `threadSafe` | `bool` | ✗ | `false` | 是否启用线程安全 |

#### 异常

| 异常类型 | 触发条件 |
|----------|----------|
| `ArgumentNullException` | `createFunc` 为 `null` |
| `ObjectDisposedException` | 在对象池已销毁后调用方法 |

### ObjectPoolManager

对象池管理器，用于统一管理多个对象池。

```csharp
public class ObjectPoolManager : IDisposable
```

#### 构造函数

```csharp
public ObjectPoolManager(bool threadSafe = false)
```

**参数说明：**

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| `threadSafe` | `bool` | `false` | 是否启用线程安全 |

#### 方法

##### RegisterPool&lt;T&gt;

注册一个对象池到管理器。

```csharp
public void RegisterPool<T>(IObjectPool<T> pool) where T : class
```

**参数：**
- `pool`: 要注册的对象池实例

**异常：**
- `ArgumentNullException`: 当 `pool` 为 `null` 时抛出

##### GetPool&lt;T&gt;

获取指定类型的对象池。

```csharp
public IObjectPool<T> GetPool<T>() where T : class
```

**返回值：**
- 对象池实例，如果不存在则返回 `null`

##### GetOrCreatePool&lt;T&gt;

获取或创建指定类型的对象池。

```csharp
public IObjectPool<T> GetOrCreatePool<T>(Func<T> createFunc, int maxSize = -1) where T : class
```

**参数：**
- `createFunc`: 对象创建函数
- `maxSize`: 池的最大容量

**返回值：**
- 对象池实例

##### GetOrCreateDefaultPool&lt;T&gt;

获取或创建使用默认构造函数的对象池。

```csharp
public IObjectPool<T> GetOrCreateDefaultPool<T>(int maxSize = -1) where T : class, new()
```

**参数：**
- `maxSize`: 池的最大容量

**返回值：**
- 对象池实例

##### Get&lt;T&gt;

从指定类型的对象池中获取对象。

```csharp
public T Get<T>() where T : class
```

**返回值：**
- 池化对象实例，如果池不存在则返回 `null`

##### Release&lt;T&gt;

将对象释放回对应类型的对象池。

```csharp
public void Release<T>(T obj) where T : class
```

**参数：**
- `obj`: 要释放的对象

##### WarmUp&lt;T&gt;

预热指定类型的对象池。

```csharp
public void WarmUp<T>(int count) where T : class
```

**参数：**
- `count`: 要预创建的对象数量

##### Clear&lt;T&gt;

清空指定类型的对象池。

```csharp
public void Clear<T>() where T : class
```

##### ClearAll

清空所有对象池。

```csharp
public void ClearAll()
```

## 工厂类

### ObjectPoolFactory

提供便捷的对象池创建方法。

```csharp
public static class ObjectPoolFactory
```

#### 静态方法

##### Create&lt;T&gt;

创建一个基本的对象池。

```csharp
public static IObjectPool<T> Create<T>(
    Func<T> createFunc,
    int maxSize = -1,
    bool threadSafe = false) where T : class
```

##### Create&lt;T&gt; (完整版本)

创建一个带有回调的对象池。

```csharp
public static IObjectPool<T> Create<T>(
    Func<T> createFunc,
    Action<T> onGet,
    Action<T> onRelease,
    Action<T> onDestroy,
    int maxSize = -1,
    bool threadSafe = false) where T : class
```

##### CreateForPoolable&lt;T&gt;

创建一个支持重置接口的对象池。

```csharp
public static IObjectPool<T> CreateForPoolable<T>(
    Func<T> createFunc,
    int maxSize = -1,
    bool threadSafe = false) where T : class, IPoolable
```

##### CreateDefault&lt;T&gt;

创建一个默认构造函数的对象池。

```csharp
public static IObjectPool<T> CreateDefault<T>(
    int maxSize = -1,
    bool threadSafe = false) where T : class, new()
```

##### CreateDefaultForPoolable&lt;T&gt;

创建一个默认构造函数且支持重置接口的对象池。

```csharp
public static IObjectPool<T> CreateDefaultForPoolable<T>(
    int maxSize = -1,
    bool threadSafe = false) where T : class, IPoolable, new()
```

## 使用示例

### 基本对象池

```csharp
// 创建对象池
var pool = ObjectPoolFactory.Create(() => new MyObject());

// 获取对象
var obj = pool.Get();

// 使用对象
obj.DoSomething();

// 释放对象
pool.Release(obj);
```

### 带回调的对象池

```csharp
var pool = ObjectPoolFactory.Create(
    createFunc: () => new MyObject(),
    onGet: obj => obj.Reset(),
    onRelease: obj => obj.Cleanup(),
    onDestroy: obj => obj.Dispose(),
    maxSize: 100,
    threadSafe: true
);
```

### IPoolable 对象池

```csharp
public class MyPoolableObject : IPoolable
{
    public void OnGet() { /* 重置状态 */ }
    public void OnRelease() { /* 清理状态 */ }
    public void OnDestroy() { /* 释放资源 */ }
}

var pool = ObjectPoolFactory.CreateForPoolable(() => new MyPoolableObject());
```

### 对象池管理器

```csharp
var manager = new ObjectPoolManager();

// 注册对象池
var pool = ObjectPoolFactory.Create(() => new MyObject());
manager.RegisterPool(pool);

// 或直接创建
var pool2 = manager.GetOrCreatePool(() => new AnotherObject());

// 使用
var obj = manager.Get<MyObject>();
manager.Release(obj);
```

## 性能注意事项

### 线程安全开销

启用线程安全会带来额外的性能开销：

```csharp
// 单线程环境（推荐）
var pool = ObjectPoolFactory.Create(() => new MyObject(), threadSafe: false);

// 多线程环境
var pool = ObjectPoolFactory.Create(() => new MyObject(), threadSafe: true);
```

### 容量设置建议

```csharp
// 根据实际需求设置合适的容量
var pool = ObjectPoolFactory.Create(
    () => new Bullet(),
    maxSize: 200  // 设置为游戏中可能同时存在的最大子弹数
);
```

### 预热策略

```csharp
// 在游戏开始时预热，避免运行时分配
void Start()
{
    bulletPool.WarmUp(50);
    enemyPool.WarmUp(20);
}
```

## 错误处理

### 常见异常

| 异常 | 原因 | 解决方案 |
|------|------|----------|
| `ArgumentNullException` | 传入 `null` 参数 | 检查参数有效性 |
| `ObjectDisposedException` | 使用已销毁的对象池 | 确保对象池生命周期管理 |

### 最佳实践

```csharp
// 安全的对象池使用
try
{
    var obj = pool.Get();
    // 使用对象
    pool.Release(obj);
}
catch (ObjectDisposedException)
{
    // 对象池已被销毁
    Debug.LogWarning("对象池已被销毁");
}
finally
{
    // 清理代码
}
```

## 内存管理

### 对象池生命周期

```csharp
// 创建
var pool = ObjectPoolFactory.Create(() => new MyObject());

// 使用
// ...

// 清理（重要！）
pool.Dispose(); // 或者 using 语句
```

### 使用 using 语句

```csharp
using (var pool = ObjectPoolFactory.Create(() => new MyObject()))
{
    var obj = pool.Get();
    // 使用对象
    pool.Release(obj);
} // 自动调用 Dispose()
```

### 管理器生命周期

```csharp
using (var manager = new ObjectPoolManager())
{
    // 注册和使用对象池
    // ...
} // 自动清理所有注册的对象池
```
