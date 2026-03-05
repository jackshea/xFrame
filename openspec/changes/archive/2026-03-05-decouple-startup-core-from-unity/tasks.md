## 1. 启动核心层抽象

- [x] 1.1 新增 `IStartupOrchestrator` 及默认实现，统一封装 `RunAsync` / `ShutdownAsync` 生命周期
- [x] 1.2 为 Orchestrator 增加状态机与并发保护（重复启动、重复关闭、取消传播）
- [x] 1.3 让现有 `StartupPipeline` 执行能力通过 Orchestrator 对外暴露，保持重试/超时/失败策略一致

## 2. 流程配置代码化

- [x] 2.1 抽象 `IStartupProfileProvider`，将环境流程映射从硬编码 `switch` 迁移到 C# 配置提供器
- [x] 2.2 提供默认 `CodeStartupProfileProvider`，覆盖 `Release`、`DevFull`、`DevSkipToBattle` 等现有环境
- [x] 2.3 支持测试替换 profile provider，确保可在 EditMode 构造定制流程

## 3. Unity 薄接入改造

- [x] 3.1 将 Unity 侧入口收敛为单脚本，仅负责调用核心层启动方法与生命周期 token 管理
- [x] 3.2 下沉任务安装抽象为纯 C# installer，Unity 层仅保留必要适配
- [x] 3.3 提供 NullView/无 UI 运行模式，避免核心流程强依赖 Unity 视图对象

## 4. Unity RPC 启停能力

- [x] 4.1 在 Agent Bridge/RPC 链路增加启动与关闭方法（`startup.run` / `startup.stop` 或等价接口）
- [x] 4.2 复用同一 Orchestrator，确保 RPC 与 Unity 入口执行一致流程
- [x] 4.3 补充非 Play Mode 执行校验与错误提示，明确不支持任务的诊断信息

## 5. 测试与回归

- [x] 5.1 新增 EditMode 单测：覆盖 Orchestrator 状态机、取消与失败治理
- [x] 5.2 新增配置相关单测：覆盖 profile provider 的环境映射与替换机制
- [x] 5.3 新增/更新 RPC 路径测试：覆盖启动成功、关闭成功、重复调用与异常场景
- [x] 5.4 运行受影响测试集并记录结果，确认现有启动行为与关键回归不被破坏
  - 记录：`dotnet test Tests/StartupPipeline.DotnetTests/StartupPipeline.DotnetTests.csproj` => 11/11 通过
  - 记录：`unity.tests.run` EditMode 全量 => total=435, failed=7（均为 `SchedulerServiceTests` 既有失败，未出现启动流程/RPC 新增测试失败）
