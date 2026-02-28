# xFrame

一个轻量级的 Unity C# 游戏开发框架，基于 Unity 2021.3.51f1 和通用渲染管线 (URP) 构建。

## 快速开始

### 1. 安装框架

将 `Assets/xFrame` 文件夹导入到你的 Unity 项目中。

### 2. 初始化框架

在场景中添加 `xFrameBootstrapper` 预制体，或使用 `Assets/xFrame/Runtime/Bootstrapper/BootstrapperPrefab.prefab`。

### 3. 配置生命周期

框架会自动初始化核心服务，包括：
- 事件总线
- 依赖注入容器
- 日志系统
- 任务调度器
- 状态机

## 核心模块

### EventBus（事件总线）

事件驱动的通信系统，用于解耦模块间的依赖。

**使用示例：**
```csharp
// 定义事件
public class PlayerDamageEvent : IEvent
{
    public int Damage;
    public GameObject Source;
}

// 订阅事件
xFrameEventBus.SubscribeTo<PlayerDamageEvent>((ref e) =>
{
    Debug.Log($"Player took {e.Damage} damage from {e.Source.name}");
});

// 触发事件
xFrameEventBus.Raise(new PlayerDamageEvent { Damage = 10, Source = enemy });
```

### DI（依赖注入）

基于 VContainer 的依赖注入容器，管理服务生命周期。

**生命周期类型：**
- `Singleton` - 单例，整个应用生命周期内只创建一个实例
- `Transient` - 瞬态，每次解析创建新实例
- `Scoped` - 作用域，在同一作用域内返回相同实例

**使用示例：**
```csharp
// 注册服务（在 xFrameLifetimeScope.Configure 中）
builder.Register<ITestService, TestService>(Lifetime.Singleton);

// 解析服务
var service = xFrameLifetimeScope.Container.Resolve<ITestService>();
```

### Logging（日志系统）

多输出的日志系统，支持控制台、文件、网络和 UnityDebug。

**日志等级：**
- Verbose
- Debug
- Info
- Warning
- Error
- Fatal

**使用示例：**
```csharp
// 获取日志记录器
var logger = xFrameApplication.Instance.Logger;

// 记录日志
logger.Info("Game started");
logger.Error("Connection failed", exception);
```

### ObjectPool（对象池）

高性能的对象池系统，支持多种对象池策略。

**使用示例：**
```csharp
// 创建对象池
var pool = xFrameApplication.Instance.SchedulerService;

// 从池中获取对象
var obj = pool.Get<Enemy>();

// 归还对象
pool.Return(obj);
```

### StateMachine（状态机）

带编辑器支持的状态机系统，用于管理游戏对象的状态转换。

**使用示例：**
```csharp
// 定义状态
public class IdleState : StateBase<Player> { }
public class MoveState : StateBase<Player> { }

// 创建状态机
var stateMachine = xFrameApplication.Instance.StateMachineService;

// 切换状态
stateMachine.ChangeState<MoveState>();
```

### Scheduler（任务调度器）

时间任务调度系统，支持延迟执行、定时重复执行等。

**API：**
```csharp
// 延迟执行
schedulerService.Delay(1f, () => Debug.Log("1秒后执行"));

// 定时重复
schedulerService.Interval(0.5f, () => Debug.Log("每0.5秒执行"));

// 下一帧执行
schedulerService.NextFrame(() => Debug.Log("下一帧执行"));
```

### UI（UI 系统）

视图-展示器模式的 UI 管理系统，支持组件绑定和复用。

**核心概念：**
- `UIView` - 视图基类
- `UIPresenter` - 展示器基类
- `UIComponent` - UI 组件基类
- `UIBinder` - UI 绑定器

### MVVM（数据绑定 UI）

提供轻量级 MVVM 基础能力，用于实现 UI 与业务逻辑解耦。

**核心能力：**
- `BindableProperty<T>`：低开销属性变更通知
- `RelayCommand`：View 到 ViewModel 的命令调用
- `BindingContext`：统一管理绑定生命周期，避免内存泄漏

示例位于 `Assets/xFrame/Runtime/MVVM/Examples/`，包含 `PlayerModel`、`PlayerViewModel` 与 `PlayerView`。

### Persistence（数据持久化）

跨平台的数据持久化系统，支持多种存储提供者。

### Serialization（序列化）

通用的序列化接口和实现，支持 JSON 等多种格式。

### ResourceManager（资源管理）

基于 Addressable 和传统加载的资源管理系统。

### DataStructures（数据结构）

LRU（最近最少使用）缓存实现。

### Networking（网络通信）

已提供基础接口与默认实现（`INetworkClient`、`NullNetworkClient`），可按项目协议扩展。

### Platform（平台特定工具）

已提供基础平台服务封装（`IPlatformService`、`UnityPlatformService`）。

### Utilities（工具函数）

已提供基础工具能力（`IGuidService`、`RetryUtility`）。

## 测试

框架包含完整的测试套件：

### EditMode 测试
位于 `Assets/xFrame/Tests/EditMode/`，包括：
- EventBus 测试
- LRU Cache 测试
- ObjectPool 测试
- Persistence 测试
- Scheduler 测试
- Serialization 测试
- StateMachine 测试
- DI 测试
- UI 测试
- MVVM 测试

### PlayMode 测试
位于 `Assets/xFrame/Tests/PlayMode/`，包括：
- UI 测试
- Scheduler 测试

运行测试：在 Unity Editor 中，选择 `Window > General > Test Runner`

## 示例代码

详细的示例代码位于 `Assets/xFrame.Examples/`：
- Logging 示例
- Scheduler 示例
- StateMachine 示例
- ObjectPool 示例
- UI 示例

## 文档

各模块的详细文档位于 `Assets/xFrame/Docs/`：
- UI 框架文档
- 有限状态机文档
- 数据持久化文档
- 序列化模块文档
- ObjectPool 文档

## 依赖项

主要依赖包括：
- **com.cysharp.unitask** - Unity 高性能 async/await
- **jp.hadashikick.vcontainer** - 现代依赖注入 (v1.17.0)
- **com.peturdarri.generic-event-bus** - 事件系统
- **com.unity.addressables** - Addressable 资源系统 (v1.19.19)
- **com.unity.test-framework** - 测试框架 (v1.1.33)
- **com.unity.render-pipelines.universal** - URP (v12.1.16)
- **com.unity.textmeshpro** - 高级文本渲染 (v3.0.9)

## 构建和发布

### 构建项目

1. 在 Unity 2021.3.51f1 中打开项目
2. 选择 `File > Build Settings`
3. 配置目标平台和构建设置
4. 点击 `Build`

### 打包为 Unity 包

在 Project Settings 中，选择 `Assets/xFrame`，点击右键选择 `Export Package`，生成 `.unitypackage` 文件。

## 贡献指南

欢迎贡献代码和改进！请遵循以下步骤：

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 提交 Pull Request

### 代码规范

- 遵循现有的代码风格
- 所有公共 API 使用中文注释
- 添加适当的单元测试
- 确保所有测试通过

### 测试要求

- 新功能必须包含单元测试
- 测试覆盖率应保持或提高
- EditMode 测试不应依赖场景
- PlayMode 测试应在适当的测试场景中运行

## 许可证

请查看项目根目录的 LICENSE 文件了解许可信息。

## 联系方式

- 问题反馈：提交 Issue
- 功能建议：提交 Feature Request
- 贡献代码：提交 Pull Request

---

**注意**：xFrame 是一个不断发展的框架，功能和 API 可能会变化。建议查看最新的文档和示例代码。
