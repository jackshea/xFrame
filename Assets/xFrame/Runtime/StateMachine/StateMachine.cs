using System;
using System.Collections.Generic;

namespace xFrame.StateMachine
{
    /// <summary>
    /// 通用有限状态机，不带上下文
    /// </summary>
    public class StateMachine
    {
        private IState _currentState;
        private readonly Dictionary<Type, IState> _states = new Dictionary<Type, IState>();

        /// <summary>
        /// 当前状态
        /// </summary>
        public IState CurrentState => _currentState;

        /// <summary>
        /// 当前状态类型
        /// </summary>
        public Type CurrentStateType => _currentState?.GetType();

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 状态改变事件
        /// </summary>
        public event Action<IState, IState> OnStateChanged;

        /// <summary>
        /// 添加状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        /// <param name="state">状态实例</param>
        public void AddState<TState>(TState state) where TState : IState
        {
            var stateType = typeof(TState);
            if (_states.ContainsKey(stateType))
            {
                throw new InvalidOperationException($"State {stateType.Name} already exists.");
            }
            _states[stateType] = state;
        }

        /// <summary>
        /// 移除状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        public void RemoveState<TState>() where TState : IState
        {
            var stateType = typeof(TState);
            if (_currentState?.GetType() == stateType)
            {
                throw new InvalidOperationException($"Cannot remove current state {stateType.Name}.");
            }
            _states.Remove(stateType);
        }

        /// <summary>
        /// 获取状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        /// <returns>状态实例</returns>
        public TState GetState<TState>() where TState : IState
        {
            var stateType = typeof(TState);
            if (_states.TryGetValue(stateType, out var state))
            {
                return (TState)state;
            }
            return default;
        }

        /// <summary>
        /// 切换到指定状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        public void ChangeState<TState>() where TState : IState
        {
            var stateType = typeof(TState);
            if (!_states.TryGetValue(stateType, out var newState))
            {
                throw new InvalidOperationException($"State {stateType.Name} not found.");
            }

            var previousState = _currentState;
            
            // 退出当前状态
            _currentState?.OnExit();

            // 切换状态
            _currentState = newState;

            // 进入新状态
            _currentState.OnEnter();

            // 触发状态改变事件
            OnStateChanged?.Invoke(previousState, _currentState);

            IsRunning = true;
        }

        /// <summary>
        /// 更新当前状态
        /// </summary>
        public void Update()
        {
            if (IsRunning && _currentState != null)
            {
                _currentState.OnUpdate();
            }
        }

        /// <summary>
        /// 停止状态机
        /// </summary>
        public void Stop()
        {
            if (_currentState != null)
            {
                _currentState.OnExit();
                _currentState = null;
            }
            IsRunning = false;
        }

        /// <summary>
        /// 清空所有状态
        /// </summary>
        public void Clear()
        {
            Stop();
            _states.Clear();
        }
    }

    /// <summary>
    /// 通用有限状态机，带上下文
    /// </summary>
    /// <typeparam name="TContext">状态机上下文类型</typeparam>
    public class StateMachine<TContext>
    {
        private IState<TContext> _currentState;
        private readonly Dictionary<Type, IState<TContext>> _states = new Dictionary<Type, IState<TContext>>();
        private TContext _context;

        /// <summary>
        /// 当前状态
        /// </summary>
        public IState<TContext> CurrentState => _currentState;

        /// <summary>
        /// 当前状态类型
        /// </summary>
        public Type CurrentStateType => _currentState?.GetType();

        /// <summary>
        /// 状态机上下文
        /// </summary>
        public TContext Context
        {
            get => _context;
            set => _context = value;
        }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 状态改变事件
        /// </summary>
        public event Action<IState<TContext>, IState<TContext>> OnStateChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">状态机上下文</param>
        public StateMachine(TContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 添加状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        /// <param name="state">状态实例</param>
        public void AddState<TState>(TState state) where TState : IState<TContext>
        {
            var stateType = typeof(TState);
            if (_states.ContainsKey(stateType))
            {
                throw new InvalidOperationException($"State {stateType.Name} already exists.");
            }
            _states[stateType] = state;
        }

        /// <summary>
        /// 移除状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        public void RemoveState<TState>() where TState : IState<TContext>
        {
            var stateType = typeof(TState);
            if (_currentState?.GetType() == stateType)
            {
                throw new InvalidOperationException($"Cannot remove current state {stateType.Name}.");
            }
            _states.Remove(stateType);
        }

        /// <summary>
        /// 获取状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        /// <returns>状态实例</returns>
        public TState GetState<TState>() where TState : IState<TContext>
        {
            var stateType = typeof(TState);
            if (_states.TryGetValue(stateType, out var state))
            {
                return (TState)state;
            }
            return default;
        }

        /// <summary>
        /// 切换到指定状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        public void ChangeState<TState>() where TState : IState<TContext>
        {
            var stateType = typeof(TState);
            if (!_states.TryGetValue(stateType, out var newState))
            {
                throw new InvalidOperationException($"State {stateType.Name} not found.");
            }

            var previousState = _currentState;
            
            // 退出当前状态
            _currentState?.OnExit(_context);

            // 切换状态
            _currentState = newState;

            // 进入新状态
            _currentState.OnEnter(_context);

            // 触发状态改变事件
            OnStateChanged?.Invoke(previousState, _currentState);

            IsRunning = true;
        }

        /// <summary>
        /// 更新当前状态
        /// </summary>
        public void Update()
        {
            if (IsRunning && _currentState != null)
            {
                _currentState.OnUpdate(_context);
            }
        }

        /// <summary>
        /// 停止状态机
        /// </summary>
        public void Stop()
        {
            if (_currentState != null)
            {
                _currentState.OnExit(_context);
                _currentState = null;
            }
            IsRunning = false;
        }

        /// <summary>
        /// 清空所有状态
        /// </summary>
        public void Clear()
        {
            Stop();
            _states.Clear();
        }
    }
}
