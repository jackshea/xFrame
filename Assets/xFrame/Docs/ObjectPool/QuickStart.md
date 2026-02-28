# xFrame 对象池系统 - 快速入门指南

## 5分钟快速上手

### 第一步：创建您的第一个对象池

```csharp
using xFrame.Core.ObjectPool;

// 创建一个简单的对象池
var bulletPool = ObjectPoolFactory.Create(() => new Bullet());

// 预热对象池（可选，但推荐）
bulletPool.WarmUp(20);
```

### 第二步：使用对象池

```csharp
// 获取对象
var bullet = bulletPool.Get();

// 设置对象属性
bullet.Position = playerPosition;
bullet.Direction = aimDirection;
bullet.Speed = bulletSpeed;

// 在游戏逻辑中使用对象
// ...

// 当对象不再需要时，释放回池中
bulletPool.Release(bullet);
```

### 第三步：清理资源

```csharp
// 在游戏结束或场景切换时
bulletPool.Dispose();
```

## 常见使用场景

### 场景1：游戏中的子弹系统

```csharp
public class BulletManager : MonoBehaviour
{
    private IObjectPool<Bullet> bulletPool;
    
    void Start()
    {
        // 创建子弹对象池
        bulletPool = ObjectPoolFactory.Create(
            createFunc: () => new Bullet(),
            onGet: bullet => bullet.gameObject.SetActive(true),
            onRelease: bullet => bullet.gameObject.SetActive(false),
            maxSize: 100
        );
        
        // 预热
        bulletPool.WarmUp(20);
    }
    
    public void FireBullet(Vector3 position, Vector3 direction)
    {
        var bullet = bulletPool.Get();
        bullet.Initialize(position, direction);
    }
    
    public void DestroyBullet(Bullet bullet)
    {
        bulletPool.Release(bullet);
    }
    
    void OnDestroy()
    {
        bulletPool?.Dispose();
    }
}
```

### 场景2：实现IPoolable接口的对象

```csharp
public class Enemy : IPoolable
{
    public float Health { get; private set; }
    public Vector3 Position { get; set; }
    
    // 从池中获取时调用
    public void OnGet()
    {
        Health = 100f;
        Position = Vector3.zero;
        gameObject.SetActive(true);
    }
    
    // 释放回池中时调用
    public void OnRelease()
    {
        gameObject.SetActive(false);
    }
    
    // 对象被销毁时调用
    public void OnDestroy()
    {
        if (gameObject != null)
            Destroy(gameObject);
    }
}

// 使用支持IPoolable的对象池
var enemyPool = ObjectPoolFactory.CreateForPoolable(() => new Enemy());
```

### 场景3：使用对象池管理器

```csharp
public class GameManager : MonoBehaviour
{
    private ObjectPoolManager poolManager;
    
    void Start()
    {
        // 创建对象池管理器
        poolManager = new ObjectPoolManager();
        
        // 注册各种对象池
        var bulletPool = ObjectPoolFactory.Create(() => new Bullet());
        var enemyPool = ObjectPoolFactory.Create(() => new Enemy());
        var effectPool = ObjectPoolFactory.Create(() => new Effect());
        
        poolManager.RegisterPool(bulletPool);
        poolManager.RegisterPool(enemyPool);
        poolManager.RegisterPool(effectPool);
        
        // 预热所有对象池
        poolManager.WarmUp<Bullet>(50);
        poolManager.WarmUp<Enemy>(20);
        poolManager.WarmUp<Effect>(30);
    }
    
    public void SpawnBullet()
    {
        var bullet = poolManager.Get<Bullet>();
        // 使用子弹...
    }
    
    public void DestroyBullet(Bullet bullet)
    {
        poolManager.Release(bullet);
    }
    
    void OnDestroy()
    {
        poolManager?.Dispose();
    }
}
```

## 性能优化技巧

### 1. 合理预热

```csharp
// 在关卡开始前预热，避免游戏中的卡顿
void OnLevelStart()
{
    bulletPool.WarmUp(50);  // 根据关卡需求预热
    enemyPool.WarmUp(20);
    effectPool.WarmUp(30);
}
```

### 2. 设置合适的容量限制

```csharp
// 根据游戏需求设置容量，避免内存浪费
var bulletPool = ObjectPoolFactory.Create(
    () => new Bullet(),
    maxSize: 200  // 同时最多200个子弹
);
```

### 3. 及时释放对象

```csharp
// 对象使用完毕后立即释放
void OnBulletHitTarget(Bullet bullet)
{
    // 处理碰撞逻辑
    HandleHit(bullet);
    
    // 立即释放
    bulletPool.Release(bullet);
}
```

## 常见错误及解决方案

### 错误1：忘记释放对象

```csharp
// ❌ 错误：获取对象后忘记释放
var bullet = bulletPool.Get();
// ... 使用后忘记调用 Release

// ✅ 正确：确保对象被释放
var bullet = bulletPool.Get();
try
{
    // 使用对象
    UseBullet(bullet);
}
finally
{
    bulletPool.Release(bullet);
}
```

### 错误2：重复释放同一对象

```csharp
// ❌ 错误：重复释放
bulletPool.Release(bullet);
bulletPool.Release(bullet); // 第二次释放会被忽略，但不是好习惯

// ✅ 正确：释放后将引用设为null
bulletPool.Release(bullet);
bullet = null;
```

### 错误3：使用已释放的对象

```csharp
// ❌ 错误：释放后继续使用
bulletPool.Release(bullet);
bullet.Move(); // 危险！对象可能已被重置

// ✅ 正确：释放前完成所有操作
bullet.Move();
bulletPool.Release(bullet);
bullet = null;
```

### 错误4：忘记销毁对象池

```csharp
// ❌ 错误：忘记清理
void OnDestroy()
{
    // 忘记销毁对象池，可能导致内存泄漏
}

// ✅ 正确：确保清理资源
void OnDestroy()
{
    bulletPool?.Dispose();
    enemyPool?.Dispose();
    poolManager?.Dispose();
}
```

## 调试技巧

### 1. 监控对象池状态

```csharp
void Update()
{
    // 在开发阶段监控对象池状态
    Debug.Log($"子弹池: {bulletPool.CountInPool}/{bulletPool.CountAll}");
    Debug.Log($"敌人池: {enemyPool.CountInPool}/{enemyPool.CountAll}");
}
```

### 2. 使用OnGUI显示状态

```csharp
void OnGUI()
{
    GUILayout.Label($"子弹池: {bulletPool.CountInPool}/{bulletPool.CountAll}");
    GUILayout.Label($"敌人池: {enemyPool.CountInPool}/{enemyPool.CountAll}");
    
    if (GUILayout.Button("清空子弹池"))
    {
        bulletPool.Clear();
    }
}
```

### 3. 添加调试回调

```csharp
var debugPool = ObjectPoolFactory.Create(
    createFunc: () => new Bullet(),
    onGet: bullet => Debug.Log($"获取子弹: {bullet.GetHashCode()}"),
    onRelease: bullet => Debug.Log($"释放子弹: {bullet.GetHashCode()}"),
    onDestroy: bullet => Debug.Log($"销毁子弹: {bullet.GetHashCode()}")
);
```

## 进阶用法

### 1. 自定义对象重置逻辑

```csharp
var customPool = ObjectPoolFactory.Create(
    createFunc: () => new ComplexObject(),
    onGet: obj => {
        obj.Reset();
        obj.Initialize();
        obj.SetActive(true);
    },
    onRelease: obj => {
        obj.Cleanup();
        obj.SetActive(false);
    }
);
```

### 2. 条件性对象销毁

```csharp
var smartPool = ObjectPoolFactory.Create(
    createFunc: () => new SmartObject(),
    onDestroy: obj => {
        // 只有在特定条件下才真正销毁
        if (obj.ShouldDestroy())
        {
            obj.ReleaseResources();
        }
    },
    maxSize: 50
);
```

### 3. 线程安全的对象池

```csharp
// 在多线程环境中使用
var threadSafePool = ObjectPoolFactory.Create(
    () => new ThreadSafeObject(),
    threadSafe: true
);

// 可以在不同线程中安全使用
Task.Run(() => {
    var obj = threadSafePool.Get();
    // 使用对象
    threadSafePool.Release(obj);
});
```

## 下一步

现在您已经掌握了对象池系统的基本用法，建议：

1. 查看 [完整文档](README.md) 了解更多高级功能
2. 参考 [API文档](API_Reference.md) 了解详细的接口说明
3. 运行 [示例代码](./Examples/ObjectPoolExample.cs) 查看实际应用
4. 查看 [单元测试](../Tests/) 了解各种使用场景

## 需要帮助？

如果您在使用过程中遇到问题：

1. 检查是否正确实现了对象的生命周期管理
2. 确认对象池的容量设置是否合理
3. 验证是否正确处理了对象的释放和清理
4. 查看单元测试中的示例用法

记住：对象池的核心思想是**重用对象以减少GC压力**，正确的使用方式是获取→使用→释放的循环。
