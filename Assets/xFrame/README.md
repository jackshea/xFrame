# xFrame

xFrame 是面向 Unity 项目的轻量级游戏框架，当前仓库基于 `Unity 2021.3.51f1` 与 `URP 12.1.16` 维护。本文档只保留开发者最先需要了解的信息，具体模块设计请查阅 `Assets/xFrame/Docs/`。

## 开发者先看这里

- Unity 版本：`2021.3.51f1`
- 主要依赖：`VContainer`、`UniTask`、`GenericEventBus`、`Addressables`
- 框架代码：`Assets/xFrame/Runtime/`
- 编辑器扩展：`Assets/xFrame/Editor/`
- 测试目录：`Assets/xFrame/Tests/`
- 示例目录：`Assets/xFrame.Examples/`
- 业务代码建议放在：`Assets/Game/`

## 目录说明

### Runtime

`Assets/xFrame/Runtime/` 是框架运行时代码，当前主要模块包括：

- `Bootstrapper` / `Unity/Bootstrapper`：框架启动与 Unity 生命周期接入
- `Core`：应用入口、日志、调度、时间等基础设施
- `DI`：基于 VContainer 的依赖注入封装
- `EventBus`：事件发布订阅
- `Logging`：日志能力与输出适配
- `MVVM`：轻量级 UI 数据绑定
- `Networking`：网络抽象与 AgentBridge
- `ObjectPool`：对象池
- `Persistence`：持久化、迁移、安全与存储提供者
- `ResourceManager`：资源管理
- `Scheduler`：任务调度
- `Serialization`：序列化
- `StateMachine`：状态机
- `UI`：UI 框架与组件复用
- `Utilities` / `Platform` / `DataStructures`：通用基础能力

### Editor

`Assets/xFrame/Editor/` 放置仅编辑器可用的能力，包括：

- 配置管理窗口
- 测试运行辅助
- AgentBridge 的 Editor 侧 WebSocket Host

### Tests

`Assets/xFrame/Tests/` 按 Unity Test Framework 约定拆分为：

- `EditMode`：纯逻辑与编辑器侧验证
- `PlayMode`：运行时行为验证

当前已覆盖 DI、EventBus、Logging、MVVM、Networking、ObjectPool、Persistence、ResourceManager、Scheduler、Serialization、StateMachine、UI、Utilities 等模块。

## 如何接入

1. 打开项目时使用 `Unity 2021.3.51f1`。
2. 场景内接入框架启动入口，通常使用 `xFrameBootstrapper` 相关预制体或启动链路。
3. 新增运行时服务时，优先通过 VContainer 注册并以接口对外暴露。
4. 新增编辑器工具时，放入 `Assets/xFrame/Editor/`，避免运行时代码依赖 Editor。

如果你只是要在当前仓库内继续开发，优先遵守现有目录分层，不要让 `Assets/xFrame` 直接依赖 `Assets/Game`。

## 测试与验证

- 测试框架：`NUnit + Unity Test Framework`
- 推荐顺序：受影响单测 -> 对应测试集 -> 全量测试
- 测试位置：
  - EditMode：`Assets/xFrame/Tests/EditMode/`
  - PlayMode：`Assets/xFrame/Tests/PlayMode/`
- 在 Unity Editor 中可通过 `Window > General > Test Runner` 执行
- 仓库协作默认通过 `unity-rpc` skill 触发 Unity 侧测试

## AgentBridge

如果需要让外部工具调用 Unity Editor，可使用 AgentBridge：

- Runtime 路由：`Assets/xFrame/Runtime/Networking/AgentBridge/`
- Editor Host：`Assets/xFrame/Editor/AgentBridge/`
- 默认端点：`ws://127.0.0.1:17777`
- 端口冲突时会自动分配可用端口，并写入 `UserSettings/AgentBridgeSettings.json`

示例：

```bash
python3 .agents/skills/unity-rpc/scripts/unity-rpc.py call --method agent.commands --params '{}'
```

## 推荐阅读

- UI 框架：`Assets/xFrame/Docs/UI框架.md`
- UI 组件复用：`Assets/xFrame/Docs/UI组件复用设计.md`
- 有限状态机：`Assets/xFrame/Docs/有限状态机.md`
- 数据持久化：`Assets/xFrame/Docs/数据持久化模块设计文档.md`
- ObjectPool：`Assets/xFrame/Docs/ObjectPool/README.md`
- 快速上手对象池：`Assets/xFrame/Docs/ObjectPool/QuickStart.md`

## 不放在 README 的内容

README 不再展开各模块 API 示例。原因很简单：这些信息更新频率高、容易过时，也不适合作为开发者入口。后续如果需要补充示例，优先放到模块文档、示例工程或测试里。
