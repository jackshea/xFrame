using System;
using System.Threading.Tasks;
using UnityEngine;

namespace xFrame.Core.ResourceManager
{
    /// <summary>
    /// 资源管理器接口
    /// 提供统一的资源加载和释放接口，隐藏底层实现细节
    /// </summary>
    public interface IAssetManager
    {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="address">资源地址</param>
        /// <returns>加载的资源对象，失败时返回null</returns>
        T LoadAsset<T>(string address) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="address">资源地址</param>
        /// <returns>异步任务，包含加载的资源对象</returns>
        Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object;

        /// <summary>
        /// 同步加载资源（非泛型版本）
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <param name="type">资源类型</param>
        /// <returns>加载的资源对象，失败时返回null</returns>
        UnityEngine.Object LoadAsset(string address, Type type);

        /// <summary>
        /// 异步加载资源（非泛型版本）
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <param name="type">资源类型</param>
        /// <returns>异步任务，包含加载的资源对象</returns>
        Task<UnityEngine.Object> LoadAssetAsync(string address, Type type);

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="asset">要释放的资源对象</param>
        void ReleaseAsset(UnityEngine.Object asset);

        /// <summary>
        /// 释放指定地址的资源
        /// </summary>
        /// <param name="address">资源地址</param>
        void ReleaseAsset(string address);

        /// <summary>
        /// 预加载资源到缓存
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <returns>预加载任务</returns>
        Task PreloadAssetAsync(string address);

        /// <summary>
        /// 检查资源是否已缓存
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <returns>如果资源已缓存返回true，否则返回false</returns>
        bool IsAssetCached(string address);

        /// <summary>
        /// 清理所有缓存的资源
        /// </summary>
        void ClearCache();

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存统计信息</returns>
        AssetCacheStats GetCacheStats();
    }

    /// <summary>
    /// 资源缓存统计信息
    /// </summary>
    public struct AssetCacheStats
    {
        /// <summary>
        /// 缓存中的资源数量
        /// </summary>
        public int CachedAssetCount;

        /// <summary>
        /// 缓存命中次数
        /// </summary>
        public int CacheHitCount;

        /// <summary>
        /// 缓存未命中次数
        /// </summary>
        public int CacheMissCount;

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public float CacheHitRate => CacheHitCount + CacheMissCount > 0 
            ? (float)CacheHitCount / (CacheHitCount + CacheMissCount) 
            : 0f;
    }
}
