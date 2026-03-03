# OpenCode Unity Fleck WebSocket Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让 OpenCode 通过 WebSocket(JSON-RPC 2.0) 与 Unity Editor 建立连接，并可安全调用 Unity 代码。

**Architecture:** Runtime 层提供协议与路由核心，Editor 层用 Fleck 承载传输通道与连接生命周期。默认白名单命令路由，开发模式可启用受限反射调用。通过仓库内 Skill 与 Python CLI 统一 OpenCode 调用入口。

**Tech Stack:** Unity 2021.3, C#, Fleck, VContainer, NUnit(EditMode), Python 3

---

### Task 1: 先写路由与认证失败用例（RED）

**Files:**
- Create: `Assets/xFrame/Tests/EditMode/NetworkingTests/AgentBridgeRouterTests.cs`

**Step 1: Write the failing test**

- 新增测试：未认证请求调用白名单命令时返回 `-32001`。

**Step 2: Run test to verify it fails**

Run: `dotnet test "xFrame.EditModeTests.csproj" --filter AgentBridgeRouterTests`  
Expected: FAIL（类型或实现尚不存在）

**Step 3: Write minimal implementation**

- 暂不实现，进入下一任务创建最小实现。

**Step 4: Run test to verify it passes**

- 在任务 2 完成后回到此步骤。

### Task 2: 实现 Runtime 协议与路由最小闭环（GREEN）

**Files:**
- Create: `Assets/xFrame/Runtime/Networking/AgentBridge/AgentBridgeOptions.cs`
- Create: `Assets/xFrame/Runtime/Networking/AgentBridge/JsonRpcModels.cs`
- Create: `Assets/xFrame/Runtime/Networking/AgentBridge/IAgentRpcCommandHandler.cs`
- Create: `Assets/xFrame/Runtime/Networking/AgentBridge/AgentCommandRegistry.cs`
- Create: `Assets/xFrame/Runtime/Networking/AgentBridge/AgentRpcRouter.cs`

**Step 1: Write minimal implementation**

- 完成认证门禁、命令注册与标准 JSON-RPC 错误回包。

**Step 2: Run target test**

Run: `dotnet test "xFrame.EditModeTests.csproj" --filter AgentBridgeRouterTests`  
Expected: PASS

### Task 3: 增加白名单命令处理器（RED/GREEN）

**Files:**
- Create: `Assets/xFrame/Runtime/Networking/AgentBridge/Commands/PingCommandHandler.cs`
- Create: `Assets/xFrame/Runtime/Networking/AgentBridge/Commands/AuthenticateCommandHandler.cs`
- Create: `Assets/xFrame/Runtime/Networking/AgentBridge/Commands/ListCommandsHandler.cs`
- Create: `Assets/xFrame/Runtime/Networking/AgentBridge/Commands/FindGameObjectCommandHandler.cs`
- Create: `Assets/xFrame/Runtime/Networking/AgentBridge/Commands/InvokeComponentCommandHandler.cs`
- Modify: `Assets/xFrame/Runtime/Networking/NetworkingServiceExtensions.cs`

**Step 1: Write failing tests**

- 新增 `agent.commands` 返回已注册命令。
- 新增 `unity.gameobject.find` / `unity.component.invoke` 参数校验失败用例。

**Step 2: Verify RED**

Run: `dotnet test "xFrame.EditModeTests.csproj" --filter AgentBridgeRouterTests`  
Expected: FAIL

**Step 3: Implement minimal code**

- 注册默认命令处理器并返回结构化结果。

**Step 4: Verify GREEN**

Run: `dotnet test "xFrame.EditModeTests.csproj" --filter AgentBridgeRouterTests`  
Expected: PASS

### Task 4: 增加混合模式反射调用（RED/GREEN）

**Files:**
- Create: `Assets/xFrame/Runtime/Networking/AgentBridge/AgentReflectionInvoker.cs`
- Modify: `Assets/xFrame/Runtime/Networking/AgentBridge/AgentRpcRouter.cs`
- Modify: `Assets/xFrame/Tests/EditMode/NetworkingTests/AgentBridgeRouterTests.cs`

**Step 1: Write failing tests**

- `EnableReflectionBridge=false` 时 `unity.reflect.invoke` 返回 `-32012`。
- 启用后但类型不在白名单，仍返回 `-32012`。

**Step 2: Verify RED**

Run: `dotnet test "xFrame.EditModeTests.csproj" --filter AgentBridgeRouterTests`  
Expected: FAIL

**Step 3: Implement minimal code**

- 实现受限反射调用，仅允许白名单程序集与类型前缀。

**Step 4: Verify GREEN**

Run: `dotnet test "xFrame.EditModeTests.csproj" --filter AgentBridgeRouterTests`  
Expected: PASS

### Task 5: 接入 Fleck 传输层（Editor）

**Files:**
- Create: `Assets/xFrame/Editor/AgentBridge/FleckAgentBridgeServer.cs`
- Create: `Assets/xFrame/Editor/AgentBridge/AgentBridgeEditorBootstrap.cs`
- Modify: `Assets/xFrame/Editor/xFrame.Editor.asmdef`

**Step 1: Write failing integration-leaning test (可选最小化)**

- 若无可稳定自动化用例，至少补充可构造实例与启动参数校验的 EditMode 测试。

**Step 2: Implement minimal code**

- 使用 Fleck 启动 WebSocket 服务。
- 每条消息交由 `AgentRpcRouter` 处理并回包。

**Step 3: Verify**

Run: `dotnet test "xFrame.EditModeTests.csproj" --filter AgentBridge`  
Expected: PASS 或明确记录环境限制。

### Task 6: 提供 OpenCode Agent Skill 与客户端脚本

**Files:**
- Create: `skills/unity-rpc/SKILL.md`
- Create: `scripts/agent/unity_rpc_client.py`
- Create: `scripts/agent/test_unity_rpc_client.py`

**Step 1: Write failing Python tests**

- 覆盖参数解析、请求封装、错误响应处理。

**Step 2: Verify RED**

Run: `python -m unittest scripts/agent/test_unity_rpc_client.py`  
Expected: FAIL

**Step 3: Implement minimal code**

- 实现命令行 `call`，支持 `--endpoint --token --method --params`。

**Step 4: Verify GREEN**

Run: `python -m unittest scripts/agent/test_unity_rpc_client.py`  
Expected: PASS

### Task 7: 回归验证与文档补充

**Files:**
- Modify: `Assets/xFrame/README.md`
- Modify: `AI_WORKFLOW.md`（如需补充验证命令）

**Step 1: Run focused tests**

- `dotnet test "xFrame.EditModeTests.csproj" --filter AgentBridge`
- `python -m unittest scripts/agent/test_unity_rpc_client.py`

**Step 2: Run quick sanity checks**

- `python -m py_compile scripts/agent/unity_rpc_client.py`

**Step 3: Record results**

- 在最终说明中提供执行命令与结果摘要。
