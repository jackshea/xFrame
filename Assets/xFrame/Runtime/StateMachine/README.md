# xFrame 状态机系统

基于状态模式实现的通用有限状态机系统。

## 功能特性

- ✅ 支持不带上下文的简单状态机
- ✅ 支持带上下文的泛型状态机
- ✅ 状态生命周期管理（OnEnter、OnUpdate、OnExit）
- ✅ 状态改变事件通知
- ✅ 状态机管理模块，支持多个状态机实例
- ✅ 自动更新机制
- ✅ 完整的示例代码

## 核心组件

### 1. IState / IState<TContext>
状态接口，定义状态的基本行为：
- `OnEnter()` - 进入状态时调用
- `OnUpdate()` - 状态更新时调用
- `OnExit()` - 退出状态时调用

### 2. StateBase / StateBase<TContext>
状态基类，提供默认的空实现，方便继承。

### 3. StateMachine / StateMachine<TContext>
状态机核心类，负责管理状态的切换和更新。

### 4. StateMachineModule
状态机管理模块，支持创建和管理多个状态机实例。

## 使用方法

### 基本用法（不带上下文）

```csharp
// 创建状态机
var stateMachine = new StateMachine();

// 添加状态
stateMachine.AddState(new IdleState());
stateMachine.AddState(new MoveState());

// 切换状态
stateMachine.ChangeState<IdleState>();

// 在Update中更新状态机
void Update()
{
    stateMachine.Update();
}
```

### 高级用法（带上下文）

```csharp
// 定义上下文
public class PlayerContext
{
    public GameObject GameObject { get; set; }
    public float Health { get; set; }
    public float MoveSpeed { get; set; }
}

// 定义状态
public class PlayerIdleState : StateBase<PlayerContext>
{
    public override void OnEnter(PlayerContext context)
    {
        Debug.Log("进入待机状态");
    }

    public override void OnUpdate(PlayerContext context)
    {
        // 状态逻辑
    }

    public override void OnExit(PlayerContext context)
    {
        Debug.Log("退出待机状态");
    }
}

// 使用状态机
var context = new PlayerContext { Health = 100f, MoveSpeed = 5f };
var stateMachine = new StateMachine<PlayerContext>(context);

stateMachine.AddState(new PlayerIdleState());
stateMachine.AddState(new PlayerMoveState());

// 监听状态改变事件
stateMachine.OnStateChanged += (prev, next) =>
{
    Debug.Log($"状态改变: {prev?.GetType().Name} -> {next?.GetType().Name}");
};

// 切换状态
stateMachine.ChangeState<PlayerIdleState>();
```

### 使用状态机模块

```csharp
// 在xFrameBootstrapper中注册模块
public class GameBootstrapper : xFrameBootstrapper
{
    protected override void RegisterModules()
    {
        base.RegisterModules();
        RegisterModule<StateMachineModule>();
    }
}

// 使用模块创建状态机
var module = GetModule<StateMachineModule>();
var context = new PlayerContext(gameObject);
var stateMachine = module.CreateStateMachine("PlayerSM", context, autoUpdate: true);

// 添加状态
stateMachine.AddState(new PlayerIdleState());
stateMachine.AddState(new PlayerMoveState());

// 切换状态
stateMachine.ChangeState<PlayerIdleState>();

// 模块会自动更新所有autoUpdate为true的状态机
```

## 示例代码

查看 `Examples` 文件夹中的完整示例：
- `PlayerContext.cs` - 玩家上下文定义
- `PlayerIdleState.cs` - 待机状态
- `PlayerMoveState.cs` - 移动状态
- `PlayerJumpState.cs` - 跳跃状态
- `PlayerDeadState.cs` - 死亡状态
- `PlayerStateMachineExample.cs` - 完整使用示例

## 最佳实践

1. **状态设计原则**
   - 每个状态应该职责单一
   - 状态之间的转换逻辑应该清晰
   - 避免在状态中直接引用其他状态

2. **上下文设计**
   - 上下文应该包含状态需要的所有共享数据
   - 避免在上下文中放置过多的逻辑
   - 使用属性而不是公共字段

3. **状态转换**
   - 在外部控制状态转换，而不是在状态内部
   - 使用事件通知状态改变
   - 避免频繁的状态切换

4. **性能优化**
   - 复用状态实例，避免频繁创建
   - 对于不需要每帧更新的状态机，设置autoUpdate为false
   - 使用状态机模块统一管理多个状态机

## API 参考

### StateMachine

| 方法 | 说明 |
|------|------|
| `AddState<TState>(state)` | 添加状态 |
| `RemoveState<TState>()` | 移除状态 |
| `GetState<TState>()` | 获取状态实例 |
| `ChangeState<TState>()` | 切换到指定状态 |
| `Update()` | 更新当前状态 |
| `Stop()` | 停止状态机 |
| `Clear()` | 清空所有状态 |

### StateMachineModule

| 方法 | 说明 |
|------|------|
| `CreateStateMachine(name, autoUpdate)` | 创建状态机（不带上下文） |
| `CreateStateMachine<TContext>(name, context, autoUpdate)` | 创建状态机（带上下文） |
| `GetStateMachine(name)` | 获取状态机 |
| `RemoveStateMachine(name)` | 移除状态机 |
| `Update()` | 更新所有自动更新的状态机 |

## 许可证

MIT License
