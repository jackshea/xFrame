using System.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace xFrame.Core.ResourceManager
{
    /// <summary>
    /// AssetManager使用示例
    /// 演示如何在不引用Addressable命名空间的情况下使用资源管理功能
    /// </summary>
    public class AssetManagerExample : MonoBehaviour
    {
        [Inject]
        private IAssetManager _assetManager;

        /// <summary>
        /// 示例：同步加载资源
        /// </summary>
        public void ExampleSyncLoad()
        {
            // 加载GameObject资源
            var prefab = _assetManager.LoadAsset<GameObject>("MyPrefab");
            if (prefab != null)
            {
                Debug.Log($"成功加载预制体: {prefab.name}");
                
                // 实例化对象
                var instance = Instantiate(prefab);
                
                // 使用完毕后释放资源
                _assetManager.ReleaseAsset(prefab);
            }
            else
            {
                Debug.LogError("加载预制体失败");
            }
        }

        /// <summary>
        /// 示例：异步加载资源
        /// </summary>
        public async Task ExampleAsyncLoad()
        {
            // 异步加载Texture2D资源
            var texture = await _assetManager.LoadAssetAsync<Texture2D>("MyTexture");
            if (texture != null)
            {
                Debug.Log($"成功异步加载纹理: {texture.name}, 尺寸: {texture.width}x{texture.height}");
                
                // 使用纹理...
                
                // 使用完毕后释放资源
                _assetManager.ReleaseAsset(texture);
            }
            else
            {
                Debug.LogError("异步加载纹理失败");
            }
        }

        /// <summary>
        /// 示例：预加载资源
        /// </summary>
        public async Task ExamplePreload()
        {
            // 预加载多个资源到缓存
            await _assetManager.PreloadAssetAsync("MyPrefab");
            await _assetManager.PreloadAssetAsync("MyTexture");
            await _assetManager.PreloadAssetAsync("MyAudioClip");
            
            Debug.Log("预加载完成");
            
            // 检查缓存状态
            var stats = _assetManager.GetCacheStats();
            Debug.Log($"缓存统计 - 资源数: {stats.CachedAssetCount}, 命中率: {stats.CacheHitRate:P2}");
        }

        /// <summary>
        /// 示例：检查资源缓存状态
        /// </summary>
        public void ExampleCheckCache()
        {
            string[] assetAddresses = { "MyPrefab", "MyTexture", "MyAudioClip" };
            
            foreach (var address in assetAddresses)
            {
                bool isCached = _assetManager.IsAssetCached(address);
                Debug.Log($"资源 {address} 缓存状态: {(isCached ? "已缓存" : "未缓存")}");
            }
        }

        /// <summary>
        /// 示例：批量释放资源
        /// </summary>
        public void ExampleBatchRelease()
        {
            // 获取释放前的统计信息
            var statsBefore = _assetManager.GetCacheStats();
            Debug.Log($"释放前缓存统计 - 资源数: {statsBefore.CachedAssetCount}");
            
            // 清理所有缓存
            _assetManager.ClearCache();
            
            // 获取释放后的统计信息
            var statsAfter = _assetManager.GetCacheStats();
            Debug.Log($"释放后缓存统计 - 资源数: {statsAfter.CachedAssetCount}");
        }

        /// <summary>
        /// 示例：非泛型方式加载资源
        /// </summary>
        public void ExampleNonGenericLoad()
        {
            // 使用Type参数加载资源
            var audioClip = _assetManager.LoadAsset("MyAudioClip", typeof(AudioClip)) as AudioClip;
            if (audioClip != null)
            {
                Debug.Log($"成功加载音频: {audioClip.name}, 时长: {audioClip.length}秒");
                
                // 使用音频...
                
                // 释放资源
                _assetManager.ReleaseAsset("MyAudioClip");
            }
            else
            {
                Debug.LogError("加载音频失败");
            }
        }

        /// <summary>
        /// Unity Start方法 - 演示各种用法
        /// </summary>
        private async void Start()
        {
            // 等待依赖注入完成
            await Task.Delay(100);
            
            if (_assetManager == null)
            {
                Debug.LogError("AssetManager未正确注入，请检查VContainer配置");
                return;
            }
            
            Debug.Log("=== AssetManager使用示例开始 ===");
            
            // 演示预加载
            await ExamplePreload();
            
            // 演示同步加载
            ExampleSyncLoad();
            
            // 演示异步加载
            await ExampleAsyncLoad();
            
            // 演示非泛型加载
            ExampleNonGenericLoad();
            
            // 检查缓存状态
            ExampleCheckCache();
            
            // 演示批量释放
            ExampleBatchRelease();
            
            Debug.Log("=== AssetManager使用示例结束 ===");
        }
    }
}
