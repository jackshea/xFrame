using System;
using System.Collections.Generic;
using VContainer;
using xFrame.Core.Logging;
using xFrame.Core.ResourceManager;

namespace xFrame.Core
{
    /// <summary>
    /// 模块注册管理器
    /// 负责集中注册和管理所有框架模块
    /// </summary>
    public static class ModuleRegistry
    {
        /// <summary>
        /// 注册所有核心模块
        /// </summary>
        /// <param name="moduleManager">模块管理器实例</param>
        public static void RegisterCoreModules(ModuleManager moduleManager)
        {
            if (moduleManager == null)
                throw new ArgumentNullException(nameof(moduleManager));
                
            moduleManager.RegisterModule<XLoggingModule>();
            moduleManager.RegisterModule<AssetManagerModule>();
        }
        
        /// <summary>
        /// 注册所有功能模块
        /// </summary>
        /// <param name="moduleManager">模块管理器实例</param>
        public static void RegisterFeatureModules(ModuleManager moduleManager)
        {
            if (moduleManager == null)
                throw new ArgumentNullException(nameof(moduleManager));
                
            // 显式注册功能模块
        }
        
        /// <summary>
        /// 注册所有模块（核心和功能）
        /// </summary>
        /// <param name="moduleManager">模块管理器实例</param>
        public static void RegisterAllModules(ModuleManager moduleManager)
        {
            RegisterCoreModules(moduleManager);
            RegisterFeatureModules(moduleManager);
        }
        
        /// <summary>
        /// 自动注册模块到容器
        /// 在xFrameLifetimeScope中使用
        /// </summary>
        /// <param name="container">VContainer容器</param>
        public static void SetupInContainer(IObjectResolver container)
        {
            try
            {
                var moduleManager = container.Resolve<ModuleManager>();
                if (moduleManager != null)
                {
                    RegisterAllModules(moduleManager);
                }
                else
                {
                    UnityEngine.Debug.LogError("ModuleRegistry: ModuleManager未在容器中注册");
                }
            }
            catch (Exception ex)
            {
                // 如果日志系统已初始化，记录错误
                try
                {
                    var logger = XLog.GetLogger("ModuleRegistry");
                    logger.Error("注册模块失败", ex);
                }
                catch
                {
                    // 日志系统可能未初始化，使用Unity日志
                    UnityEngine.Debug.LogError($"注册模块失败，错误: {ex.Message}");
                }
            }
        }
    }
}
