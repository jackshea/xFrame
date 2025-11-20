namespace xFrame.Runtime.StateMachine
{
    /// <summary>
    /// 状态基类，提供默认的空实现
    /// </summary>
    public abstract class StateBase : IState
    {
        /// <summary>
        /// 进入状态时调用
        /// </summary>
        public virtual void OnEnter() { }

        /// <summary>
        /// 状态更新时调用
        /// </summary>
        public virtual void OnUpdate() { }

        /// <summary>
        /// 退出状态时调用
        /// </summary>
        public virtual void OnExit() { }
    }

    /// <summary>
    /// 泛型状态基类，提供默认的空实现
    /// </summary>
    /// <typeparam name="TContext">状态机上下文类型</typeparam>
    public abstract class StateBase<TContext> : IState<TContext>
    {
        /// <summary>
        /// 进入状态时调用
        /// </summary>
        /// <param name="context">状态机上下文</param>
        public virtual void OnEnter(TContext context) { }

        /// <summary>
        /// 状态更新时调用
        /// </summary>
        /// <param name="context">状态机上下文</param>
        public virtual void OnUpdate(TContext context) { }

        /// <summary>
        /// 退出状态时调用
        /// </summary>
        /// <param name="context">状态机上下文</param>
        public virtual void OnExit(TContext context) { }
    }
}
