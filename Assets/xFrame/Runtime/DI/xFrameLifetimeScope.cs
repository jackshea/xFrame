using UnityEngine;
using VContainer;
using VContainer.Unity;
using xFrame.Core;
using xFrame.Core.Logging;
using xFrame.Core.ResourceManager;
using xFrame.Core.MessagePipe;
using MessagePipe;
using MessagePipe.VContainer;

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
        /// 资源管理器缓存容量
        /// </summary>
        [SerializeField]
        private int assetManagerCacheCapacity = 100;
        
        
        
        /// <summary>
        /// 配置容器
        /// </summary>
        /// <param name="builder">容器构建器</param>
        protected override void Configure(IContainerBuilder builder)
        {
            // 注册核心系统
            RegisterModuleSystem(builder);
            RegisterLoggingSystem(builder);
            RegisterResourceSystem(builder);
            RegisterMessagePipeSystem(builder);
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
                xFrame.Core.ModuleRegistry.SetupInContainer(container);
                if (moduleUpdater != null)
                {
                    moduleUpdater.Initialize(container);
                }
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
                return (moduleName) => logManager.GetLogger(moduleName);
            }, Lifetime.Singleton);

            // 注册泛型日志工厂方法
            builder.RegisterFactory<System.Type, IXLogger>(container =>
            {
                var logManager = container.Resolve<IXLogManager>();
                return (type) => logManager.GetLogger(type);
            }, Lifetime.Singleton);
        }

        /// <summary>
        /// 注册资源管理系统到VContainer
        /// </summary>
        /// <param name="builder">容器构建器</param>
        private void RegisterResourceSystem(IContainerBuilder builder)
        {
            // 注册缓存容量参数
            builder.RegisterInstance(assetManagerCacheCapacity);
            
            // 注册资源管理器实现为单例
            builder.Register<IAssetManager, AddressableAssetManager>(Lifetime.Singleton);

            // 注册资源管理模块为单例，并标记为可初始化
            builder.Register<AssetManagerModule>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
        }

        /// <summary>
        /// 注册MessagePipe事件系统到VContainer
        /// </summary>
        /// <param name="builder">容器构建器</param>
        private void RegisterMessagePipeSystem(IContainerBuilder builder)
        {
            // 配置MessagePipe选项
            var options = builder.RegisterMessagePipe(options =>
            {
                // 启用堆栈跟踪捕获以帮助调试订阅泄漏
                options.EnableCaptureStackTrace = true;
                
                // 设置实例生命周期为单例
                options.InstanceLifetime = global::MessagePipe.InstanceLifetime.Singleton;
                
                // 配置异步发布策略
                options.DefaultAsyncPublishStrategy = global::MessagePipe.AsyncPublishStrategy.Parallel;
            });

            // 注册MessagePipe模块为单例，并标记为可初始化
            builder.Register<xFrame.Core.MessagePipe.MessagePipeModule>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
        }
    }
}