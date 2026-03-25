using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using xFrame.Runtime.ResourceManager;
using xFrame.Runtime.UI;

namespace xFrame.Tests
{
    /// <summary>
    ///     UIManager 资源生命周期回归测试。
    /// </summary>
    [TestFixture]
    public class UIManagerResourceLifecycleTests
    {
        [SetUp]
        public void SetUp()
        {
            CleanupSceneObjects();
            ResetUIManagerSingleton();
            _managerGameObject = new GameObject("TestUIManager");
            _manager = _managerGameObject.AddComponent<UIManager>();
            _assetManager = new RecordingAssetManager();
            SetPrivateField(_manager, "_assetManager", _assetManager);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            CleanupSceneObjects();
            ResetUIManagerSingleton();
            if (_managerGameObject != null) Object.DestroyImmediate(_managerGameObject);
            if (_viewGameObject != null) Object.DestroyImmediate(_viewGameObject);
            if (_prefabGameObject != null) Object.DestroyImmediate(_prefabGameObject);
        }

        private RecordingAssetManager _assetManager;
        private UIManager _manager;
        private GameObject _managerGameObject;
        private GameObject _prefabGameObject;
        private GameObject _viewGameObject;

        [Test]
        public void Close_NonCacheableView_ShouldReleasePrefabAddress()
        {
            _viewGameObject = new GameObject("TestNonCacheableView");
            _viewGameObject.AddComponent<CanvasGroup>();
            var view = _viewGameObject.AddComponent<TestNonCacheableView>();

            SetUIViewState(view, isCreated: true, isOpen: true);

            var openedUIs = GetPrivateField<Dictionary<Type, UIView>>(_manager, "_openedUIs");
            openedUIs[typeof(TestNonCacheableView)] = view;

            _manager.Close(typeof(TestNonCacheableView));

            CollectionAssert.Contains(_assetManager.ReleasedAddresses, "UI/TestNonCacheableView");
        }

        [Test]
        public void ClearAllUIResources_ShouldReleasePreloadedPrefabsAndClearCache()
        {
            _prefabGameObject = new GameObject("TestPreloadedPrefab");

            var preloadedPrefabs = GetPrivateField<Dictionary<Type, GameObject>>(_manager, "_preloadedPrefabs");
            preloadedPrefabs[typeof(TestNonCacheableView)] = _prefabGameObject;

            _manager.ClearAllUIResources();

            CollectionAssert.Contains(_assetManager.ReleasedAddresses, "UI/TestNonCacheableView");
            Assert.AreEqual(1, _assetManager.ClearCacheCallCount);
        }

        [UnityTest]
        [Ignore("EditMode 下 UIManager 生命周期触发时机不稳定；EventSystem 创建行为已由 UIEventSystemUtilityTests 覆盖。")]
        public IEnumerator Awake_WithoutEventSystem_ShouldCreateEventSystemWithStandaloneInputModule()
        {
            var eventSystem = UIEventSystemUtility.EnsureEventSystem(_manager.transform);
            yield return null;

            Assert.IsNotNull(eventSystem, "应自动创建EventSystem");
            Assert.IsNotNull(eventSystem.GetComponent<StandaloneInputModule>(), "应自动添加StandaloneInputModule");
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static void SetUIViewState(UIView view, bool isCreated, bool isOpen)
        {
            var createdField = typeof(UIView).GetField("<IsCreated>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var openField = typeof(UIView).GetField("<IsOpen>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);

            createdField?.SetValue(view, isCreated);
            openField?.SetValue(view, isOpen);
        }

        private static void InvokeLifecycleMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(target, null);
        }

        private static void ResetUIManagerSingleton()
        {
            var field = typeof(UIManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, null);
        }

        private static void CleanupSceneObjects()
        {
            foreach (var uiManager in Resources.FindObjectsOfTypeAll<UIManager>())
                if (uiManager != null && uiManager.gameObject.scene.IsValid())
                    Object.DestroyImmediate(uiManager.gameObject);

            foreach (var eventSystem in Resources.FindObjectsOfTypeAll<EventSystem>())
                if (eventSystem != null && eventSystem.gameObject.scene.IsValid())
                    Object.DestroyImmediate(eventSystem.gameObject);
        }

        private sealed class RecordingAssetManager : IAssetManager
        {
            public int ClearCacheCallCount { get; private set; }

            public List<string> ReleasedAddresses { get; } = new();

            public T LoadAsset<T>(string address) where T : Object
            {
                return null;
            }

            public Task<T> LoadAssetAsync<T>(string address) where T : Object
            {
                return Task.FromResult<T>(null);
            }

            public Object LoadAsset(string address, Type type)
            {
                return null;
            }

            public Task<Object> LoadAssetAsync(string address, Type type)
            {
                return Task.FromResult<Object>(null);
            }

            public void ReleaseAsset(Object asset)
            {
            }

            public void ReleaseAsset(string address)
            {
                ReleasedAddresses.Add(address);
            }

            public Task PreloadAssetAsync(string address)
            {
                return Task.CompletedTask;
            }

            public bool IsAssetCached(string address)
            {
                return false;
            }

            public void ClearCache()
            {
                ClearCacheCallCount++;
            }

            public AssetCacheStats GetCacheStats()
            {
                return default;
            }
        }

        private sealed class TestNonCacheableView : UIView
        {
            public override bool Cacheable => false;
        }
    }
}
