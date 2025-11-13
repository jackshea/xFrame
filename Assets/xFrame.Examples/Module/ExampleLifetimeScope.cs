using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using xFrame.Runtime;
using xFrame.Runtime.Logging;
using xFrame.Examples;

public class ExampleLifetimeScope : LifetimeScope
{
    /// <summary>
    /// 模块更新器引用
    /// </summary>
    [SerializeField]
    private ModuleUpdater moduleUpdater;

    protected override void Configure(IContainerBuilder builder)
    {
        RegisterExampleModule(builder);
        RegisterLoggingSystem(builder);
    }

    private void RegisterExampleModule(IContainerBuilder builder)
    {
        // 注册模块管理器为单例，并实现IStartable接口
        builder.Register<ModuleManager>(Lifetime.Singleton)
            .AsImplementedInterfaces()
            .AsSelf();

        // 注册模块更新器
        if (moduleUpdater != null) builder.RegisterComponent(moduleUpdater);

        // 注册示例模块
        builder.Register<ExampleModule>(Lifetime.Singleton)
            .AsImplementedInterfaces()
            .AsSelf();

        // 构建完成后初始化模块更新器
        builder.RegisterBuildCallback(container =>
        {
            if (moduleUpdater != null) moduleUpdater.Initialize(container);
        });
    }

    /// <summary>
    /// 注册日志系统到VContainer
    /// </summary>
    /// <param name="builder">容器构建器</param>
    private void RegisterLoggingSystem(IContainerBuilder builder)
    {
        // 注册日志管理器为单例
        builder.Register<IXLogManager, XLogManager>(Lifetime.Singleton);

        // 注册日志模块为单例，并标记为可初始化
        builder.Register<XLoggingModule>(Lifetime.Singleton)
            .AsImplementedInterfaces()
            .AsSelf();

        // 注册日志工厂方法，用于创建特定模块的Logger
        builder.RegisterFactory<string, IXLogger>(container =>
        {
            var logManager = container.Resolve<IXLogManager>();
            return moduleName => logManager.GetLogger(moduleName);
        }, Lifetime.Singleton);

        // 注册泛型日志工厂方法
        builder.RegisterFactory<Type, IXLogger>(container =>
        {
            var logManager = container.Resolve<IXLogManager>();
            return type => logManager.GetLogger(type);
        }, Lifetime.Singleton);
    }
}