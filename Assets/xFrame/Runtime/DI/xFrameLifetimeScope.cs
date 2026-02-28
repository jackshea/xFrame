using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using xFrame.Runtime.Logging;
using xFrame.Runtime.ResourceManager;
using xFrame.Runtime.Serialization;
using xFrame.Runtime.Scheduler;
using xFrame.Runtime.StateMachine;
using xFrame.Runtime.Networking;
using xFrame.Runtime.Platform;
using xFrame.Runtime.Utilities;
using xFrame.Runtime.MVVM;

namespace xFrame.Runtime.DI
{
    /// <summary>
    /// xFrame框架的生命周期容器
    /// 负责注册框架的核心服务和模块
    /// </summary>
    public class xFrameLifetimeScope : LifetimeScope
    {
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
            RegisterLoggingSystem(builder);
            RegisterResourceSystem(builder);
            RegisterStateMachineModule(builder);
            RegisterSerializationModule(builder);
            RegisterSchedulerModule(builder);
            RegisterNetworkingModule(builder);
            RegisterPlatformModule(builder);
            RegisterUtilitiesModule(builder);
            RegisterMVVMModule(builder);
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
        /// 注册状态机模块到VContainer
        /// </summary>
        /// <param name="builder">容器构建器</param>
        private void RegisterStateMachineModule(IContainerBuilder builder)
        {
            // 使用扩展方法注册状态机模块
            builder.RegisterStateMachineModule();
        }

        /// <summary>
        /// 注册序列化模块到VContainer
        /// </summary>
        /// <param name="builder">容器构建器</param>
        private void RegisterSerializationModule(IContainerBuilder builder)
        {
            // 使用扩展方法注册序列化模块
            builder.RegisterSerializationModule();
        }

        /// <summary>
        /// 注册调度器模块到VContainer
        /// </summary>
        /// <param name="builder">容器构建器</param>
        private void RegisterSchedulerModule(IContainerBuilder builder)
        {
            // 使用扩展方法注册调度器模块
            builder.RegisterSchedulerModule();

            // 注册调度器模块为单例，并标记为可初始化
            builder.Register<SchedulerModule>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
        }

        /// <summary>
        /// 注册网络模块到VContainer
        /// </summary>
        /// <param name="builder">容器构建器</param>
        private void RegisterNetworkingModule(IContainerBuilder builder)
        {
            builder.RegisterNetworkingModule();
        }

        /// <summary>
        /// 注册平台模块到VContainer
        /// </summary>
        /// <param name="builder">容器构建器</param>
        private void RegisterPlatformModule(IContainerBuilder builder)
        {
            builder.RegisterPlatformModule();
        }

        /// <summary>
        /// 注册工具模块到VContainer
        /// </summary>
        /// <param name="builder">容器构建器</param>
        private void RegisterUtilitiesModule(IContainerBuilder builder)
        {
            builder.RegisterUtilitiesModule();
        }

        /// <summary>
        /// 注册 MVVM 模块到VContainer
        /// </summary>
        /// <param name="builder">容器构建器</param>
        private void RegisterMVVMModule(IContainerBuilder builder)
        {
            builder.RegisterMVVMModule();
        }
    }
}
