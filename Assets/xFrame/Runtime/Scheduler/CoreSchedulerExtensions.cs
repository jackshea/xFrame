using VContainer;
using xFrame.Runtime.Core;
using xFrame.Runtime.Core.Scheduler;

namespace xFrame.Runtime.Scheduler
{
    /// <summary>
    /// 核心调度器模块的VContainer注册扩展方法
    /// 支持将核心调度器注册到Unity的DI容器中
    /// </summary>
    public static class CoreSchedulerExtensions
    {
        /// <summary>
        /// 注册核心调度器模块到VContainer容器
        /// </summary>
        /// <param name="builder">容器构建器</param>
        public static void RegisterCoreSchedulerModule(this IContainerBuilder builder)
        {
            // 注册核心日志管理器
            builder.Register<CoreLogManager>(Lifetime.Singleton)
                .As<ICoreLogManager>();

            // 注册核心调度器
            builder.Register<CoreScheduler>(Lifetime.Singleton)
                .As<ICoreScheduler>();
        }
    }
}
