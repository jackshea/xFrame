using VContainer;

namespace xFrame.Runtime.Scheduler
{
    /// <summary>
    ///     调度器模块的VContainer注册扩展方法
    /// </summary>
    public static class SchedulerServiceExtensions
    {
        /// <summary>
        ///     注册调度器模块到VContainer容器
        /// </summary>
        /// <param name="builder">容器构建器</param>
        public static void RegisterSchedulerModule(this IContainerBuilder builder)
        {
            // AsImplementedInterfaces 已覆盖 ISchedulerService，这里避免重复注册同一契约。
            builder.Register<SchedulerService>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
        }
    }
}
