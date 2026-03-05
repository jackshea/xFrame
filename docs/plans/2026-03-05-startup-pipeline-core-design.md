# xFrame 启动流程核心实现说明

## 目标

本次实现聚焦“最核心能力”：

1. 启动流程的纯 C# 编排，不依赖 `UnityEngine`。
2. 通过 `IStartupView` 注入表现层，实现逻辑与 UI 解耦。
3. 提供可配置的 Profile 组装能力（`BootEnvironment` + `StartupTaskRegistry`）。
4. 提供失败治理基础能力（重试、超时、致命/非致命、继续/中断）。
5. 具备可测试性（EditMode 单元测试通过 Mock View + Fake Task 覆盖核心路径）。

## 代码落点

- 核心实现：`Assets/xFrame/Runtime/Bootstrapper/StartupPipeline.cs`
- 核心测试：`Assets/xFrame/Tests/EditMode/StartupPipelineTests.cs`
- Unity 薄接入：
  - `Assets/xFrame/Runtime/Unity/Startup/UnityStartupEntry.cs`
  - `Assets/xFrame/Runtime/Unity/Startup/UnityStartupView.cs`
  - `Assets/xFrame/Runtime/Unity/Startup/UnityStartupInstallerBase.cs`
  - `Assets/xFrame/Runtime/Unity/Startup/UnityStartupInstallerExample.cs`

## 核心类型

### 1) 任务与视图

- `IStartupTask`
  - `TaskName`、`Weight`
  - `FailurePolicy`（失败后中断/继续）
  - `ExecutionOptions`（重试次数、超时）
  - `InjectView(IStartupView view)`
  - `ExecuteAsync(CancellationToken)`

- `IStartupView`
  - `ShowLoading(string message, float progress)`
  - `ShowErrorDialogAsync(string message, CancellationToken)`：返回 `true` 表示用户选择重试
  - `HideLoading()`

### 2) 结果模型

- `StartupTaskResult`
  - `IsSuccess`、`IsFatal`
  - `ErrorCode`、`ErrorMessage`、`Exception`
  - 工厂方法：`Success()` / `Failed(...)`

- `StartupPipelineResult`
  - `IsSuccess`、`IsCancelled`
  - `FailedTaskName`、`FailureResult`

### 3) 流程编排

- `StartupPipelineBuilder`
  - `AddTask(...)`
  - `WithView(...)`
  - `Build()`

- `StartupPipeline`
  - 按权重推进全局进度
  - 每个任务执行前注入 View
  - 内部执行支持“自动重试 + 超时保护”
  - 失败后根据 `IsFatal` + `FailurePolicy` + 用户交互决定继续/中断

### 4) Profile 组装

- `BootEnvironment`：`Release` / `DevFull` / `DevSkipToBattle`
- `StartupTaskKey`：启动步骤键
- `StartupTaskRegistry`：任务工厂注册中心
- `StartupProfile`：环境到任务序列的映射
- `StartupPipelineFactory`：根据环境和注册中心构建最终 pipeline

### 5) 代码化配置（不使用 JSON）

- `StartupProfileBuilder`
  - `Add(...)`：按顺序追加任务
  - `AddIf(...)`：按条件追加任务
  - `AddRange(...)`：批量追加任务
  - `Build()`：产出 `StartupProfile`

- `StartupPipelineFactory.Create(StartupProfile, StartupTaskRegistry, IStartupView)`
  - 允许在 C# 中显式定义 profile 并构建流程
  - 适用于 Dev/自动化测试场景的按需组装

- `DelegateStartupTask`
  - 使用委托快速定义启动节点，减少样板代码
  - 适用于示例、Mock、测试环境任务快速搭建

## 已覆盖行为（测试）

`StartupPipelineTests` 覆盖了核心行为：

1. 成功路径下的执行顺序与加权进度。
2. 任务异常后的自动重试成功。
3. 致命失败 + 用户取消时中断流程。
4. 非致命失败 + ContinuePolicy 时跳过继续。
5. `DevSkipToBattle` Profile 的任务组装顺序。
6. 代码配置 Profile（`StartupProfileBuilder`）的组装顺序与条件分支。
7. `StartupPipelineLauncher` 安装器链路可以正确安装并执行任务。
8. `DelegateStartupTask` 可通过注入 View + 回调执行成功。

## Dotnet 单测执行说明

为满足命令行环境下的稳定验证，新增了独立测试工程：

- `Tests/StartupPipeline.DotnetTests/StartupPipeline.DotnetTests.csproj`

该工程直接编译以下源码并运行 NUnit：

- `Assets/xFrame/Runtime/Bootstrapper/StartupPipeline.cs`
- `Assets/xFrame/Tests/EditMode/StartupPipelineTests.cs`

验证命令：

```bash
dotnet test Tests/StartupPipeline.DotnetTests/StartupPipeline.DotnetTests.csproj -v minimal
```

当前结果：`Passed: 6, Failed: 0`。

最近一次结果：`Passed: 7, Failed: 0`。

## Unity 薄接入说明

在不污染核心层的前提下，新增了最小 Unity 接入面：

1. `UnityStartupEntry`
   - MonoBehaviour 入口，仅负责：环境选择、安装任务、创建 pipeline、触发 `RunAsync`
   - 生命周期销毁时自动取消 `CancellationToken`
2. `UnityStartupInstallerBase`
   - 作为 Unity 侧任务注册点，项目方只需继承并在 `Install` 里注册任务
3. `UnityStartupView`
   - `IStartupView` 的 Unity 适配实现（当前默认日志输出，支持错误时自动重试开关）
4. `UnityStartupInstallerExample`
   - 提供可直接挂载的示例安装器
   - 演示如何注册所有 `StartupTaskKey`，并通过 Inspector 开关控制可选步骤

这使得“启动逻辑纯 C# + Unity 仅作 View/入口胶水层”的边界更加清晰。

## 后续建议

1. 增加 `boot.json` 读取器，将 `StartupProfile` 配置化。
2. 在 `Assets/xFrame/Runtime/Unity/` 添加 `IStartupView` 的 UGUI/UIToolkit 适配实现。
3. 接入现有日志系统（`IXLogger`）输出结构化启动事件与失败上下文。
4. 为关键任务补充真实实现（热更、SDK、网络、大厅/战斗入口）。
