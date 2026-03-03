---
name: unity-rpc
description: 通过 Fleck WebSocket 调用 Unity Agent Bridge(JSON-RPC 2.0) 的标准技能。
---

# Unity RPC Skill

## 目的

通过本仓库的 `scripts/agent/unity_rpc_client.py`，让 OpenCode 以统一流程调用 Unity Editor 内的 Agent Bridge。

## 前置条件

1. Unity Editor 已打开当前工程。
2. `xFrame/AgentBridge` 已在 Editor 中启动（默认自动启动，菜单可手动 Start/Stop）。
3. Python 环境可用。
4. 若缺少依赖，先安装：

```bash
pip install websocket-client
```

## 标准调用流程

1. 使用 `call` 子命令调用业务方法。
2. 客户端会自动先发 `agent.ping`。
3. 若返回未认证（`-32001`），客户端自动调用 `agent.authenticate` 并重试业务方法。

## 命令模板

```bash
python scripts/agent/unity_rpc_client.py call \
  --endpoint ws://127.0.0.1:17777 \
  --token xframe-dev-token \
  --method unity.gameobject.find \
  --params '{"name":"Player"}'
```

## 常用方法

- `agent.commands`
- `unity.gameobject.find`
- `unity.component.invoke`
- `unity.reflect.invoke`（需在 Unity 配置中启用反射桥接）

## 故障排查

- 连接失败：确认 Unity 正在运行且端口未占用。
- 401/`-32001`：检查 token。
- `-32012`：反射桥接未启用或类型/程序集不在白名单。
