using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using xFrame.Core.DataStructures;

namespace xFrame.Core.ResourceManager
{
    /// <summary>
    /// 基于Addressable的资源管理器实现
    /// 内部封装Unity Addressable系统，对外提供统一的资源管理接口
    /// </summary>
    public class AddressableAssetManager : IAssetManager, IDisposable
    {
        private readonly ILRUCache<string, UnityEngine.Object> _assetCache;
#if UNITY_ADDRESSABLES
        private readonly Dictionary<string, AsyncOperationHandle> _loadingOperations;
#else
        private readonly Dictionary<string, ResourceRequest> _loadingOperations;
#endif
        private readonly Dictionary<UnityEngine.Object, string> _assetToAddressMap;
        private readonly object _lockObject = new object();

        // 缓存统计信息
        private int _cacheHitCount;
        private int _cacheMissCount;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cacheCapacity">缓存容量，默认为100</param>
        public AddressableAssetManager(int cacheCapacity = 100)
        {
            // 直接实例化ThreadSafeLRUCache
            _assetCache = new ThreadSafeLRUCache<string, UnityEngine.Object>(cacheCapacity);
#if UNITY_ADDRESSABLES
            _loadingOperations = new Dictionary<string, AsyncOperationHandle>();
#else
            _loadingOperations = new Dictionary<string, ResourceRequest>();
#endif
            _assetToAddressMap = new Dictionary<UnityEngine.Object, string>();
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="address">资源地址</param>
        /// <returns>加载的资源对象，失败时返回null</returns>
        public T LoadAsset<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"[AddressableAssetManager] 资源地址不能为空");
                return null;
            }

            // 检查缓存
            if (_assetCache.TryGet(address, out var cachedAsset))
            {
                lock (_lockObject)
                {
                    _cacheHitCount++;
                }
                return cachedAsset as T;
            }

            lock (_lockObject)
            {
                _cacheMissCount++;
            }

            try
            {
#if UNITY_ADDRESSABLES
                // 使用Addressable同步加载
                var handle = Addressables.LoadAssetAsync<T>(address);
                var asset = handle.WaitForCompletion();
#else
                // 没有Addressable时使用Resources.Load
                var asset = Resources.Load<T>(address);
#endif

                if (asset != null)
                {
                    // 添加到缓存
                    _assetCache.Put(address, asset);
                    _assetToAddressMap[asset] = address;

                    // 保存操作句柄用于后续释放
                    lock (_lockObject)
                    {
#if UNITY_ADDRESSABLES
                        _loadingOperations[address] = handle;
#endif
                    }
                }
                else
                {
                    Debug.LogError($"[AddressableAssetManager] 加载资源失败: {address}");
                }

                return asset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableAssetManager] 加载资源异常: {address}, 错误: {ex.Message}");
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
                Debug.LogError($"[AddressableAssetManager] 资源地址不能为空");
                return null;
            }

            // 检查缓存
            if (_assetCache.TryGet(address, out var cachedAsset))
            {
                lock (_lockObject)
                {
                    _cacheHitCount++;
                }
                return cachedAsset as T;
            }

            lock (_lockObject)
            {
                _cacheMissCount++;
            }

            try
            {
#if UNITY_ADDRESSABLES
                // 使用Addressable异步加载
                var handle = Addressables.LoadAssetAsync<T>(address);
                var asset = await handle.Task;
#else
                // 没有Addressable时使用Resources.LoadAsync
                var request = Resources.LoadAsync<T>(address);
                
                while (!request.isDone)
                {
                    await Task.Yield();
                }
                
                var asset = request.asset as T;
#endif

                if (asset != null)
                {
                    // 添加到缓存
                    _assetCache.Put(address, asset);
                    _assetToAddressMap[asset] = address;

                    // 保存操作句柄用于后续释放
                    lock (_lockObject)
                    {
#if UNITY_ADDRESSABLES
                        _loadingOperations[address] = handle;
#else
                        _loadingOperations[address] = request;
#endif
                    }
                }
                else
                {
                    Debug.LogError($"[AddressableAssetManager] 异步加载资源失败: {address}");
                }

                return asset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableAssetManager] 异步加载资源异常: {address}, 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 同步加载资源（非泛型版本）
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <param name="type">资源类型</param>
        /// <returns>加载的资源对象，失败时返回null</returns>
        public UnityEngine.Object LoadAsset(string address, Type type)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"[AddressableAssetManager] 资源地址不能为空");
                return null;
            }

            if (type == null)
            {
                Debug.LogError($"[AddressableAssetManager] 资源类型不能为空");
                return null;
            }

            // 检查缓存
            if (_assetCache.TryGet(address, out var cachedAsset))
            {
                lock (_lockObject)
                {
                    _cacheHitCount++;
                }
                return cachedAsset;
            }

            lock (_lockObject)
            {
                _cacheMissCount++;
            }

            try
            {
#if UNITY_ADDRESSABLES
                // 使用Addressable同步加载
                var handle = Addressables.LoadAssetAsync(address, type);
                var asset = handle.WaitForCompletion();
#else
                // 没有Addressable时使用Resources.Load
                var asset = Resources.Load(address, type);
#endif

                if (asset != null)
                {
                    // 添加到缓存
                    _assetCache.Put(address, asset);
                    _assetToAddressMap[asset] = address;

                    // 保存操作句柄用于后续释放
                    lock (_lockObject)
                    {
#if UNITY_ADDRESSABLES
                        _loadingOperations[address] = handle;
#endif
                    }
                }
                else
                {
                    Debug.LogError($"[AddressableAssetManager] 加载资源失败: {address}");
                }

                return asset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableAssetManager] 加载资源异常: {address}, 错误: {ex.Message}");
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
                Debug.LogError($"[AddressableAssetManager] 资源地址不能为空");
                return null;
            }

            if (type == null)
            {
                Debug.LogError($"[AddressableAssetManager] 资源类型不能为空");
                return null;
            }

            // 检查缓存
            if (_assetCache.TryGet(address, out var cachedAsset))
            {
                lock (_lockObject)
                {
                    _cacheHitCount++;
                }
                return cachedAsset;
            }

            lock (_lockObject)
            {
                _cacheMissCount++;
            }

            try
            {
#if UNITY_ADDRESSABLES
                // 使用Addressable异步加载
                var handle = Addressables.LoadAssetAsync(address, type);
                var asset = await handle.Task;
#else
                // 没有Addressable时使用Resources.LoadAsync
                var request = Resources.LoadAsync(address, type);
                
                while (!request.isDone)
                {
                    await Task.Yield();
                }
                
                var asset = request.asset;
#endif

                if (asset != null)
                {
                    // 添加到缓存
                    _assetCache.Put(address, asset);
                    _assetToAddressMap[asset] = address;

                    // 保存操作句柄用于后续释放
                    lock (_lockObject)
                    {
#if UNITY_ADDRESSABLES
                        _loadingOperations[address] = handle;
#else
                        _loadingOperations[address] = request;
#endif
                    }
                }
                else
                {
                    Debug.LogError($"[AddressableAssetManager] 异步加载资源失败: {address}");
                }

                return asset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableAssetManager] 异步加载资源异常: {address}, 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="asset">要释放的资源对象</param>
        public void ReleaseAsset(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                Debug.LogWarning($"[AddressableAssetManager] 尝试释放空资源");
                return;
            }

            // 查找资源对应的地址
            if (_assetToAddressMap.TryGetValue(asset, out var address))
            {
                ReleaseAsset(address);
            }
            else
            {
                Debug.LogWarning($"[AddressableAssetManager] 未找到资源对应的地址: {asset.name}");
            }
        }

        /// <summary>
        /// 释放指定地址的资源
        /// </summary>
        /// <param name="address">资源地址</param>
        public void ReleaseAsset(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogWarning($"[AddressableAssetManager] 资源地址不能为空");
                return;
            }

            lock (_lockObject)
            {
                // 从缓存中移除
                if (_assetCache.TryGet(address, out var asset))
                {
                    _assetCache.Remove(address);
                    _assetToAddressMap.Remove(asset);
                }

                // 释放Addressable资源
                if (_loadingOperations.TryGetValue(address, out var operation))
                {
#if UNITY_ADDRESSABLES
                    if (operation.IsValid())
                    {
                        Addressables.Release(operation);
                    }
#endif
                    _loadingOperations.Remove(address);
                }
            }
        }

        /// <summary>
        /// 预加载资源到缓存
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <returns>预加载任务</returns>
        public async Task PreloadAssetAsync(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"[AddressableAssetManager] 资源地址不能为空");
                return;
            }

            // 如果已经缓存，直接返回
            if (_assetCache.ContainsKey(address))
            {
                return;
            }

            try
            {
#if UNITY_ADDRESSABLES
                // 异步加载资源到缓存
                var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(address);
                var asset = await handle.Task;
#else
                // 没有Addressable时使用Resources.LoadAsync
                var request = Resources.LoadAsync<UnityEngine.Object>(address);
                
                while (!request.isDone)
                {
                    await Task.Yield();
                }
                
                var asset = request.asset;
#endif

                if (asset != null)
                {
                    _assetCache.Put(address, asset);
                    _assetToAddressMap[asset] = address;

                    lock (_lockObject)
                    {
#if UNITY_ADDRESSABLES
                        _loadingOperations[address] = handle;
#else
                        _loadingOperations[address] = request;
#endif
                    }
                }
                else
                {
                    Debug.LogError($"[AddressableAssetManager] 预加载资源失败: {address}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableAssetManager] 预加载资源异常: {address}, 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查资源是否已缓存
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <returns>如果资源已缓存返回true，否则返回false</returns>
        public bool IsAssetCached(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return false;
            }

            return _assetCache.ContainsKey(address);
        }

        /// <summary>
        /// 清理所有缓存的资源
        /// </summary>
        public void ClearCache()
        {
            lock (_lockObject)
            {
                // 释放所有Addressable资源
                foreach (var kvp in _loadingOperations)
                {
#if UNITY_ADDRESSABLES
                    if (kvp.Value.IsValid())
                    {
                        Addressables.Release(kvp.Value);
                    }
#endif
                }

                // 清理所有集合
                _loadingOperations.Clear();
                _assetToAddressMap.Clear();
                _assetCache.Clear();

                // 重置统计信息
                _cacheHitCount = 0;
                _cacheMissCount = 0;
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存统计信息</returns>
        public AssetCacheStats GetCacheStats()
        {
            lock (_lockObject)
            {
                return new AssetCacheStats
                {
                    CachedAssetCount = _assetCache.Count,
                    CacheHitCount = _cacheHitCount,
                    CacheMissCount = _cacheMissCount
                };
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            ClearCache();
        }
    }
}
