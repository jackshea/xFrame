## Context

现有启动体系已将任务执行集中到 `StartupPipeline`，但流程创建与运行仍依赖 `UnityStartupEntry`、`UnityStartupInstallerBase`、`UnityStartupView` 等 Unity 组件组合。该结构在游戏运行时可用，但在“通过 Unity RPC 启停游戏”与“非 Play Mode 自动化验证”场景下，仍需依赖场景对象与生命周期回调，阻碍了脚本化控制和可测试性。

目标是把“是否在 Unity 中触发”与“启动流程如何编排”拆开：流程编排沉入纯 C# 核心层，Unity 仅保留一个薄入口。

## Goals / Non-Goals

**Goals:**
- 构建纯 C# 的启动核心服务，支持 `StartAsync`/`StopAsync` 生命周期管理。
- 使用 C# 脚本定义环境流程，不依赖 Inspector 任务拼装。
- Unity 层保留单入口脚本，职责仅为桥接调用与取消令牌管理。
- 提供可复用 API 给 Unity RPC，在不播放场景时也可触发启动与关闭流程。
- 保持既有失败重试、超时、错误策略能力不退化。

**Non-Goals:**
- 不重写全部启动任务业务实现。
- 不修改现有 `BootEnvironment` 的业务语义。
- 不引入新的可视化流程配置编辑器。

## Decisions

1. 启动核心服务抽象
- 新增 `IStartupOrchestrator`（纯 C#）作为统一入口，暴露 `RunAsync(BootEnvironment, CancellationToken)` 与 `ShutdownAsync(CancellationToken)`。
- `StartupPipeline` 继续作为执行引擎，但其创建、任务注册和生命周期状态由 Orchestrator 统一管理。
- 核心层维护状态机（Idle/Running/Stopping/Stopped），避免重复启动或并发关闭造成状态错乱。

2. 代码化流程配置
- 将 `StartupProfile.Create(BootEnvironment)` 的硬编码 `switch` 升级为可扩展 C# 配置提供器（如 `IStartupProfileProvider`）。
- 默认实现 `CodeStartupProfileProvider` 使用 C# 脚本集中定义环境-任务映射。
- 支持测试替换 Provider，便于在 EditMode 下构造定制流程。

3. 任务注册与 Unity 解耦
- 保留 `StartupTaskRegistry`，但将安装入口从 `MonoBehaviour` 基类下沉为纯 C# `IStartupTaskInstaller`。
- Unity 侧若需要注入 Unity 相关任务，通过薄适配器将 Unity 依赖转译为 installer 输入，而非直接由核心层依赖 Unity 类型。

4. Unity 薄接入策略
- 将 Unity 层收敛为单脚本（保留或替换 `UnityStartupEntry`），仅负责：
  - 在 `Start()` 或手动调用时触发 `IStartupOrchestrator.RunAsync`。
  - 在 `OnDestroy()` 取消 token。
  - 可选注入 `IStartupView` 适配（无视图时使用 NullView）。
- Unity 脚本不再承担任务拼装、流程配置决策与重试策略判断。

5. RPC 启停支持
- 在 Agent Bridge / RPC 调用链增加 `startup.run` 与 `startup.stop`（或等价方法），直接调用同一 `IStartupOrchestrator`。
- 当无 Unity 场景对象时，RPC 使用核心层默认 NullView 和代码化配置执行，满足“不播放 Unity 也能启停”的自动化诉求。

6. 兼容迁移
- 第一阶段保留旧接口外观（例如 `StartupPipelineLauncher`）并内部委托新 Orchestrator，避免调用方一次性改造。
- 第二阶段逐步收敛旧的 Unity Installer Base 和多组件绑定路径。

## Risks / Trade-offs

- [风险] 历史任务中隐式依赖 `MonoBehaviour`/场景对象，解耦后运行失败。
  - [缓解] 在任务注册期显式标注 Unity 依赖任务；核心层执行前做依赖检查并给出可诊断错误。
- [风险] 同时支持旧入口与新入口导致短期结构重复。
  - [缓解] 采用阶段性迁移并定义移除时间点，控制过渡周期。
- [风险] RPC 在非 Play Mode 触发时，部分任务本身不具备执行条件。
  - [缓解] 为环境配置提供可切换任务集（例如 headless profile），并在启动前校验执行上下文。

## Migration Plan

1. 引入 `IStartupOrchestrator` 与代码化 `IStartupProfileProvider`，保持旧 API 可用。
2. 将 `StartupPipelineLauncher` 与 Unity 入口改为调用 Orchestrator。
3. 下沉 installer 抽象，新增 Unity 薄适配层。
4. 接入 RPC 启停方法并打通无场景对象执行路径。
5. 增加/更新 EditMode 回归测试，覆盖核心编排、状态机和配置切换。
6. 清理多余 MonoBehaviour 启动拼装逻辑，保留一个 Unity 入口脚本。

## Open Questions

- `ShutdownAsync` 的职责边界是否包含网络断连、SDK 释放、场景卸载全流程，还是仅管理启动任务生命周期？
- 对非 Play Mode 执行，默认是否启用专用 profile（跳过场景/渲染相关任务）？
- RPC 启停接口是否需要返回结构化阶段进度，便于外部自动化等待与超时治理？
