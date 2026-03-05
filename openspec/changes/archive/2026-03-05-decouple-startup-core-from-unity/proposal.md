## Why

当前启动流程虽然已经引入 `StartupPipeline`，但入口、任务安装、环境选择和表现层仍依赖多个 Unity `MonoBehaviour` 组件协作，导致以下问题：

- 启动行为与 Unity 生命周期强耦合，难以在非 Play Mode 下独立触发。
- Unity RPC 侧无法直接复用同一套启动/关闭逻辑，自动化联调和 CI 场景成本高。
- 启动配置主要通过场景对象与 Inspector 驱动，不利于代码化版本管理和环境复现。

需要将启动流程收敛为“纯 C# 核心编排 + Unity 薄接入”，使 Unity 层只保留一个简单入口脚本，其余逻辑在独立核心层执行。

## What Changes

- 重构启动架构，建立独立于 Unity 的启动核心层，统一承担流程编排、生命周期管理与任务执行。
- 将环境流程配置改为 C# 脚本声明，移除/弱化对场景 Inspector 配置的依赖。
- Unity 侧收敛为单入口脚本，仅调用核心层的启动方法并传递必要上下文。
- 增加可复用的启动/关闭 API，使 Unity RPC 在不播放场景（非 Play Mode）时也可触发核心流程。
- 约束启动任务边界：与 Unity API 交互的逻辑通过适配接口下沉到薄层，核心层保持纯 C#。

## Capabilities

### New Capabilities
- `startup-core-orchestration`: 提供与 Unity 生命周期解耦的启动/关闭核心编排服务。
- `startup-code-first-profile`: 使用 C# 代码定义环境到启动步骤的映射与策略。
- `startup-rpc-lifecycle-control`: 暴露可供 Unity RPC 调用的统一启动与关闭入口，无需依赖场景中多个脚本联动。

### Modified Capabilities
- `startup-unity-entry`: Unity 入口由多组件拼装改为单脚本薄调用模式。

## Impact

- 影响模块：
  - `Assets/xFrame/Runtime/Bootstrapper/`（启动核心编排重构）
  - `Assets/xFrame/Runtime/Unity/Startup/`（入口与适配层收敛）
  - `scripts/agent/UnityRpcClient/` 或 Agent Bridge 对应调用链（新增/调整启动与关闭 RPC）
- 测试影响：需要新增 EditMode 单测覆盖核心层状态机、流程配置与失败治理；补充 RPC 触发路径回归测试。
- 兼容性影响：保留现有 `BootEnvironment` 语义，迁移期间提供兼容适配，避免一次性破坏现有任务实现。
