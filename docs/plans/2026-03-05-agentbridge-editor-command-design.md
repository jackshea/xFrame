# AgentBridge 编辑器外部命令设计

## 目标

在现有 `FleckAgentBridgeServer`（WebSocket + JSON-RPC）基础上扩展 Unity Editor 可控命令，支持：

1. 外部触发编辑器菜单命令（如打开设置窗口）。
2. 外部触发 EditMode/PlayMode 测试执行。
3. 返回结构化 JSON 结果，便于终端工具自动判断成功/失败。

## 设计边界

- 不新建独立监听器，不引入第二套端口与协议。
- 复用现有能力：认证门禁、主线程调度、错误码、日志。
- Editor 专属能力放在 `Assets/xFrame/Editor/AgentBridge/`，避免污染 Runtime 层。

## 协议扩展

新增 RPC 方法：

1. `unity.editor.executeMenu`
   - `params.menuPath`：菜单路径（必填），例如 `Edit/Project Settings...`。
   - 返回：`{ executed: bool, menuPath: string }`。
   - 失败：参数缺失返回 `-32602`。

2. `unity.tests.run`
   - `params.mode`：`EditMode`/`PlayMode`，默认 `EditMode`。
   - `params.filter`：可选过滤条件（测试名、类名或命名空间片段）。
   - `params.timeoutMs`：可选等待超时。
   - 返回：
     - 成功：`{ started: true, mode: string, waitCompleted: bool, summary?: {...} }`
     - 超时：`started=true` 但 `waitCompleted=false`。
     - 参数非法：`-32602`。

## 执行模型

### 1) 主线程约束

- Fleck 收到消息后仍由现有 `EditorMainThreadDispatcher` 切回主线程。
- 命令处理器内部可直接访问 `EditorApplication.ExecuteMenuItem` 与 `TestRunnerApi`。

### 2) 测试执行回调

- 使用 `UnityEditor.TestTools.TestRunner.Api.TestRunnerApi` + `ICallbacks`。
- 在回调中收集：总数、通过数、失败数、跳过数、耗时、失败明细。
- 命令可选“同步等待结果”：
  - 若在 `timeoutMs` 内完成，直接返回 summary。
  - 超时则返回已启动状态，外部后续可再次查询（后续迭代项）。

## 错误处理与兼容性

- 命令处理器内部捕获异常并转 `AgentRpcExecutionResult.Failure`。
- 保留原有 `AgentRpcRouter` 行为，不改协议基础结构。
- `agent.commands` 自动包含新命令，保证外部可发现性。

## 测试策略

优先 EditMode 单元测试覆盖：

1. 未认证调用新命令返回 `-32001`（复用既有门禁验证）。
2. `agent.commands` 返回新增命令名称。
3. 非法参数返回 `-32602`。
4. `unity.editor.executeMenu` 在可执行菜单路径上返回 `executed=true`。

> `unity.tests.run` 的完整集成验证依赖 Unity Test Runner 运行环境，单测先覆盖参数与启动分支，结果汇总细节通过日志与手工联调确认。

## 影响文件

- 新增：`Assets/xFrame/Editor/AgentBridge/EditorExecuteMenuCommandHandler.cs`
- 新增：`Assets/xFrame/Editor/AgentBridge/EditorRunTestsCommandHandler.cs`
- 修改：`Assets/xFrame/Editor/AgentBridge/FleckAgentBridgeServer.cs`
- 修改：`Assets/xFrame/Tests/EditMode/NetworkingTests/AgentBridgeRouterTests.cs`

## 验证命令

```bash
dotnet test "xFrame.EditModeTests.csproj" --filter AgentBridgeRouterTests
```
