---
name: unity-rpc
description: Use when you need to call Unity Agent Bridge(JSON-RPC 2.0 over WebSocket) from this repository and want a consistent CLI workflow.
---

# Unity RPC

通过仓库内的 `scripts/agent/unity-rpc.py`（Python 单文件脚本）调用 Unity Editor 内的 Agent Bridge。

## 前置条件

1. Unity Editor 已打开当前工程。
2. `xFrame/AgentBridge` 已在 Editor 中启动（默认自动启动，可在菜单手动 Start/Stop）。
3. 本机可用 `Python 3`（推荐 `python3 --version`，无需安装额外依赖）。

## 标准调用流程

1. 使用 `call` 子命令执行业务方法。
2. 客户端会先发送 `agent.ping`。
3. 连通后再执行业务方法；`unity.tests.run` 会持续输出测试进度事件。

## 环境变量（推荐）

- `UNITY_RPC_HOST`：Unity 运行主机 IP（例如 `10.22.61.131`）。
- `UNITY_RPC_PORT`：Agent Bridge 端口（默认 `17777`，可选）。
- `UNITY_RPC_ENDPOINT`：完整地址（例如 `ws://10.22.61.131:17777`，设置后优先于 `UNITY_RPC_HOST`/`UNITY_RPC_PORT`）。

设置示例：

PowerShell：

```powershell
$env:UNITY_RPC_HOST = "10.22.61.131"
$env:UNITY_RPC_PORT = "17777"
```

bash/zsh：

```bash
export UNITY_RPC_HOST="10.22.61.131"
export UNITY_RPC_PORT="17777"
```

## 命令模板

```bash
python3 scripts/agent/unity-rpc.py call \
  --method unity.gameobject.find \
  --params '{"name":"Player"}'
```

## 超时建议（测试场景）

- 单元测试可能耗时较长，建议显式传 `--timeout`（单位：秒），例如 `3600`。
- 若在 Agent/CLI 中执行，还应同步拉长命令执行超时，避免进程被外层提前取消。

## 常用方法

- `agent.commands`
- `unity.gameobject.find`
- `unity.component.invoke`
- `unity.reflect.invoke`（需在 Unity 配置中启用反射桥接）

## 执行单元测试

1. 触发测试（推荐先跑 `EditMode`）：

```bash
python3 scripts/agent/unity-rpc.py call \
  --timeout 3600 \
  --method unity.tests.run \
  --params '{"mode":"EditMode"}'
```

2. 查询最近一次测试结果：

```bash
python3 scripts/agent/unity-rpc.py call \
  --method unity.tests.lastResult \
  --params '{}'
```

3. 若需按名称过滤测试，可在 `unity.tests.run` 里传 `filter`（例如 `{"mode":"EditMode","filter":"SchedulerServiceTests"}`）：

```bash
python3 scripts/agent/unity-rpc.py call \
  --timeout 3600 \
  --method unity.tests.run \
  --params '{"mode":"EditMode","filter":"SchedulerServiceTests"}'
```

## 故障排查

- 连接失败：确认 Unity 正在运行且端口未被占用。
- `missing endpoint`：检查 Unity 是否运行，以及 `UserSettings/AgentBridgeSettings.json` 是否写入了运行中的实例。
- `-32012`：反射桥接未启用，或类型/程序集不在白名单。
