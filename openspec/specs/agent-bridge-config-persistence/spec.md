## Purpose
定义 Agent Bridge 目标地址配置与持久化能力，保证配置可校验、可保存并可在重启后恢复。

## Requirements

### Requirement: Agent Bridge endpoint can be configured
系统 MUST 提供 Agent Bridge 目标地址配置能力，至少包含 IPv4/主机名格式的 IP(或 Host) 与端口字段，并在保存前执行合法性校验。

#### Scenario: Save valid endpoint
- **WHEN** 用户输入合法的 IP 与端口并触发保存
- **THEN** 系统保存该配置并将其标记为当前生效目标地址

#### Scenario: Reject invalid endpoint
- **WHEN** 用户输入非法 IP 或端口超出 1-65535 范围并触发保存
- **THEN** 系统拒绝保存该配置并返回可识别的错误信息

### Requirement: Agent Bridge endpoint configuration SHALL persist across restarts
系统 MUST 将已保存的 Agent Bridge 地址配置持久化到本地存储，并在应用重新启动后自动恢复。

#### Scenario: Load persisted endpoint on startup
- **WHEN** 系统启动且本地存在有效的 Agent Bridge 地址配置
- **THEN** 系统加载该配置作为初始化连接参数

#### Scenario: Fallback when persisted endpoint is invalid
- **WHEN** 系统启动且本地配置缺失或校验失败
- **THEN** 系统回退到默认地址配置并记录告警日志
