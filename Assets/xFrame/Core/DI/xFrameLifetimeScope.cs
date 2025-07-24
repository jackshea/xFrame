using UnityEngine;
using VContainer;
using VContainer.Unity;
using xFrame.Core;
using xFrame.Core.Logging;
using ILogger = xFrame.Core.Logging.ILogger;

namespace xFrame.Core.DI
{
    /// <summary>
    /// xFrame框架的生命周期容器
    /// 负责注册框架的核心服务和模块
    /// </summary>
    public class xFrameLifetimeScope : LifetimeScope
    {
        /// <summary>
        /// 模块更新器引用
        /// </summary>
        [SerializeField]
        private ModuleUpdater moduleUpdater;
        
        /// <summary>
        /// 配置容器
        /// </summary>
        /// <param name="builder">容器构建器</param>
        protected override void Configure(IContainerBuilder builder)
        {
            // 注册核心系统
            RegisterLoggingSystem(builder);
            RegisterModuleSystem(builder);
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
        
        /// <summary>
        /// 注册模块系统到VContainer
        /// </summary>
        /// <param name="builder">容器构建器</param>
        private void RegisterModuleSystem(IContainerBuilder builder)
        {
            // 注册模块管理器为单例，并实现IStartable接口
            builder.Register<ModuleManager>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
                
            // 注册模块更新器
            if (moduleUpdater != null)
            {
                builder.RegisterComponent(moduleUpdater);
            }
            
            // 构建完成后初始化模块更新器
            builder.RegisterBuildCallback(container => 
            {
                if (moduleUpdater != null)
                {
                    moduleUpdater.Initialize(container);
                }
            });
        }
    }
}