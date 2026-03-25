using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    public class AddressableAssetManager : IAssetManager, IResourceDomainAssetManager, IDisposable
    {
        private readonly ILRUCache<string, Object> _assetCache;
        private readonly Dictionary<string, Task<Object>> _inflightLoads;
        private readonly Dictionary<ResourceLoadKey, SharedDomainLoadRecord> _domainInflightLoads;
        private readonly Dictionary<long, DomainRequestRecord> _domainRequestRecords;
        private readonly Dictionary<long, HashSet<long>> _domainToRequestIds;
        private readonly Dictionary<Object, string> _assetToAddressMap;
        private readonly object _lockObject = new();
        private readonly Func<string, Type, Object> _syncLoader;
        private readonly Func<string, Type, Task<Object>> _asyncLoader;
        private readonly Action<Object> _releaseAction;

        // 缓存统计信息
        private int _cacheHitCount;
        private int _cacheMissCount;
        private long _nextDomainId;
        private long _nextRequestId;

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
            Func<string, Type, Task<Object>> asyncLoader,
            Action<Object> releaseAction = null)
        {
            // 直接实例化ThreadSafeLRUCache
            _assetCache = new ThreadSafeLRUCache<string, Object>(cacheCapacity);
            _inflightLoads = new Dictionary<string, Task<Object>>();
            _domainInflightLoads = new Dictionary<ResourceLoadKey, SharedDomainLoadRecord>();
            _domainRequestRecords = new Dictionary<long, DomainRequestRecord>();
            _domainToRequestIds = new Dictionary<long, HashSet<long>>();
            _assetToAddressMap = new Dictionary<Object, string>();
            _syncLoader = syncLoader ?? DefaultSyncLoad;
            _asyncLoader = asyncLoader ?? DefaultAsyncLoad;
            _releaseAction = releaseAction ?? DefaultReleaseAsset;
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

            return await LoadAssetAsyncInternal(address, typeof(T)).ConfigureAwait(false) as T;
        }

        /// <summary>
        ///     创建新的资源域。
        /// </summary>
        /// <param name="name">资源域名称。</param>
        /// <returns>新创建的资源域实例。</returns>
        public ResourceDomain CreateDomain(string name = null)
        {
            var domainId = Interlocked.Increment(ref _nextDomainId);
            return new ResourceDomain(domainId, name);
        }

        /// <summary>
        ///     销毁指定资源域。
        /// </summary>
        /// <param name="domain">要销毁的资源域。</param>
        public void DestroyDomain(ResourceDomain domain)
        {
            if (domain == null) throw new ArgumentNullException(nameof(domain));

            InvalidateDomainRequests(domain, DomainRequestInvalidationReason.DomainDestroyed);
            domain.Destroy();
        }

        /// <summary>
        ///     将资源域推进到下一代生命周期。
        /// </summary>
        /// <param name="domain">要续代的资源域。</param>
        public void RenewDomain(ResourceDomain domain)
        {
            if (domain == null) throw new ArgumentNullException(nameof(domain));

            InvalidateDomainRequests(domain, DomainRequestInvalidationReason.GenerationMismatch);
            domain.Renew();
        }

        /// <summary>
        ///     在指定资源域内异步加载资源。
        /// </summary>
        /// <typeparam name="T">资源类型。</typeparam>
        /// <param name="domain">资源域。</param>
        /// <param name="address">资源地址。</param>
        /// <returns>若域在完成时仍有效则返回资源，否则返回 null。</returns>
        public async Task<T> LoadAssetAsync<T>(ResourceDomain domain, string address) where T : Object
        {
            return await LoadAssetAsync(domain, address, typeof(T)).ConfigureAwait(false) as T;
        }

        /// <summary>
        ///     在指定资源域内异步加载资源。
        /// </summary>
        /// <param name="domain">资源域。</param>
        /// <param name="address">资源地址。</param>
        /// <param name="type">资源类型。</param>
        /// <returns>若域在完成时仍有效则返回资源，否则返回 null。</returns>
        public Task<Object> LoadAssetAsync(ResourceDomain domain, string address, Type type)
        {
            if (domain == null) throw new ArgumentNullException(nameof(domain));

            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("[AddressableAssetManager] 资源地址不能为空");
                return Task.FromResult<Object>(null);
            }

            if (type == null)
            {
                Debug.LogError("[AddressableAssetManager] 资源类型不能为空");
                return Task.FromResult<Object>(null);
            }

            domain.GetSnapshot(out var generation, out var isAlive);
            if (!isAlive)
            {
                Debug.LogWarning($"[AddressableAssetManager] 资源域已销毁，忽略异步加载请求: {domain.Name}, 地址: {address}");
                return Task.FromResult<Object>(null);
            }

            if (_assetCache.TryGet(address, out var cachedAsset))
            {
                lock (_lockObject)
                {
                    _cacheHitCount++;
                }

                return Task.FromResult(cachedAsset);
            }

            var requestId = Interlocked.Increment(ref _nextRequestId);
            var taskCompletionSource = new TaskCompletionSource<Object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var loadKey = new ResourceLoadKey(address, type);
            SharedDomainLoadRecord sharedLoadRecord;
            var shouldObserve = false;

            lock (_lockObject)
            {
                if (_assetCache.TryGet(address, out cachedAsset))
                {
                    _cacheHitCount++;
                    return Task.FromResult(cachedAsset);
                }

                if (!_domainInflightLoads.TryGetValue(loadKey, out sharedLoadRecord))
                {
                    _cacheMissCount++;
                    sharedLoadRecord = new SharedDomainLoadRecord(loadKey, _asyncLoader.Invoke(address, type));
                    _domainInflightLoads[loadKey] = sharedLoadRecord;
                    shouldObserve = true;
                }
                else
                {
                    _cacheHitCount++;
                }

                var requestRecord = new DomainRequestRecord(
                    requestId,
                    domain,
                    generation,
                    address,
                    type,
                    loadKey,
                    taskCompletionSource);

                _domainRequestRecords[requestId] = requestRecord;
                sharedLoadRecord.RequestIds.Add(requestId);

                if (!_domainToRequestIds.TryGetValue(domain.DomainId, out var requestIds))
                {
                    requestIds = new HashSet<long>();
                    _domainToRequestIds[domain.DomainId] = requestIds;
                }

                requestIds.Add(requestId);
            }

            if (shouldObserve)
            {
                _ = ObserveSharedDomainLoadAsync(sharedLoadRecord);
            }

            return taskCompletionSource.Task;
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

            return await LoadAssetAsyncInternal(address, type).ConfigureAwait(false);
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

                SafeReleaseAsset(asset);
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
                var asset = await handle.Task.ConfigureAwait(false);
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
                foreach (var asset in _assetToAddressMap.Keys.ToList()) SafeReleaseAsset(asset);
#endif
                _inflightLoads.Clear();
                _domainInflightLoads.Clear();
                _domainRequestRecords.Clear();
                _domainToRequestIds.Clear();
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
                return await loadTask.ConfigureAwait(false);
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
                var asset = await _asyncLoader.Invoke(address, type).ConfigureAwait(false);
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
            return await handle.Task.ConfigureAwait(false);
#else
            var request = Resources.LoadAsync(address, type);
            while (!request.isDone) await Task.Yield();
            return request.asset;
#endif
        }

        private static void DefaultReleaseAsset(Object asset)
        {
            if (asset == null) return;

#if UNITY_ADDRESSABLES
            Addressables.Release(asset);
#endif
        }

        private void InvalidateDomainRequests(ResourceDomain domain, DomainRequestInvalidationReason reason)
        {
            List<long> requestIds = null;

            lock (_lockObject)
            {
                if (_domainToRequestIds.TryGetValue(domain.DomainId, out var requestIdSet))
                {
                    requestIds = requestIdSet.ToList();
                    _domainToRequestIds.Remove(domain.DomainId);
                }

                if (requestIds == null) return;

                foreach (var requestId in requestIds)
                {
                    if (_domainRequestRecords.TryGetValue(requestId, out var requestRecord))
                    {
                        requestRecord.IsCanceled = true;
                        requestRecord.InvalidationReason = reason;
                    }
                }
            }
        }

        private async Task ObserveSharedDomainLoadAsync(SharedDomainLoadRecord sharedLoadRecord)
        {
            Object asset = null;

            try
            {
                asset = await sharedLoadRecord.LoadTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableAssetManager] 域绑定异步加载异常: {sharedLoadRecord.LoadKey.Address}, 错误: {ex.Message}");
            }

            CompleteSharedDomainLoad(sharedLoadRecord.LoadKey, asset);
        }

        private void CompleteSharedDomainLoad(ResourceLoadKey loadKey, Object asset)
        {
            List<DomainRequestRecord> requestRecords;

            lock (_lockObject)
            {
                if (!_domainInflightLoads.TryGetValue(loadKey, out var sharedLoadRecord))
                {
                    if (asset != null) SafeReleaseAsset(asset);
                    return;
                }

                _domainInflightLoads.Remove(loadKey);
                requestRecords = sharedLoadRecord.RequestIds
                    .Select(requestId => _domainRequestRecords.TryGetValue(requestId, out var record) ? record : null)
                    .Where(record => record != null)
                    .ToList();
            }

            var validRequestExists = false;
            foreach (var requestRecord in requestRecords)
            {
                if (TryMarkRequestCompleted(requestRecord, out _, out var invalidationReason))
                {
                    validRequestExists = true;
                    continue;
                }

                Debug.LogWarning(
                    $"[AddressableAssetManager] 丢弃孤儿资源结果: Domain={requestRecord.Domain.Name}, Generation={requestRecord.Generation}, Address={requestRecord.Address}, Reason={invalidationReason}");
            }

            if (asset != null && validRequestExists)
            {
                TrackLoadedAsset(loadKey.Address, asset);
            }
            else if (asset != null)
            {
                SafeReleaseAsset(asset);
            }

            foreach (var requestRecord in requestRecords)
            {
                var requestResult = validRequestExists && requestRecord.WasAccepted ? asset : null;
                CleanupCompletedRequest(requestRecord, requestResult);
            }
        }

        private bool TryMarkRequestCompleted(
            DomainRequestRecord requestRecord,
            out ResourceDomain currentDomain,
            out DomainRequestInvalidationReason invalidationReason)
        {
            currentDomain = requestRecord.Domain;
            invalidationReason = DomainRequestInvalidationReason.None;

            lock (_lockObject)
            {
                if (requestRecord.IsCompleted)
                {
                    invalidationReason = DomainRequestInvalidationReason.AlreadyCompleted;
                    return false;
                }

                if (requestRecord.IsCanceled)
                {
                    invalidationReason = requestRecord.InvalidationReason;
                    requestRecord.IsCompleted = true;
                    return false;
                }

                requestRecord.Domain.GetSnapshot(out var currentGeneration, out var isAlive);
                if (!isAlive)
                {
                    invalidationReason = DomainRequestInvalidationReason.DomainDestroyed;
                    requestRecord.IsCompleted = true;
                    requestRecord.InvalidationReason = invalidationReason;
                    return false;
                }

                if (currentGeneration != requestRecord.Generation)
                {
                    invalidationReason = DomainRequestInvalidationReason.GenerationMismatch;
                    requestRecord.IsCompleted = true;
                    requestRecord.InvalidationReason = invalidationReason;
                    return false;
                }

                requestRecord.IsCompleted = true;
                requestRecord.WasAccepted = true;
                return true;
            }
        }

        private void CleanupCompletedRequest(DomainRequestRecord requestRecord, Object asset)
        {
            TaskCompletionSource<Object> completionSource = null;

            lock (_lockObject)
            {
                if (_domainRequestRecords.Remove(requestRecord.RequestId))
                {
                    if (_domainToRequestIds.TryGetValue(requestRecord.Domain.DomainId, out var requestIds))
                    {
                        requestIds.Remove(requestRecord.RequestId);
                        if (requestIds.Count == 0)
                        {
                            _domainToRequestIds.Remove(requestRecord.Domain.DomainId);
                        }
                    }
                }

                completionSource = requestRecord.CompletionSource;
            }

            completionSource.TrySetResult(asset);
        }

        private void SafeReleaseAsset(Object asset)
        {
            if (asset == null) return;

            try
            {
                _releaseAction.Invoke(asset);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableAssetManager] 释放资源异常: {asset.name}, 错误: {ex.Message}");
            }
        }

        private readonly struct ResourceLoadKey : IEquatable<ResourceLoadKey>
        {
            public ResourceLoadKey(string address, Type assetType)
            {
                Address = address;
                AssetType = assetType;
            }

            public string Address { get; }

            public Type AssetType { get; }

            public bool Equals(ResourceLoadKey other)
            {
                return string.Equals(Address, other.Address, StringComparison.Ordinal) && AssetType == other.AssetType;
            }

            public override bool Equals(object obj)
            {
                return obj is ResourceLoadKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Address != null ? StringComparer.Ordinal.GetHashCode(Address) : 0) * 397) ^
                           (AssetType != null ? AssetType.GetHashCode() : 0);
                }
            }
        }

        private sealed class SharedDomainLoadRecord
        {
            public SharedDomainLoadRecord(ResourceLoadKey loadKey, Task<Object> loadTask)
            {
                LoadKey = loadKey;
                LoadTask = loadTask;
                RequestIds = new HashSet<long>();
            }

            public ResourceLoadKey LoadKey { get; }

            public Task<Object> LoadTask { get; }

            public HashSet<long> RequestIds { get; }
        }

        private sealed class DomainRequestRecord
        {
            public DomainRequestRecord(
                long requestId,
                ResourceDomain domain,
                int generation,
                string address,
                Type assetType,
                ResourceLoadKey loadKey,
                TaskCompletionSource<Object> completionSource)
            {
                RequestId = requestId;
                Domain = domain;
                Generation = generation;
                Address = address;
                AssetType = assetType;
                LoadKey = loadKey;
                CompletionSource = completionSource;
                InvalidationReason = DomainRequestInvalidationReason.None;
            }

            public long RequestId { get; }

            public ResourceDomain Domain { get; }

            public int Generation { get; }

            public string Address { get; }

            public Type AssetType { get; }

            public ResourceLoadKey LoadKey { get; }

            public TaskCompletionSource<Object> CompletionSource { get; }

            public bool IsCanceled { get; set; }

            public bool IsCompleted { get; set; }

            public bool WasAccepted { get; set; }

            public DomainRequestInvalidationReason InvalidationReason { get; set; }
        }

        private enum DomainRequestInvalidationReason
        {
            None = 0,
            DomainDestroyed = 1,
            GenerationMismatch = 2,
            AlreadyCompleted = 3
        }
    }
}
