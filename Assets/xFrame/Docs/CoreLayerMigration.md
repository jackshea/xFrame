# 核心层解耦迁移指南

## 概述

本框架已实现核心层与Unity解耦，允许：
- 在无Unity环境下运行游戏逻辑（集成测试/AI模拟）
- 运行时集成测试
- 多平台移植

## 架构分层

```
┌─────────────────────────────────────────────┐
│           Unity适配层 (Unity/)              │
│   UnityTimeProvider, UnityLogAppender,      │
│   UnityGameRunner                           │
└─────────────────────────────────────────────┘
                    ↓ 桥接
┌─────────────────────────────────────────────┐
│           核心层 (Core/)                    │
│   IGameCore, IGameRunner, ITimeProvider,    │
│   ICoreScheduler, CoreEventBus, CoreLogManager│
└─────────────────────────────────────────────┘
                    ↓ 接口
┌─────────────────────────────────────────────┐
│           业务层 (Game/)                    │
│   游戏逻辑、状态机、配置等                   │
└─────────────────────────────────────────────┘
```

## 快速开始

### 方式1: Unity环境运行（原有方式保持兼容）

```csharp
// 使用原有的MonoBehaviour启动器
// xFrameApplication 和 xFrameBootstrapper 仍然可用
```

### 方式2: Unity环境运行（推荐新方式）

```csharp
// 添加 UnityGameRunner 组件到场景
var runner = gameObject.AddComponent<UnityGameRunner>();
runner.Run();
```

### 方式3: 无Unity运行（测试/AI）

```csharp
// 创建模拟运行器
var runner = GameRunner.CreateSimulated(60);

// 注册自定义服务
runner.RegisterService<IMyService, MyService>();

// 启动
runner.Run();

// 模拟帧更新
for (int i = 0; i < 60 * 5; i++) // 模拟5秒
{
    runner.Update();
}

// 停止
runner.Stop();
```

## 迁移现有代码

### 1. 时间相关

旧代码:
```csharp
float delta = UnityEngine.Time.deltaTime;
float time = UnityEngine.Time.time;
```

新代码:
```csharp
// 方式1: 注入ITimeProvider
public class MyClass(ITimeProvider timeProvider)
{
    float delta = timeProvider.DeltaTime;
    float time = timeProvider.Time;
}

// 方式2: 使用核心调度器
var scheduler = runner.GetService<ICoreScheduler>();
scheduler.Delay(1f, () => Debug.Log("延迟1秒"));
```

### 2. 日志相关

旧代码:
```csharp
UnityEngine.Debug.Log("消息");
```

新代码:
```csharp
// 方式1: 使用核心日志（推荐）
var logger = logManager.GetLogger<MyClass>();
logger.Info("消息");

// 方式2: 使用Unity适配器（在Unity环境）
var unityLogger = new UnityLogAppender();
unityLogger.Append(entry);
```

### 3. 事件相关

旧代码:
```csharp
xFrameEventBus.Raise(new MyEvent());
xFrameEventBus.SubscribeTo<MyEvent>(handler);
```

新代码（核心层）:
```csharp
CoreEventBus.Raise(new MyCoreEvent());
CoreEventBus.Subscribe<MyCoreEvent>(handler);
```

### 4. 调度任务

旧代码:
```csharp
schedulerService.Delay(1f, () => { });
schedulerService.Interval(0.5f, () => { });
```

新代码:
```csharp
ICoreScheduler scheduler = ...; // 注入
scheduler.Delay(1f, () => { });
scheduler.Interval(0.5f, () => { });
```

## 核心接口列表

| 接口 | 说明 | 位置 |
|------|------|------|
| `IGameRunner` | 游戏运行器 | Core/Application |
| `ITimeProvider` | 时间提供者 | Core/Time |
| `ICoreScheduler` | 调度器 | Core/Scheduler |
| `ICoreLogManager` | 日志管理器 | Core/Log |
| `ICoreEvent` | 事件基类 | Core/Events |
| `CoreEventBus` | 事件总线 | Core/Events |

## 运行集成测试

```csharp
// 在Unity中运行
var tests = new CoreLayerIntegrationTests();
tests.RunAllTests();

// 或在命令行运行
dotnet test --filter "CoreLayerIntegrationTests"
```

## 目录结构

```
Assets/xFrame/Runtime/
├── Core/                          # 核心层（无Unity依赖）
│   ├── Application/
│   │   ├── IGameCore.cs
│   │   └── GameRunner.cs
│   ├── Time/
│   │   └── ITimeProvider.cs
│   ├── Scheduler/
│   │   └── ICoreScheduler.cs
│   ├── Log/
│   │   └── CoreLogManager.cs
│   └── Events/
│       └── CoreEventBus.cs
│
├── Unity/                          # Unity适配层
│   ├── Adapter/
│   │   └── UnityAdapter.cs
│   └── Bootstrapper/
│       └── UnityGameRunner.cs
│
└── (原有模块保持不变)
    ├── Scheduler/
    ├── EventBus/
    ├── Logging/
    └── ...
```

## 向后兼容性

原有的启动器 (`xFrameApplication`, `xFrameBootstrapper`) 仍然可用，
新架构作为补充提供无Unity运行能力。
