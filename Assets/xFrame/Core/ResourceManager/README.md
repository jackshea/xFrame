# xFrame AssetManager 资源管理模块

## 概述

AssetManager是xFrame框架的资源管理模块，提供了统一的资源加载和释放接口。该模块封装了Unity的Addressable系统，同时保持了良好的扩展性，支持后续切换到其他资源管理实现（如Resources、AssetBundle等）。

## 主要特性

- **统一接口**: 提供`IAssetManager`接口，隐藏底层实现细节
- **缓存支持**: 集成LRU缓存系统，提升资源加载效率
- **异步加载**: 支持同步和异步资源加载
- **依赖注入**: 完全集成VContainer依赖注入系统
- **模块化设计**: 实现`IModule`接口，支持框架模块化管理
- **扩展性**: 通过`IAssetProvider`接口支持多种资源管理方案
- **统计信息**: 提供详细的缓存和加载统计信息
- **线程安全**: 支持多线程环境下的资源管理

## 核心组件

### 1. IAssetManager 接口

主要的资源管理接口，提供以下功能：

```csharp
// 同步加载
T LoadAsset<T>(string address) where T : UnityEngine.Object;
UnityEngine.Object LoadAsset(string address, Type type);

// 异步加载
Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object;
Task<UnityEngine.Object> LoadAssetAsync(string address, Type type);

// 资源释放
void ReleaseAsset(UnityEngine.Object asset);
void ReleaseAsset(string address);

// 缓存管理
Task PreloadAssetAsync(string address);
bool IsAssetCached(string address);
void ClearCache();
AssetCacheStats GetCacheStats();
```

### 2. AddressableAssetManager 实现

基于Unity Addressable系统的资源管理器实现：

- 内部封装Addressable API，外部无需引用Addressable命名空间
- 使用LRU缓存提升加载效率
- 支持同步和异步加载
- 自动管理资源句柄和释放

### 3. AssetManagerModule 模块

实现`IModule`接口的资源管理模块：

- 优先级设为50，在日志模块之后初始化
- 自动注册到VContainer容器
- 提供模块生命周期管理

### 4. IAssetProvider 扩展接口

为后续扩展预留的资源提供者接口：

- 支持多种资源管理方案（Addressable、Resources、AssetBundle等）
- 统一的加载和释放接口
- 提供者统计信息

## 使用方法

### 1. 基本用法

```csharp
public class MyScript : MonoBehaviour
{
    [Inject]
    private IAssetManager _assetManager;

    private async void Start()
    {
        // 同步加载
        var prefab = _assetManager.LoadAsset<GameObject>("MyPrefab");
        if (prefab != null)
        {
            var instance = Instantiate(prefab);
            _assetManager.ReleaseAsset(prefab);
        }

        // 异步加载
        var texture = await _assetManager.LoadAssetAsync<Texture2D>("MyTexture");
        if (texture != null)
        {
            // 使用纹理...
            _assetManager.ReleaseAsset(texture);
        }
    }
}
```

### 2. 预加载和缓存管理

```csharp
// 预加载资源到缓存
await _assetManager.PreloadAssetAsync("MyPrefab");
await _assetManager.PreloadAssetAsync("MyTexture");

// 检查缓存状态
bool isCached = _assetManager.IsAssetCached("MyPrefab");

// 获取缓存统计
var stats = _assetManager.GetCacheStats();
Debug.Log($"缓存资源数: {stats.CachedAssetCount}, 命中率: {stats.CacheHitRate:P2}");

// 清理所有缓存
_assetManager.ClearCache();
```

### 3. 非泛型加载

```csharp
// 使用Type参数加载
var audioClip = _assetManager.LoadAsset("MyAudio", typeof(AudioClip)) as AudioClip;

// 异步非泛型加载
var asset = await _assetManager.LoadAssetAsync("MyAsset", typeof(ScriptableObject));
```

## 配置和注册

### 1. VContainer注册

AssetManager已自动注册到xFrameLifetimeScope：

```csharp
// 在xFrameLifetimeScope.cs中
private void RegisterResourceSystem(IContainerBuilder builder)
{
    builder.Register<IAssetManager, AddressableAssetManager>(Lifetime.Singleton);
    builder.Register<AssetManagerModule>(Lifetime.Singleton)
        .AsImplementedInterfaces()
        .AsSelf();
}
```

### 2. 模块注册

AssetManagerModule已注册到ModuleRegistry：

```csharp
// 在ModuleRegistry.cs中
public static void RegisterCoreModules(ModuleManager moduleManager)
{
    moduleManager.RegisterModule<XLoggingModule>();
    moduleManager.RegisterModule<AssetManagerModule>();
}
```

## 扩展性

### 1. 自定义资源提供者

实现`IAssetProvider`接口来支持其他资源管理方案：

```csharp
public class CustomAssetProvider : IAssetProvider
{
    public string ProviderName => "Custom";
    public bool SupportsAsync => true;

    public T LoadAsset<T>(string address) where T : UnityEngine.Object
    {
        // 自定义加载逻辑
    }

    // 实现其他接口方法...
}
```

### 2. 切换资源提供者

通过修改VContainer注册来切换不同的资源管理实现：

```csharp
// 切换到Resources系统
builder.Register<IAssetManager, ResourcesAssetManager>(Lifetime.Singleton);

// 或者注册自定义实现
builder.Register<IAssetManager, CustomAssetManager>(Lifetime.Singleton);
```

## 性能优化

### 1. 缓存配置

```csharp
// 创建自定义容量的AssetManager
var assetManager = new AddressableAssetManager(cacheCapacity: 200);
```

### 2. 预加载策略

```csharp
// 在场景开始时预加载常用资源
private async void PreloadCommonAssets()
{
    string[] commonAssets = { "UI_Button", "Effect_Explosion", "Audio_BGM" };
    
    var preloadTasks = commonAssets.Select(address => 
        _assetManager.PreloadAssetAsync(address));
    
    await Task.WhenAll(preloadTasks);
}
```

### 3. 资源释放管理

```csharp
// 及时释放不再使用的资源
private void OnDestroy()
{
    if (_loadedAssets != null)
    {
        foreach (var asset in _loadedAssets)
        {
            _assetManager.ReleaseAsset(asset);
        }
        _loadedAssets.Clear();
    }
}
```

## 最佳实践

1. **及时释放**: 使用完资源后及时调用`ReleaseAsset`
2. **预加载**: 对于频繁使用的资源，使用`PreloadAssetAsync`预加载到缓存
3. **异步优先**: 优先使用异步加载避免阻塞主线程
4. **统计监控**: 定期检查`GetCacheStats()`来优化缓存策略
5. **错误处理**: 始终检查加载结果是否为null
6. **依赖注入**: 通过VContainer注入`IAssetManager`而不是直接实例化

## 注意事项

1. **Addressable依赖**: 当前实现依赖Unity Addressable包，确保项目中已安装
2. **地址格式**: 资源地址格式需要符合Addressable系统的要求
3. **线程安全**: 虽然实现了线程安全，但建议在主线程中进行UI相关的资源操作
4. **内存管理**: 大量资源加载时注意监控内存使用情况
5. **缓存容量**: 根据项目需求合理设置缓存容量，避免内存溢出

## 示例代码

完整的使用示例请参考 `AssetManagerExample.cs` 文件，其中包含了各种使用场景的演示代码。
