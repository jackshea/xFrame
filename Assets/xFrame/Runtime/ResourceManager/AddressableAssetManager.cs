using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using xFrame.Runtime.DataStructures;
using Object = UnityEngine.Object;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace xFrame.Runtime.ResourceManager
{
    /// <summary>
    ///     基于Addressable的资源管理器实现
    ///     内部封装Unity Addressable系统，对外提供统一的资源管理接口
    /// </summary>
    public class AddressableAssetManager : IAssetManager, IDisposable
    {
        private readonly ILRUCache<string, Object> _assetCache;
        private readonly Dictionary<string, Task<Object>> _inflightLoads;
        private readonly Dictionary<Object, string> _assetToAddressMap;
        private readonly object _lockObject = new();
        private readonly Func<string, Type, Object> _syncLoader;
        private readonly Func<string, Type, Task<Object>> _asyncLoader;

        // 缓存统计信息
        private int _cacheHitCount;
        private int _cacheMissCount;

        /// <summary>
        ///     构造函数
        /// </summary>
        /// <param name="cacheCapacity">缓存容量，默认为100</param>
        public AddressableAssetManager(int cacheCapacity = 100)
            : this(cacheCapacity, null, null)
        {
        }

        internal AddressableAssetManager(
            int cacheCapacity,
            Func<string, Type, Object> syncLoader,
            Func<string, Type, Task<Object>> asyncLoader)
        {
            // 直接实例化ThreadSafeLRUCache
            _assetCache = new ThreadSafeLRUCache<string, Object>(cacheCapacity);
            _inflightLoads = new Dictionary<string, Task<Object>>();
            _assetToAddressMap = new Dictionary<Object, string>();
            _syncLoader = syncLoader ?? DefaultSyncLoad;
            _asyncLoader = asyncLoader ?? DefaultAsyncLoad;
        }

        /// <summary>
        ///     同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="address">资源地址</param>
        /// <returns>加载的资源对象，失败时返回null</returns>
        public T LoadAsset<T>(string address) where T : Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("[AddressableAssetManager] 资源地址不能为空");
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

            Task<Object> inflightTask = null;
            lock (_lockObject)
            {
                if (_inflightLoads.TryGetValue(address, out inflightTask))
                {
                    _cacheHitCount++;
                }
                else
                {
                    _cacheMissCount++;
                }
            }

            if (inflightTask != null) return inflightTask.GetAwaiter().GetResult() as T;

            try
            {
                var asset = _syncLoader.Invoke(address, typeof(T)) as T;

                if (asset != null)
                {
                    TrackLoadedAsset(address, asset);
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
        ///     异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="address">资源地址</param>
        /// <returns>异步任务，包含加载的资源对象</returns>
        public async Task<T> LoadAssetAsync<T>(string address) where T : Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("[AddressableAssetManager] 资源地址不能为空");
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

            return await LoadAssetAsyncInternal(address, typeof(T)) as T;
        }

        /// <summary>
        ///     同步加载资源（非泛型版本）
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <param name="type">资源类型</param>
        /// <returns>加载的资源对象，失败时返回null</returns>
        public Object LoadAsset(string address, Type type)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("[AddressableAssetManager] 资源地址不能为空");
                return null;
            }

            if (type == null)
            {
                Debug.LogError("[AddressableAssetManager] 资源类型不能为空");
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

            Task<Object> inflightTask = null;
            lock (_lockObject)
            {
                if (_inflightLoads.TryGetValue(address, out inflightTask))
                {
                    _cacheHitCount++;
                }
                else
                {
                    _cacheMissCount++;
                }
            }

            if (inflightTask != null) return inflightTask.GetAwaiter().GetResult();

            try
            {
                var asset = _syncLoader.Invoke(address, type);

                if (asset != null)
                {
                    TrackLoadedAsset(address, asset);
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
        ///     异步加载资源（非泛型版本）
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <param name="type">资源类型</param>
        /// <returns>异步任务，包含加载的资源对象</returns>
        public async Task<Object> LoadAssetAsync(string address, Type type)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("[AddressableAssetManager] 资源地址不能为空");
                return null;
            }

            if (type == null)
            {
                Debug.LogError("[AddressableAssetManager] 资源类型不能为空");
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

            return await LoadAssetAsyncInternal(address, type);
        }

        /// <summary>
        ///     释放资源
        /// </summary>
        /// <param name="asset">要释放的资源对象</param>
        public void ReleaseAsset(Object asset)
        {
            if (asset == null)
            {
                Debug.LogWarning("[AddressableAssetManager] 尝试释放空资源");
                return;
            }

            // 查找资源对应的地址
            if (_assetToAddressMap.TryGetValue(asset, out var address))
                ReleaseAsset(address);
            else
                Debug.LogWarning($"[AddressableAssetManager] 未找到资源对应的地址: {asset.name}");
        }

        /// <summary>
        ///     释放指定地址的资源
        /// </summary>
        /// <param name="address">资源地址</param>
        public void ReleaseAsset(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogWarning("[AddressableAssetManager] 资源地址不能为空");
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

#if UNITY_ADDRESSABLES
                if (asset != null) Addressables.Release(asset);
#endif
            }
        }

        /// <summary>
        ///     预加载资源到缓存
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <returns>预加载任务</returns>
        public async Task PreloadAssetAsync(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("[AddressableAssetManager] 资源地址不能为空");
                return;
            }

            // 如果已经缓存，直接返回
            if (_assetCache.ContainsKey(address)) return;

            try
            {
#if UNITY_ADDRESSABLES
                // 异步加载资源到缓存
                var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(address);
                var asset = await handle.Task;
#else
                // 没有Addressable时使用Resources.LoadAsync
                var request = Resources.LoadAsync<Object>(address);

                while (!request.isDone) await Task.Yield();

                var asset = request.asset;
#endif

                if (asset != null)
                {
                    TrackLoadedAsset(address, asset);
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
        ///     检查资源是否已缓存
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <returns>如果资源已缓存返回true，否则返回false</returns>
        public bool IsAssetCached(string address)
        {
            if (string.IsNullOrEmpty(address)) return false;

            return _assetCache.ContainsKey(address);
        }

        /// <summary>
        ///     清理所有缓存的资源
        /// </summary>
        public void ClearCache()
        {
            lock (_lockObject)
            {
                // 清理所有集合
#if UNITY_ADDRESSABLES
                foreach (var asset in _assetToAddressMap.Keys.ToList()) Addressables.Release(asset);
#endif
                _inflightLoads.Clear();
                _assetToAddressMap.Clear();
                _assetCache.Clear();

                // 重置统计信息
                _cacheHitCount = 0;
                _cacheMissCount = 0;
            }
        }

        /// <summary>
        ///     获取缓存统计信息
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
        ///     释放资源
        /// </summary>
        public void Dispose()
        {
            ClearCache();
        }

        private async Task<Object> LoadAssetAsyncInternal(string address, Type type)
        {
            Task<Object> loadTask;
            var shouldCreate = false;

            lock (_lockObject)
            {
                if (_assetCache.TryGet(address, out var cachedAsset))
                {
                    _cacheHitCount++;
                    return cachedAsset;
                }

                if (_inflightLoads.TryGetValue(address, out loadTask))
                {
                    _cacheHitCount++;
                }
                else
                {
                    _cacheMissCount++;
                    loadTask = LoadAndTrackAsync(address, type);
                    _inflightLoads[address] = loadTask;
                    shouldCreate = true;
                }
            }

            try
            {
                return await loadTask;
            }
            finally
            {
                if (shouldCreate)
                {
                    lock (_lockObject)
                    {
                        if (_inflightLoads.TryGetValue(address, out var currentTask) && currentTask == loadTask)
                            _inflightLoads.Remove(address);
                    }
                }
            }
        }

        private async Task<Object> LoadAndTrackAsync(string address, Type type)
        {
            try
            {
                var asset = await _asyncLoader.Invoke(address, type);
                if (asset != null)
                {
                    TrackLoadedAsset(address, asset);
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

        private void TrackLoadedAsset(string address, Object asset)
        {
            if (asset == null) return;

            lock (_lockObject)
            {
                _assetCache.Put(address, asset);
                _assetToAddressMap[asset] = address;
            }
        }

        private static Object DefaultSyncLoad(string address, Type type)
        {
#if UNITY_ADDRESSABLES
            var handle = Addressables.LoadAssetAsync(address, type);
            return handle.WaitForCompletion();
#else
            return Resources.Load(address, type);
#endif
        }

        private static async Task<Object> DefaultAsyncLoad(string address, Type type)
        {
#if UNITY_ADDRESSABLES
            var handle = Addressables.LoadAssetAsync(address, type);
            return await handle.Task;
#else
            var request = Resources.LoadAsync(address, type);
            while (!request.isDone) await Task.Yield();
            return request.asset;
#endif
        }
    }
}
