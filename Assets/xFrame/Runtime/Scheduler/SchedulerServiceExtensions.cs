using VContainer;

namespace xFrame.Runtime.Scheduler
{
    /// <summary>
    /// 调度器模块的VContainer注册扩展方法
    /// </summary>
    public static class SchedulerServiceExtensions
    {
        /// <summary>
        /// 注册调度器模块到VContainer容器
        /// </summary>
        /// <param name="builder">容器构建器</param>
        public static void RegisterSchedulerModule(this IContainerBuilder builder)
        {
            // 注册服务实现为单例，同时注册接口和实现类
            builder.Register<SchedulerService>(Lifetime.Singleton)
                .As<ISchedulerService>()
                .AsImplementedInterfaces() // 自动注册ITickable和IDisposable
                .AsSelf();
        }
    }
}
