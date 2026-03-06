using System;
using System.Collections.Generic;
using xFrame.Runtime.EventBus;

namespace xFrame.Runtime.StateMachine
{
    /// <summary>
    ///     通用有限状态机，不带上下文
    /// </summary>
    public class StateMachine
    {
        private readonly Dictionary<Type, IState> _states = new();

        /// <summary>
        ///     当前状态
        /// </summary>
        public IState CurrentState { get; private set; }

        /// <summary>
        ///     当前状态类型
        /// </summary>
        public Type CurrentStateType => CurrentState?.GetType();

        /// <summary>
        ///     是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        ///     状态机名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     添加状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        /// <param name="state">状态实例</param>
        public void AddState<TState>(TState state) where TState : IState
        {
            var stateType = typeof(TState);
            if (_states.ContainsKey(stateType))
                throw new InvalidOperationException($"State {stateType.Name} already exists.");
            _states[stateType] = state;
        }

        /// <summary>
        ///     移除状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        public void RemoveState<TState>() where TState : IState
        {
            var stateType = typeof(TState);
            if (CurrentState?.GetType() == stateType)
                throw new InvalidOperationException($"Cannot remove current state {stateType.Name}.");
            _states.Remove(stateType);
        }

        /// <summary>
        ///     获取状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        /// <returns>状态实例</returns>
        public TState GetState<TState>() where TState : IState
        {
            var stateType = typeof(TState);
            if (_states.TryGetValue(stateType, out var state)) return (TState)state;
            return default;
        }

        /// <summary>
        ///     切换到指定状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        public void ChangeState<TState>() where TState : IState
        {
            var stateType = typeof(TState);
            if (!_states.TryGetValue(stateType, out var newState))
                throw new InvalidOperationException($"State {stateType.Name} not found.");

            var previousState = CurrentState;

            // 退出当前状态
            CurrentState?.OnExit();

            // 切换状态
            CurrentState = newState;

            // 进入新状态
            CurrentState.OnEnter();

            // 通过事件总线触发状态改变事件
            xFrameEventBus.Raise(new StateChangedEvent(Name, previousState, CurrentState));

            IsRunning = true;
        }

        /// <summary>
        ///     更新当前状态
        /// </summary>
        public void Update()
        {
            if (IsRunning && CurrentState != null) CurrentState.OnUpdate();
        }

        /// <summary>
        ///     停止状态机
        /// </summary>
        public void Stop()
        {
            if (CurrentState != null)
            {
                CurrentState.OnExit();
                CurrentState = null;
            }

            IsRunning = false;
        }

        /// <summary>
        ///     清空所有状态
        /// </summary>
        public void Clear()
        {
            Stop();
            _states.Clear();
        }
    }

    /// <summary>
    ///     通用有限状态机，带上下文
    /// </summary>
    /// <typeparam name="TContext">状态机上下文类型</typeparam>
    public class StateMachine<TContext>
    {
        private readonly Dictionary<Type, IState<TContext>> _states = new();

        /// <summary>
        ///     构造函数
        /// </summary>
        /// <param name="context">状态机上下文</param>
        public StateMachine(TContext context)
        {
            Context = context;
        }

        /// <summary>
        ///     当前状态
        /// </summary>
        public IState<TContext> CurrentState { get; private set; }

        /// <summary>
        ///     当前状态类型
        /// </summary>
        public Type CurrentStateType => CurrentState?.GetType();

        /// <summary>
        ///     状态机上下文
        /// </summary>
        public TContext Context { get; set; }

        /// <summary>
        ///     是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        ///     状态机名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     添加状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        /// <param name="state">状态实例</param>
        public void AddState<TState>(TState state) where TState : IState<TContext>
        {
            var stateType = typeof(TState);
            if (_states.ContainsKey(stateType))
                throw new InvalidOperationException($"State {stateType.Name} already exists.");
            _states[stateType] = state;
        }

        /// <summary>
        ///     移除状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        public void RemoveState<TState>() where TState : IState<TContext>
        {
            var stateType = typeof(TState);
            if (CurrentState?.GetType() == stateType)
                throw new InvalidOperationException($"Cannot remove current state {stateType.Name}.");
            _states.Remove(stateType);
        }

        /// <summary>
        ///     获取状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        /// <returns>状态实例</returns>
        public TState GetState<TState>() where TState : IState<TContext>
        {
            var stateType = typeof(TState);
            if (_states.TryGetValue(stateType, out var state)) return (TState)state;
            return default;
        }

        /// <summary>
        ///     切换到指定状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        public void ChangeState<TState>() where TState : IState<TContext>
        {
            var stateType = typeof(TState);
            if (!_states.TryGetValue(stateType, out var newState))
                throw new InvalidOperationException($"State {stateType.Name} not found.");

            var previousState = CurrentState;

            // 退出当前状态
            CurrentState?.OnExit(Context);

            // 切换状态
            CurrentState = newState;

            // 进入新状态
            CurrentState.OnEnter(Context);

            // 通过事件总线触发状态改变事件
            xFrameEventBus.Raise(new StateChangedEvent<TContext>(Name, previousState, CurrentState, Context));

            IsRunning = true;
        }

        /// <summary>
        ///     更新当前状态
        /// </summary>
        public void Update()
        {
            if (IsRunning && CurrentState != null) CurrentState.OnUpdate(Context);
        }

        /// <summary>
        ///     停止状态机
        /// </summary>
        public void Stop()
        {
            if (CurrentState != null)
            {
                CurrentState.OnExit(Context);
                CurrentState = null;
            }

            IsRunning = false;
        }

        /// <summary>
        ///     清空所有状态
        /// </summary>
        public void Clear()
        {
            Stop();
            _states.Clear();
        }
    }
}