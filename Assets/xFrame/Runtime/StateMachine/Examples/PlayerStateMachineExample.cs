using UnityEngine;

namespace xFrame.StateMachine.Examples
{
    /// <summary>
    /// 玩家状态机使用示例
    /// </summary>
    public class PlayerStateMachineExample : MonoBehaviour
    {
        private StateMachine<PlayerContext> _stateMachine;
        private PlayerContext _context;

        private void Start()
        {
            // 创建上下文
            _context = new PlayerContext(gameObject);

            // 创建状态机
            _stateMachine = new StateMachine<PlayerContext>(_context);

            // 添加状态
            _stateMachine.AddState(new PlayerIdleState());
            _stateMachine.AddState(new PlayerMoveState());
            _stateMachine.AddState(new PlayerJumpState());
            _stateMachine.AddState(new PlayerDeadState());

            // 监听状态改变事件
            _stateMachine.OnStateChanged += OnStateChanged;

            // 设置初始状态
            _stateMachine.ChangeState<PlayerIdleState>();
        }

        private void Update()
        {
            // 更新状态机
            _stateMachine.Update();

            // 状态切换逻辑示例
            HandleStateTransitions();
        }

        /// <summary>
        /// 处理状态转换
        /// </summary>
        private void HandleStateTransitions()
        {
            // 如果生命值为0，切换到死亡状态
            if (_context.Health <= 0 && _stateMachine.CurrentStateType != typeof(PlayerDeadState))
            {
                _stateMachine.ChangeState<PlayerDeadState>();
                return;
            }

            // 如果在死亡状态，不处理其他转换
            if (_stateMachine.CurrentStateType == typeof(PlayerDeadState))
            {
                return;
            }

            // 跳跃输入
            if (Input.GetKeyDown(KeyCode.Space) && _context.IsGrounded)
            {
                _stateMachine.ChangeState<PlayerJumpState>();
                return;
            }

            // 移动输入
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
            {
                if (_stateMachine.CurrentStateType != typeof(PlayerMoveState) && 
                    _stateMachine.CurrentStateType != typeof(PlayerJumpState))
                {
                    _stateMachine.ChangeState<PlayerMoveState>();
                }
            }
            else
            {
                if (_stateMachine.CurrentStateType == typeof(PlayerMoveState))
                {
                    _stateMachine.ChangeState<PlayerIdleState>();
                }
            }
        }

        /// <summary>
        /// 状态改变回调
        /// </summary>
        /// <param name="previousState">前一个状态</param>
        /// <param name="newState">新状态</param>
        private void OnStateChanged(IState<PlayerContext> previousState, IState<PlayerContext> newState)
        {
            Debug.Log($"[PlayerStateMachine] 状态改变: {previousState?.GetType().Name} -> {newState?.GetType().Name}");
        }

        private void OnDestroy()
        {
            // 清理状态机
            _stateMachine?.Stop();
            _stateMachine = null;
        }

        /// <summary>
        /// 测试方法：减少生命值
        /// </summary>
        public void TakeDamage(float damage)
        {
            _context.Health -= damage;
            Debug.Log($"[PlayerStateMachine] 受到伤害: {damage}, 当前生命值: {_context.Health}");
        }
    }
}
