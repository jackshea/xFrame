using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using xFrame.Runtime.ResourceManager;

namespace xFrame.Tests
{
    /// <summary>
    ///     AddressableAssetManager 回归测试。
    /// </summary>
    [TestFixture]
    public class AddressableAssetManagerTests
    {
        [TearDown]
        public void TearDown()
        {
            if (_loadedAsset != null)
            {
                Object.DestroyImmediate(_loadedAsset);
                _loadedAsset = null;
            }
        }

        private GameObject _loadedAsset;

        [Test]
        public void LoadAssetAsync_SameAddressConcurrently_ShouldShareInflightTask()
        {
            var loadCount = 0;
            var completionSource = new TaskCompletionSource<Object>();
            _loadedAsset = new GameObject("LoadedPrefab");

            var manager = new AddressableAssetManager(
                8,
                (_, _) => _loadedAsset,
                (_, _) =>
                {
                    loadCount++;
                    return completionSource.Task;
                });

            var firstTask = manager.LoadAssetAsync<GameObject>("UI/TestPrefab");
            var secondTask = manager.LoadAssetAsync<GameObject>("UI/TestPrefab");

            Assert.AreEqual(1, loadCount);

            completionSource.SetResult(_loadedAsset);
            Task.WhenAll(firstTask, secondTask).GetAwaiter().GetResult();

            Assert.AreSame(_loadedAsset, firstTask.GetAwaiter().GetResult());
            Assert.AreSame(firstTask.GetAwaiter().GetResult(), secondTask.GetAwaiter().GetResult());

            var stats = manager.GetCacheStats();
            Assert.AreEqual(1, stats.CacheMissCount);
            Assert.AreEqual(1, stats.CacheHitCount);
        }
    }
}
