using VContainer;

namespace xFrame.Runtime.StateMachine
{
    /// <summary>
    ///     状态机模块的VContainer注册扩展方法
    /// </summary>
    public static class StateMachineServiceExtensions
    {
        /// <summary>
        ///     注册状态机模块到VContainer容器
        /// </summary>
        /// <param name="builder">容器构建器</param>
        public static void RegisterStateMachineModule(this IContainerBuilder builder)
        {
            // AsImplementedInterfaces 已覆盖 IStateMachineService，这里避免重复注册同一契约。
            builder.Register<StateMachineServiceService>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
        }
    }
}
