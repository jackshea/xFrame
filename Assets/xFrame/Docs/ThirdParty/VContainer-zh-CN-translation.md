# VContainer

![](https://github.com/hadashiA/VContainer/workflows/Test/badge.svg) ![](https://img.shields.io/badge/unity-2018.4+-000.svg) [![Releases](https://img.shields.io/github/release/hadashiA/VContainer.svg)](https://github.com/hadashiA/VContainer/releases) [![openupm](https://img.shields.io/npm/v/jp.hadashikick.vcontainer?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/jp.hadashikick.vcontainer/)

在 Unity 游戏引擎上运行的超快速 DI（依赖注入）库。

“V”代表让 Unity 的首字母“U”变得更纤细、更稳固……！

*   **快速解析：** 基本上比 Zenject 快 5-10 倍。
*   **最少 GC 分配：** 在解析过程中，如果不生成实例，我们实现了**零分配** 。
*   **代码体积小：** 内部类型很少，.callvirt 调用也很少。
*   **支持正确的 DI 方式：** 提供简单、透明的 API，并谨慎选择功能。这能防止 DI 声明变得过于复杂。
*   **不可变容器：** 线程安全且稳健。

## 功能

*   构造函数注入 / 方法注入 / 属性与字段注入
*   分发自定义的 PlayerLoopSystem
*   灵活的作用域管理
    *   应用可自由使用任何异步方式为您创建嵌套的生命周期范围。
*   使用 SourceGenerator 的加速模式（可选）
*   Unity 编辑器中的诊断窗口
*   UniTask 集成
*   ECS 集成 *测试版*

## 文档

访问 [vcontainer.hadashikick.jp](https://vcontainer.hadashikick.jp) 查看完整文档。

## 性能

![](./website/static/img/benchmark_result.png)

### GC 分配结果示例

![](./website/static/img/gc_alloc_profiler_result.png)

![](./website/static/img/screenshot_profiler_vcontainer.png)

![](./website/static/img/screenshot_profiler_zenject.png)

## 安装

*需要 Unity 2018.4 及以上版本*

### 通过 UPM（使用 Git URL）安装

1.  导航到你项目的 Packages 文件夹并打开 manifest.json 文件。
2.  在 "dependencies": { 这一行的下面添加这一行
    *   ```json
        "jp.hadashikick.vcontainer": "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.17.0",
        ```
        
3.  UPM 现在应该会安装该软件包。

### 通过 OpenUPM 安装

1.  该软件包可在 [openupm 注册表](https://openupm.com) 中获取。建议通过 [openupm-cli](https://github.com/openupm/openupm-cli) 安装。
2.  执行 openum 命令。
    *   ```
        openupm add jp.hadashikick.vcontainer
        ```
        

### 手动安装（使用 .unitypackage）

1.  从 [发布](https://github.com/hadashiA/VContainer/releases) 页面下载 .unitypackage。
2.  打开 VContainer.x.x.x.unitypackage

## 基本用法

首先，创建一个作用域。此处注册的类型会自动解析引用。

```csharp
public class GameLifetimeScope : LifetimeScope
{
    public override void Configure(IContainerBuilder builder)
    {
        builder.RegisterEntryPoint<ActorPresenter>();

        builder.Register<CharacterService>(Lifetime.Scoped);
        builder.Register<IRouteSearch, AStarRouteSearch>(Lifetime.Singleton);

        builder.RegisterComponentInHierarchy<ActorsView>();
    }
}
```

类的定义为

```csharp
public interface IRouteSearch
{
}

public class AStarRouteSearch : IRouteSearch
{
}

public class CharacterService
{
    readonly IRouteSearch routeSearch;

    public CharacterService(IRouteSearch routeSearch)
    {
        this.routeSearch = routeSearch;
    }
}
```

```csharp
public class ActorsView : MonoBehaviour
{
}
```

和

```csharp
public class ActorPresenter : IStartable
{
    readonly CharacterService service;
    readonly ActorsView actorsView;
    readonly IWeapon primaryWeapon;
    readonly IWeapon secondaryWeapon;
    readonly IWeapon specialWeapon;

    public ActorPresenter(
        CharacterService service,
        ActorsView actorsView,
        [Key(WeaponType.Primary)] IWeapon primaryWeapon,
        [Key(WeaponType.Secondary)] IWeapon secondaryWeapon,
        [Key(WeaponType.Special)] IWeapon specialWeapon)
    {
        this.service = service;
        this.actorsView = actorsView;
        this.primaryWeapon = primaryWeapon;
        this.secondaryWeapon = secondaryWeapon;
        this.specialWeapon = specialWeapon;
    }

    void IStartable.Start()
    {
        // Scheduled at Start () on VContainer's own PlayerLoopSystem.
    }
}
```

你也可以直接通过基于对象的 Key 从容器中解析：

*   在此示例中，当解析 CharacterService 时，其 routeSearch 会自动设置为 AStarRouteSearch 的实例。
*   此外，VContainer 可以将纯 C# 类作为入口点。（可指定多种时机，如 Start、Update 等）这有助于实现“域逻辑与呈现的分离”。

### 使用 async 的灵活作用域

LifetimeScope 可以动态创建子级。这使您能够处理游戏中经常发生的异步资源加载。

```csharp
public void LoadLevel()
{
    // ... Loading some assets

    // Create a child scope
    instantScope = currentScope.CreateChild();

    // Create a child scope with LifetimeScope prefab
    instantScope = currentScope.CreateChildFromPrefab(lifetimeScopePrefab);

    // Create a child with additional registration
    instantScope = currentScope.CreateChildFromPrefab(
        lifetimeScopePrefab,
        builder =>
        {
            // Extra Registrations ...
        });

    instantScope = currentScope.CreateChild(builder =>
    {
        // ExtraRegistrations ...
    });

    instantScope = currentScope.CreateChild(extraInstaller);
}

public void UnloadLevel()
{
    instantScope.Dispose();
}
```

此外，您可以在 Additive 场景中通过 LifetimeScope 建立父子关系。

```csharp
class SceneLoader
{
    readonly LifetimeScope currentScope;

    public SceneLoader(LifetimeScope currentScope)
    {
        this.currentScope = currentScope; // Inject the LifetimeScope to which this class belongs
    }

    IEnumerator LoadSceneAsync()
    {
        // LifetimeScope generated in this block will be parented by `this.lifetimeScope`
        using (LifetimeScope.EnqueueParent(currentScope))
        {
            // If this scene has a LifetimeScope, its parent will be `parent`.
            var loading = SceneManager.LoadSceneAsync("...", LoadSceneMode.Additive);
            while (!loading.isDone)
            {
                yield return null;
            }
        }
    }

    // UniTask example
    async UniTask LoadSceneAsync()
    {
        using (LifetimeScope.EnqueueParent(parent))
        {
            await SceneManager.LoadSceneAsync("...", LoadSceneMode.Additive);
        }
    }
}
```

```csharp
// LifetimeScopes generated during this block will be additionally Registered.
using (LifetimeScope.Enqueue(builder =>
{
    // Register for the next scene not yet loaded
    builder.RegisterInstance(extraInstance);
}))
{
    // Loading the scene..
}
```

更多信息请参阅 [作用域](https://vcontainer.hadashikick.jp/scoping/lifetime-overview) 。

## UniTask

```csharp
public class FooController : IAsyncStartable
{
    public async UniTask StartAsync(CancellationToken cancellation)
    {
        await LoadSomethingAsync(cancellation);
        await ...
        ...
    }
}
```

```csharp
builder.RegisterEntryPoint<FooController>();
```

更多信息请参阅 [集成](https://vcontainer.hadashikick.jp/integrations/unitask) 。

## 诊断窗口

![](./website/static/img/screenshot_diagnostics_window.png)

更多信息请参阅 [诊断](https://vcontainer.hadashikick.jp/diagnostics/diagnostics-window) 。

## 致谢

VContainer 的灵感来源于：

*   [Zenject](https://github.com/modesttree/Zenject) / [Extenject](https://github.com/svermeulen/Extenject)。
*   [Autofac](http://autofac.org) - [Autofac Project](https://github.com/autofac/Autofac)。
*   [MicroResolver](https://github.com/neuecc/MicroResolver)

## 作者

[@hadashiA](https://twitter.com/hadashiA)

## 许可协议

MIT