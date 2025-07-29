using System;
using System.Threading.Tasks;
using UnityEngine;

namespace xFrame.Core.ResourceManager
{
    /// <summary>
    /// 基于Unity Resources系统的资源提供者
    /// 用于演示如何实现不同的资源加载方案
    /// </summary>
    public class ResourcesAssetProvider : IAssetProvider
    {
        private readonly object _lockObject = new object();
        
        // 统计信息
        private int _loadedAssetCount;
        private int _loadSuccessCount;
        private int _loadFailureCount;
        private int _releaseCount;

        /// <summary>
        /// 提供者名称
        /// </summary>
        public string ProviderName => "Resources";

        /// <summary>
        /// 是否支持异步加载（Resources系统支持异步）
        /// </summary>
        public bool SupportsAsync => true;

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="address">资源地址（Resources路径）</param>
        /// <returns>加载的资源对象</returns>
        public T LoadAsset<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"[ResourcesAssetProvider] 资源地址不能为空");
                lock (_lockObject)
                {
                    _loadFailureCount++;
                }
                return null;
            }

            try
            {
                var asset = Resources.Load<T>(address);
                
                lock (_lockObject)
                {
                    if (asset != null)
                    {
                        _loadedAssetCount++;
                        _loadSuccessCount++;
                    }
                    else
                    {
                        _loadFailureCount++;
                    }
                }

                return asset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourcesAssetProvider] 加载资源异常: {address}, 错误: {ex.Message}");
                lock (_lockObject)
                {
                    _loadFailureCount++;
                }
                return null;
            }
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="address">资源地址</param>
        /// <returns>异步任务，包含加载的资源对象</returns>
        public async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"[ResourcesAssetProvider] 资源地址不能为空");
                lock (_lockObject)
                {
                    _loadFailureCount++;
                }
                return null;
            }

            try
            {
                // 使用Resources.LoadAsync进行异步加载
                var request = Resources.LoadAsync<T>(address);
                
                // 等待加载完成
                while (!request.isDone)
                {
                    await Task.Yield();
                }

                var asset = request.asset as T;
                
                lock (_lockObject)
                {
                    if (asset != null)
                    {
                        _loadedAssetCount++;
                        _loadSuccessCount++;
                    }
                    else
                    {
                        _loadFailureCount++;
                    }
                }

                return asset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourcesAssetProvider] 异步加载资源异常: {address}, 错误: {ex.Message}");
                lock (_lockObject)
                {
                    _loadFailureCount++;
                }
                return null;
            }
        }

        /// <summary>
        /// 同步加载资源（非泛型版本）
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <param name="type">资源类型</param>
        /// <returns>加载的资源对象</returns>
        public UnityEngine.Object LoadAsset(string address, Type type)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"[ResourcesAssetProvider] 资源地址不能为空");
                lock (_lockObject)
                {
                    _loadFailureCount++;
                }
                return null;
            }

            if (type == null)
            {
                Debug.LogError($"[ResourcesAssetProvider] 资源类型不能为空");
                lock (_lockObject)
                {
                    _loadFailureCount++;
                }
                return null;
            }

            try
            {
                var asset = Resources.Load(address, type);
                
                lock (_lockObject)
                {
                    if (asset != null)
                    {
                        _loadedAssetCount++;
                        _loadSuccessCount++;
                    }
                    else
                    {
                        _loadFailureCount++;
                    }
                }

                return asset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourcesAssetProvider] 加载资源异常: {address}, 错误: {ex.Message}");
                lock (_lockObject)
                {
                    _loadFailureCount++;
                }
                return null;
            }
        }

        /// <summary>
        /// 异步加载资源（非泛型版本）
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <param name="type">资源类型</param>
        /// <returns>异步任务，包含加载的资源对象</returns>
        public async Task<UnityEngine.Object> LoadAssetAsync(string address, Type type)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"[ResourcesAssetProvider] 资源地址不能为空");
                lock (_lockObject)
                {
                    _loadFailureCount++;
                }
                return null;
            }

            if (type == null)
            {
                Debug.LogError($"[ResourcesAssetProvider] 资源类型不能为空");
                lock (_lockObject)
                {
                    _loadFailureCount++;
                }
                return null;
            }

            try
            {
                // 使用Resources.LoadAsync进行异步加载
                var request = Resources.LoadAsync(address, type);
                
                // 等待加载完成
                while (!request.isDone)
                {
                    await Task.Yield();
                }

                var asset = request.asset;
                
                lock (_lockObject)
                {
                    if (asset != null)
                    {
                        _loadedAssetCount++;
                        _loadSuccessCount++;
                    }
                    else
                    {
                        _loadFailureCount++;
                    }
                }

                return asset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourcesAssetProvider] 异步加载资源异常: {address}, 错误: {ex.Message}");
                lock (_lockObject)
                {
                    _loadFailureCount++;
                }
                return null;
            }
        }

        /// <summary>
        /// 释放资源
        /// Resources系统加载的资源通常由Unity自动管理，这里主要用于统计
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <param name="asset">资源对象</param>
        public void ReleaseAsset(string address, UnityEngine.Object asset)
        {
            lock (_lockObject)
            {
                _releaseCount++;
                if (_loadedAssetCount > 0)
                {
                    _loadedAssetCount--;
                }
            }

            // Resources系统的资源通常不需要手动释放
            // 但可以调用Resources.UnloadAsset来释放非GameObject资源
            if (asset != null && !(asset is GameObject) && !(asset is Component))
            {
                try
                {
                    Resources.UnloadAsset(asset);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ResourcesAssetProvider] 释放资源时发生异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 检查资源是否存在
        /// Resources系统没有直接的存在性检查，这里尝试加载来判断
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <returns>如果资源存在返回true，否则返回false</returns>
        public bool AssetExists(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return false;
            }

            try
            {
                // 尝试加载资源来检查是否存在
                var asset = Resources.Load(address);
                return asset != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 预加载资源
        /// Resources系统的预加载实际上就是加载资源
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <returns>预加载任务</returns>
        public async Task PreloadAsync(string address)
        {
            // 对于Resources系统，预加载就是异步加载资源
            await LoadAssetAsync<UnityEngine.Object>(address);
        }

        /// <summary>
        /// 获取提供者统计信息
        /// </summary>
        /// <returns>提供者统计信息</returns>
        public AssetProviderStats GetStats()
        {
            lock (_lockObject)
            {
                return new AssetProviderStats
                {
                    LoadedAssetCount = _loadedAssetCount,
                    LoadSuccessCount = _loadSuccessCount,
                    LoadFailureCount = _loadFailureCount,
                    ReleaseCount = _releaseCount
                };
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // Resources系统的资源由Unity管理，这里主要重置统计信息
            lock (_lockObject)
            {
                _loadedAssetCount = 0;
                _loadSuccessCount = 0;
                _loadFailureCount = 0;
                _releaseCount = 0;
            }
        }
    }
}
