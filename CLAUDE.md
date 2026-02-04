# CLAUDE.md

本文件为 Claude Code (claude.ai/code) 在此仓库中工作时提供指导。

## 语言规范
- 始终使用中文回复
- 始终使用中文注释

## 项目概述

xFrame 是一个轻量级的 Unity C# 游戏开发框架，基于 Unity 2021.3.51f1 和通用渲染管线 (URP) 构建。它通过事件驱动架构、依赖注入和各种实用工具模块，提供模块化系统和工具来加速游戏开发。

## 架构

框架采用模块化架构，在运行时、编辑器和测试代码之间有清晰的分离：

### 核心入口点

- **xFrameApplication** (`Assets/xFrame/Runtime/Bootstrapper/xFrameApplication.cs`) - 管理生命周期和核心系统的主应用单例
- **xFrameBootstrapper** (`Assets/xFrame/Runtime/Bootstrapper/xFrameBootstrapper.cs`) - 框架初始化和系统设置
- **xFrameLifetimeScope** - 基于 VContainer 的 DI 容器，用于依赖管理

### 核心模块

位于 `Assets/xFrame/Runtime/`：

- **Bootstrapper** - 应用初始化和生命周期
- **EventBus** - 使用 GenericEventBus 的事件驱动架构
- **DI** - 基于 VContainer 的依赖注入
- **Logging** - 多输出日志系统（控制台、文件、网络、UnityDebug）
- **ObjectPool** - 支持多种策略的对象池
- **StateMachine** - 带编辑器支持的状态机
- **UI** - UI 管理和绑定系统
- **ResourceManager** - Addressable 和传统资源加载
- **Scheduler** - 任务调度系统
- **DataStructures** - LRU 缓存实现
- **Networking** - 网络通信工具
- **Platform** - 平台特定工具
- **Utilities** - 通用辅助函数

### 目录结构

```
Assets/
├── xFrame/              # 核心框架
│   ├── Runtime/         # 运行时代码
│   ├── Editor/          # 编辑器扩展
│   └── Tests/           # EditMode 和 PlayMode 测试
├── xFrame.Examples/     # 使用示例
├── Game/               # 游戏特定代码
├── ThirdParty/         # 外部包
└── Scenes/             # Unity 场景
```

## 开发

### 构建

这是一个 Unity 项目。通过 Unity Editor 进行构建：
- 在 Unity 2021.3.51f1 中打开项目
- 使用 File > Build Settings 配置和构建
- 解决方案文件（`xFrame.sln`、`*.csproj`）是自动生成的，已从 git 中排除

### 测试

使用 Unity Test Framework（基于 NUnit）：

- **EditMode 测试** - 编辑器功能（不需要场景）
  - 通过 Unity Editor 运行：Window > General > Test Runner > EditMode
  - 位于 `Assets/xFrame/Tests/EditMode/`
  - 程序集：`xFrame.EditModeTests`

- **PlayMode 测试** - 运行时功能（需要场景）
  - 通过 Unity Editor 运行：Window > General > Test Runner > PlayMode
  - 位于 `Assets/xFrame/Tests/PlayMode/`
  - 程序集：`xFrame.PlayModeTests`

### 主要依赖

来自 `Packages/manifest.json`：

- **com.cysharp.unitask** - Unity 高性能 async/await
- **jp.hadashikick.vcontainer** - 现代依赖注入（v1.17.0）
- **com.peturdarri.generic-event-bus** - 事件系统
- **com.unity.addressables** - Addressable 资源系统（v1.19.19）
- **com.unity.test-framework** - 测试框架（v1.1.33）
- **com.unity.render-pipelines.universal** - URP（v12.1.16）
- **com.unity.textmeshpro** - 高级文本渲染（v3.0.9）

## 重要模式

### 依赖注入

框架使用 VContainer 进行 DI。服务在 `xFrameLifetimeScope` 中注册，并在整个应用中解析。新服务应在 LifetimeScope 中注册，并通过构造函数注入。

### 事件驱动架构

`xFrameEventBus` 是中央事件系统。事件实现 `IEvent` 接口，并使用通用事件总线模式触发/发布。这是解耦系统之间的主要通信机制。

### 单例模式

`xFrameApplication` 和 `xFrameBootstrapper` 都使用单例模式。通过 `xFrameApplication.Instance` 或 `xFrameBootstrapper.Instance` 访问。

### 模块结构

每个模块都是自包含的，有自己的接口和实现。添加新功能时，请遵循 `Assets/xFrame/Runtime/` 中现有的模块模式。

## 注意事项

- 代码注释和文档主要为中文
- 解决方案文件是自动生成的，不应手动编辑
- 框架设计为 Unity 包/框架，可用于其他项目
- 示例位于 `Assets/xFrame.Examples/` - 参考这些以了解使用模式
- 编辑器特定代码位于 `Assets/xFrame/Editor/` 并使用 `#if UNITY_EDITOR` 守卫
