using System;
using System.Threading.Tasks;
using UnityEngine;

namespace xFrame.Core.ResourceManager
{
    /// <summary>
    /// 资源提供者接口
    /// 定义底层资源加载实现的统一接口，支持多种资源管理方案
    /// </summary>
    public interface IAssetProvider : IDisposable
    {
        /// <summary>
        /// 提供者名称
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// 是否支持异步加载
        /// </summary>
        bool SupportsAsync { get; }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="address">资源地址</param>
        /// <returns>加载的资源对象</returns>
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
        /// <returns>加载的资源对象</returns>
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
        /// <param name="address">资源地址</param>
        /// <param name="asset">资源对象</param>
        void ReleaseAsset(string address, UnityEngine.Object asset);

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <returns>如果资源存在返回true，否则返回false</returns>
        bool AssetExists(string address);

        /// <summary>
        /// 预加载资源
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <returns>预加载任务</returns>
        Task PreloadAsync(string address);

        /// <summary>
        /// 获取提供者统计信息
        /// </summary>
        /// <returns>提供者统计信息</returns>
        AssetProviderStats GetStats();
    }

    /// <summary>
    /// 资源提供者统计信息
    /// </summary>
    public struct AssetProviderStats
    {
        /// <summary>
        /// 已加载的资源数量
        /// </summary>
        public int LoadedAssetCount;

        /// <summary>
        /// 加载成功次数
        /// </summary>
        public int LoadSuccessCount;

        /// <summary>
        /// 加载失败次数
        /// </summary>
        public int LoadFailureCount;

        /// <summary>
        /// 加载成功率
        /// </summary>
        public float LoadSuccessRate => LoadSuccessCount + LoadFailureCount > 0 
            ? (float)LoadSuccessCount / (LoadSuccessCount + LoadFailureCount) 
            : 0f;

        /// <summary>
        /// 释放次数
        /// </summary>
        public int ReleaseCount;
    }
}
