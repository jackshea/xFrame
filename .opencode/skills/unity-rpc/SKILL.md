---
name: unity-rpc
description: Use when you need to call Unity Agent Bridge(JSON-RPC 2.0 over WebSocket) from this repository and want a consistent, authenticated CLI workflow.
---

# Unity RPC

通过仓库内的 `scripts/agent/UnityRpcClient`（C#）调用 Unity Editor 内的 Agent Bridge。

## 前置条件

1. Unity Editor 已打开当前工程。
2. `xFrame/AgentBridge` 已在 Editor 中启动（默认自动启动，可在菜单手动 Start/Stop）。
3. 本机可用 `.NET SDK 8+`（`dotnet --version`）。

## 标准调用流程

1. 使用 `call` 子命令执行业务方法。
2. 客户端会先发送 `agent.ping`。
3. 若返回未认证（`-32001`），客户端会自动执行 `agent.authenticate` 后重试业务方法。

## 命令模板

```bash
dotnet run --project scripts/agent/UnityRpcClient/UnityRpcClient.csproj -- call \
  --endpoint ws://10.22.61.131:17777 \
  --token xframe-dev-token \
  --method unity.gameobject.find \
  --params '{"name":"Player"}'
```

## 常用方法

- `agent.commands`
- `unity.gameobject.find`
- `unity.component.invoke`
- `unity.reflect.invoke`（需在 Unity 配置中启用反射桥接）

## 执行单元测试

1. 触发测试（推荐先跑 `EditMode`）：

```bash
dotnet run --project scripts/agent/UnityRpcClient/UnityRpcClient.csproj -- call \
  --endpoint ws://10.22.61.131:17777 \
  --token xframe-dev-token \
  --method unity.tests.run \
  --params '{"mode":"EditMode"}'
```

2. 查询最近一次测试结果：

```bash
dotnet run --project scripts/agent/UnityRpcClient/UnityRpcClient.csproj -- call \
  --endpoint ws://10.22.61.131:17777 \
  --token xframe-dev-token \
  --method unity.tests.lastResult \
  --params '{}'
```

3. 若需按名称过滤测试，可在 `unity.tests.run` 里传 `filter`（例如 `{"mode":"EditMode","filter":"SchedulerServiceTests"}`）。

## 故障排查

- 连接失败：确认 Unity 正在运行且端口未被占用。
- 401/`-32001`：检查 token 是否正确。
- `-32012`：反射桥接未启用，或类型/程序集不在白名单。
