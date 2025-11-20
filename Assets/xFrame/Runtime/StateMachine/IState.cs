namespace xFrame.StateMachine
{
    /// <summary>
    /// 状态接口，定义状态的基本行为
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// 进入状态时调用
        /// </summary>
        void OnEnter();

        /// <summary>
        /// 状态更新时调用
        /// </summary>
        void OnUpdate();

        /// <summary>
        /// 退出状态时调用
        /// </summary>
        void OnExit();
    }

    /// <summary>
    /// 泛型状态接口，支持上下文
    /// </summary>
    /// <typeparam name="TContext">状态机上下文类型</typeparam>
    public interface IState<TContext>
    {
        /// <summary>
        /// 进入状态时调用
        /// </summary>
        /// <param name="context">状态机上下文</param>
        void OnEnter(TContext context);

        /// <summary>
        /// 状态更新时调用
        /// </summary>
        /// <param name="context">状态机上下文</param>
        void OnUpdate(TContext context);

        /// <summary>
        /// 退出状态时调用
        /// </summary>
        /// <param name="context">状态机上下文</param>
        void OnExit(TContext context);
    }
}
