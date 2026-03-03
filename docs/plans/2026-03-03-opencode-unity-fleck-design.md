# OpenCode 通过 Fleck 调用 Unity 代码设计文档

## 1. 目标

- 在 Unity Editor 内通过 Fleck 暴露 WebSocket 服务端。
- 使用 JSON-RPC 2.0 协议让 OpenCode 可以请求执行 Unity 代码。
- 默认采用白名单命令路由，开发模式可开启受限反射调用（混合模式）。
- 提供仓库内 Agent Skill，统一 OpenCode 调用入口。

## 2. 约束与前提

- Fleck 当前以 `Assets/ThirdParty/Fleck/Fleck.asmdef` 引入，且限定 `Editor` 平台。
- 因此 WebSocket Host 放在 `xFrame.Editor` 程序集，避免 Runtime 对 Editor 依赖。
- 协议层与路由层尽量放在 Runtime，方便单元测试与复用。
- 仅支持本机环回连接（默认 `127.0.0.1`）。

## 3. 总体架构

- `Transport`（Editor）：`FleckAgentBridgeServer` 负责连接管理、消息收发。
- `Protocol`（Runtime）：`JsonRpcEnvelope` + `JsonRpcError` + 序列化工具。
- `Routing`（Runtime）：`AgentRpcRouter` + `AgentCommandRegistry`。
- `Execution`（Runtime/Editor）：主线程分发器 + 命令处理器。

## 4. 安全模型

- 首次请求必须执行 `agent.authenticate`。
- token 可配置，未认证连接仅允许 `agent.authenticate` 与 `agent.ping`。
- 白名单命令通过 `IAgentRpcCommandHandler` 注册。
- `unity.reflect.invoke` 仅在 `EnableReflectionBridge=true` 时可用，且受如下约束：
  - 程序集白名单
  - 类型名前缀白名单
  - 仅允许公共方法

## 5. 消息协议（JSON-RPC 2.0）

- Request：`{ jsonrpc, id, method, params }`
- Response：`{ jsonrpc, id, result }`
- Error：`{ jsonrpc, id, error: { code, message, data } }`
- 通知：无 `id` 的请求。

错误码：

- 标准：`-32700/-32600/-32601/-32602/-32603`
- 业务：
  - `-32001` 未认证
  - `-32012` 反射调用被禁用或被拒绝

## 6. 默认命令集

- `agent.ping`：连通性探测。
- `agent.authenticate`：连接认证。
- `agent.commands`：获取白名单命令集合。
- `unity.gameobject.find`：按名称查找对象。
- `unity.component.invoke`：调用指定对象组件方法（参数数组）。
- `unity.reflect.invoke`：受限反射调用（开发模式）。

## 7. Agent Skill 设计

- 在仓库新增 `skills/unity-rpc/SKILL.md`。
- Skill 统一约束调用链：
  - 先 `agent.ping`
  - 若未认证则调用 `agent.authenticate`
  - 再执行业务方法
- Skill 通过 `scripts/agent/unity_rpc_client.py` 发起 WebSocket JSON-RPC 调用。

## 8. 测试与验证

- EditMode 单测：
  - 认证门禁
  - 白名单路由
  - 反射开关控制
  - `unity.component.invoke` 参数校验
- Python 侧：
  - CLI 参数解析
  - JSON-RPC 报文构造
  - 响应错误码处理

## 9. 交付物

- Runtime：协议、路由、命令处理器、配置。
- Editor：Fleck Host 与自动启动入口。
- Tests：EditMode 新增 AgentBridge 相关回归测试。
- Skill：仓库内可复用调用模板与使用文档。
