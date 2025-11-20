using UnityEngine;
using VContainer;
using xFrame.Runtime.StateMachine;
using xFrame.Runtime.EventBus;

namespace xFrame.Examples.StateMachine
{
    /// <summary>
    /// 状态机模块使用示例（VContainer集成版本）
    /// 展示如何通过依赖注入使用状态机模块
    /// </summary>
    public class StateMachineModuleUsageExample : MonoBehaviour
    {
        private IStateMachineService _stateMachineService;
        private StateMachine<PlayerContext> _playerStateMachine;

        /// <summary>
        /// VContainer依赖注入构造方法
        /// </summary>
        [Inject]
        public void Construct(IStateMachineService stateMachineService)
        {
            _stateMachineService = stateMachineService;
            Debug.Log("[StateMachineModuleUsageExample] 状态机模块已注入");
        }

        void Start()
        {
            // 创建玩家上下文
            var context = new PlayerContext(gameObject)
            {
                Health = 100f,
                MoveSpeed = 5f
            };

            // 使用模块创建状态机（autoUpdate=true表示自动更新）
            _playerStateMachine = _stateMachineService.CreateStateMachine("PlayerSM", context, autoUpdate: true);

            // 添加状态
            _playerStateMachine.AddState(new PlayerIdleState());
            _playerStateMachine.AddState(new PlayerMoveState());
            _playerStateMachine.AddState(new PlayerJumpState());
            _playerStateMachine.AddState(new PlayerDeadState());

            // 通过事件总线监听状态改变事件
            xFrameEventBus.SubscribeTo<StateChangedEvent<PlayerContext>>(OnPlayerStateChanged);

            // 切换到初始状态
            _playerStateMachine.ChangeState<PlayerIdleState>();

            Debug.Log("[StateMachineModuleUsageExample] 玩家状态机已创建并启动");
        }

        void Update()
        {
            // 模拟输入
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _playerStateMachine.ChangeState<PlayerJumpState>();
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || 
                Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
            {
                if (_playerStateMachine.CurrentState is not PlayerMoveState)
                {
                    _playerStateMachine.ChangeState<PlayerMoveState>();
                }
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                _playerStateMachine.ChangeState<PlayerDeadState>();
            }

            // 注意：不需要手动调用 _playerStateMachine.Update()
            // VContainer的ITickable会自动更新所有autoUpdate为true的状态机
        }

        private void OnPlayerStateChanged(ref StateChangedEvent<PlayerContext> evt)
        {
            var prevName = evt.PreviousState?.GetType().Name ?? "None";
            var newName = evt.CurrentState?.GetType().Name ?? "None";
            Debug.Log($"[StateMachineModuleUsageExample] 状态改变: {prevName} -> {newName}");
        }

        void OnDestroy()
        {
            // 取消事件订阅
            xFrameEventBus.UnsubscribeFrom<StateChangedEvent<PlayerContext>>(OnPlayerStateChanged);
            
            // 清理状态机
            if (_stateMachineService != null && _playerStateMachine != null)
            {
                _stateMachineService.RemoveStateMachine("PlayerSM");
                Debug.Log("[StateMachineModuleUsageExample] 玩家状态机已移除");
            }
        }
    }
}
