namespace xFrame.Runtime.StateMachine
{
    /// <summary>
    /// 状态机模块接口，供外部调用
    /// </summary>
    public interface IStateMachineService
    {
        /// <summary>
        /// 创建状态机（不带上下文）
        /// </summary>
        /// <param name="name">状态机名称</param>
        /// <param name="autoUpdate">是否自动更新</param>
        /// <returns>状态机实例</returns>
        StateMachine CreateStateMachine(string name, bool autoUpdate = true);

        /// <summary>
        /// 创建状态机（带上下文）
        /// </summary>
        /// <typeparam name="TContext">上下文类型</typeparam>
        /// <param name="name">状态机名称</param>
        /// <param name="context">上下文实例</param>
        /// <param name="autoUpdate">是否自动更新</param>
        /// <returns>状态机实例</returns>
        StateMachine<TContext> CreateStateMachine<TContext>(string name, TContext context, bool autoUpdate = true);

        /// <summary>
        /// 获取状态机（不带上下文）
        /// </summary>
        /// <param name="name">状态机名称</param>
        /// <returns>状态机实例</returns>
        StateMachine GetStateMachine(string name);

        /// <summary>
        /// 获取状态机（带上下文）
        /// </summary>
        /// <typeparam name="TContext">上下文类型</typeparam>
        /// <param name="name">状态机名称</param>
        /// <returns>状态机实例</returns>
        StateMachine<TContext> GetStateMachine<TContext>(string name);

        /// <summary>
        /// 移除状态机
        /// </summary>
        /// <param name="name">状态机名称</param>
        void RemoveStateMachine(string name);
    }
}
