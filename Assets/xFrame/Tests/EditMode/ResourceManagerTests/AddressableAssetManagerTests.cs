using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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
        private int _releasedCount;

        [TearDown]
        public void TearDown()
        {
            if (_loadedAsset != null)
            {
                Object.DestroyImmediate(_loadedAsset);
                _loadedAsset = null;
            }

            _releasedCount = 0;
        }

        private GameObject _loadedAsset;

        [UnityTest]
        public IEnumerator LoadAssetAsync_SameAddressConcurrently_ShouldShareInflightTask()
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
            yield return new WaitUntil(() => firstTask.IsCompleted && secondTask.IsCompleted);

            Assert.AreSame(_loadedAsset, firstTask.Result);
            Assert.AreSame(firstTask.Result, secondTask.Result);

            var stats = manager.GetCacheStats();
            Assert.AreEqual(1, stats.CacheMissCount);
            Assert.AreEqual(1, stats.CacheHitCount);
        }

        [UnityTest]
        public IEnumerator LoadAssetAsync_DomainDestroyedBeforeComplete_ShouldReleaseOrphanAndReturnNull()
        {
            var completionSource = new TaskCompletionSource<Object>();
            _loadedAsset = new GameObject("OrphanedPrefab");

            var manager = new AddressableAssetManager(
                8,
                (_, _) => _loadedAsset,
                (_, _) => completionSource.Task,
                _ => _releasedCount++);

            var domain = manager.CreateDomain("ActivityDomain");
            var loadTask = manager.LoadAssetAsync<GameObject>(domain, "UI/Activity");

            manager.DestroyDomain(domain);
            manager.DestroyDomain(domain);

            completionSource.SetResult(_loadedAsset);
            yield return new WaitUntil(() => loadTask.IsCompleted);

            Assert.IsNull(loadTask.Result);
            Assert.AreEqual(1, _releasedCount);
            Assert.IsFalse(manager.IsAssetCached("UI/Activity"));
        }

        [UnityTest]
        public IEnumerator LoadAssetAsync_DomainRenewedBeforeComplete_ShouldDropOldGenerationAndAcceptNewGeneration()
        {
            var loadCount = 0;
            var completionSource = new TaskCompletionSource<Object>();
            _loadedAsset = new GameObject("RenewedPrefab");

            var manager = new AddressableAssetManager(
                8,
                (_, _) => _loadedAsset,
                (_, _) =>
                {
                    loadCount++;
                    return completionSource.Task;
                },
                _ => _releasedCount++);

            var domain = manager.CreateDomain("ActivityDomain");
            var staleTask = manager.LoadAssetAsync<GameObject>(domain, "UI/Activity");

            manager.RenewDomain(domain);
            var currentTask = manager.LoadAssetAsync<GameObject>(domain, "UI/Activity");

            completionSource.SetResult(_loadedAsset);
            yield return new WaitUntil(() => staleTask.IsCompleted && currentTask.IsCompleted);

            Assert.IsNull(staleTask.Result);
            Assert.AreSame(_loadedAsset, currentTask.Result);
            Assert.AreEqual(1, loadCount);
            Assert.AreEqual(0, _releasedCount);
            Assert.IsTrue(manager.IsAssetCached("UI/Activity"));
        }

        [UnityTest]
        public IEnumerator LoadAssetAsync_MultiDomain_ShouldIsolateLifecycle()
        {
            var loadCount = 0;
            var completionSource = new TaskCompletionSource<Object>();
            _loadedAsset = new GameObject("SharedPrefab");

            var manager = new AddressableAssetManager(
                8,
                (_, _) => _loadedAsset,
                (_, _) =>
                {
                    loadCount++;
                    return completionSource.Task;
                },
                _ => _releasedCount++);

            var destroyedDomain = manager.CreateDomain("DestroyedDomain");
            var aliveDomain = manager.CreateDomain("AliveDomain");

            var orphanedTask = manager.LoadAssetAsync<GameObject>(destroyedDomain, "UI/Shared");
            var aliveTask = manager.LoadAssetAsync<GameObject>(aliveDomain, "UI/Shared");

            manager.DestroyDomain(destroyedDomain);
            completionSource.SetResult(_loadedAsset);
            yield return new WaitUntil(() => orphanedTask.IsCompleted && aliveTask.IsCompleted);

            Assert.IsNull(orphanedTask.Result);
            Assert.AreSame(_loadedAsset, aliveTask.Result);
            Assert.AreEqual(1, loadCount);
            Assert.AreEqual(0, _releasedCount);
            Assert.IsTrue(manager.IsAssetCached("UI/Shared"));
        }
    }
}
