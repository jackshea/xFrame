using xFrame.Runtime.EventBus;

namespace xFrame.Runtime.StateMachine
{
    /// <summary>
    /// 状态改变事件（不带上下文）
    /// </summary>
    public struct StateChangedEvent : IEvent
    {
        /// <summary>
        /// 状态机名称
        /// </summary>
        public string StateMachineName { get; set; }
        
        /// <summary>
        /// 前一个状态
        /// </summary>
        public IState PreviousState { get; set; }
        
        /// <summary>
        /// 当前状态
        /// </summary>
        public IState CurrentState { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="stateMachineName">状态机名称</param>
        /// <param name="previousState">前一个状态</param>
        /// <param name="currentState">当前状态</param>
        public StateChangedEvent(string stateMachineName, IState previousState, IState currentState)
        {
            StateMachineName = stateMachineName;
            PreviousState = previousState;
            CurrentState = currentState;
        }
    }

    /// <summary>
    /// 状态改变事件（带上下文）
    /// </summary>
    /// <typeparam name="TContext">上下文类型</typeparam>
    public struct StateChangedEvent<TContext> : IEvent
    {
        /// <summary>
        /// 状态机名称
        /// </summary>
        public string StateMachineName { get; set; }
        
        /// <summary>
        /// 前一个状态
        /// </summary>
        public IState<TContext> PreviousState { get; set; }
        
        /// <summary>
        /// 当前状态
        /// </summary>
        public IState<TContext> CurrentState { get; set; }
        
        /// <summary>
        /// 上下文
        /// </summary>
        public TContext Context { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="stateMachineName">状态机名称</param>
        /// <param name="previousState">前一个状态</param>
        /// <param name="currentState">当前状态</param>
        /// <param name="context">上下文</param>
        public StateChangedEvent(string stateMachineName, IState<TContext> previousState, IState<TContext> currentState, TContext context)
        {
            StateMachineName = stateMachineName;
            PreviousState = previousState;
            CurrentState = currentState;
            Context = context;
        }
    }
}
