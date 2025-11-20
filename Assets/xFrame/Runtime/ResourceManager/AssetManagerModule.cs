using System;
using xFrame.Runtime.Logging;

namespace xFrame.Runtime.ResourceManager
{
    /// <summary>
    /// 资源管理模块
    /// 负责初始化和配置资源管理系统，集成到VContainer依赖注入框架
    /// </summary>
    public class AssetManagerModule : IDisposable
    {
        private readonly IAssetManager _assetManager;
        private readonly IXLogger _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="assetManager">资源管理器</param>
        /// <param name="logManager">日志管理器</param>
        public AssetManagerModule(IAssetManager assetManager, IXLogManager logManager)
        {
            _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
            _logger = logManager?.GetLogger<AssetManagerModule>() ??
                            throw new ArgumentNullException(nameof(logManager));
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            OnDestroy();
        }

        /// <summary>
        /// 初始化资源管理模块
        /// </summary>
        public void OnInit()
        {
            _logger.Info("资源管理模块初始化开始...");

            try
            {
                // 验证资源管理器是否正常工作
                ValidateAssetManager();

                _logger.Info("资源管理模块初始化完成");
            }
            catch (Exception ex)
            {
                _logger.Error("资源管理模块初始化失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 模块启动
        /// 在所有模块初始化完成后调用
        /// </summary>
        public void OnStart()
        {
            _logger.Info("资源管理模块启动");

            try
            {
                // 输出缓存统计信息
                var stats = _assetManager.GetCacheStats();
                _logger.Info($"资源缓存统计 - 缓存资源数: {stats.CachedAssetCount}, 命中率: {stats.CacheHitRate:P2}");
            }
            catch (Exception ex)
            {
                _logger.Error("资源管理模块启动时获取统计信息失败", ex);
            }
        }

        /// <summary>
        /// 模块销毁
        /// 清理资源和缓存
        /// </summary>
        public void OnDestroy()
        {
            _logger.Info("资源管理模块销毁开始...");

            try
            {
                // 清理所有缓存的资源
                _assetManager.ClearCache();

                // 如果资源管理器实现了IDisposable，则释放它
                if (_assetManager is IDisposable disposableManager) disposableManager.Dispose();

                _logger.Info("资源管理模块销毁完成");
            }
            catch (Exception ex)
            {
                _logger.Error("资源管理模块销毁失败", ex);
            }
        }

        /// <summary>
        /// 验证资源管理器是否正常工作
        /// </summary>
        private void ValidateAssetManager()
        {
            if (_assetManager == null) throw new InvalidOperationException("资源管理器不能为空");

            // 检查缓存统计功能是否正常
            var stats = _assetManager.GetCacheStats();
            _logger.Debug($"资源管理器验证通过，初始缓存统计: {stats.CachedAssetCount} 个资源");
        }
    }
}