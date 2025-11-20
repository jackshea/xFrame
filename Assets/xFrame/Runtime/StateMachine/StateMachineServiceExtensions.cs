using VContainer;

namespace xFrame.Runtime.StateMachine
{
    /// <summary>
    /// 状态机模块的VContainer注册扩展方法
    /// </summary>
    public static class StateMachineServiceExtensions
    {
        /// <summary>
        /// 注册状态机模块到VContainer容器
        /// </summary>
        /// <param name="builder">容器构建器</param>
        public static void RegisterStateMachineModule(this IContainerBuilder builder)
        {
            // 注册服务实现为单例，同时注册接口和实现类
            builder.Register<StateMachineServiceService>(Lifetime.Singleton)
                .As<IStateMachineService>()
                .AsImplementedInterfaces() // 自动注册ITickable和IDisposable
                .AsSelf();
        }
    }
}
