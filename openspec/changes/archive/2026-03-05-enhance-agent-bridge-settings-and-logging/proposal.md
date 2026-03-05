## Why

当前 Agent Bridage 缺少可配置的网络连接参数，IP 与端口通常需要硬编码或临时修改，导致不同环境切换成本高且容易出错。同时缺乏结构化日志输出，联调与线上问题定位效率较低。

## What Changes

- 为 Agent Bridage 增加可配置的 IP 与端口设置入口。
- 将 IP 与端口配置持久化，支持重启后自动恢复上次有效配置。
- 新增 Agent Bridage 日志输出能力，覆盖连接建立、重连、收发消息、异常等关键路径。
- 统一日志上下文信息（如目标地址、连接状态、错误原因），便于排障与回归验证。

## Capabilities

### New Capabilities
- `agent-bridge-config-persistence`: 管理 Agent Bridage 的 IP/端口配置并支持持久化加载与保存。
- `agent-bridge-observability`: 提供 Agent Bridage 关键生命周期与通信流程的结构化日志输出。

### Modified Capabilities
- (none)

## Impact

- 影响模块：`Assets/xFrame/Runtime/` 下 Agent Bridage 相关运行时代码，以及可能的配置读取/写入辅助组件。
- 测试影响：需要补充 EditMode 回归测试，覆盖配置持久化与日志触发行为。
- 运行时行为：Agent Bridage 启动时将读取已保存配置；连接与异常路径将产生标准化日志。
