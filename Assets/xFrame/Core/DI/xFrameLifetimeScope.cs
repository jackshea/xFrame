using VContainer;
using VContainer.Unity;
using xFrame.Core.Logging;

public class xFrameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 注册日志系统
        RegisterLoggingSystem(builder);
    }

    /// <summary>
    /// 注册日志系统到VContainer
    /// </summary>
    /// <param name="builder">容器构建器</param>
    private void RegisterLoggingSystem(IContainerBuilder builder)
    {
        // 注册日志管理器为单例
        builder.Register<ILogManager, LogManager>(Lifetime.Singleton);

        // 注册日志模块为单例，并标记为可初始化
        builder.Register<LoggingModule>(Lifetime.Singleton)
            .AsImplementedInterfaces()
            .AsSelf();

        // 注册日志工厂方法，用于创建特定模块的Logger
        builder.RegisterFactory<string, ILogger>(container =>
        {
            var logManager = container.Resolve<ILogManager>();
            return (moduleName) => logManager.GetLogger(moduleName);
        }, Lifetime.Singleton);

        // 注册泛型日志工厂方法
        builder.RegisterFactory<System.Type, ILogger>(container =>
        {
            var logManager = container.Resolve<ILogManager>();
            return (type) => logManager.GetLogger(type);
        }, Lifetime.Singleton);
    }
}