using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using xFrame.Runtime;
using xFrame.Runtime.Unity.Startup;
using Object = UnityEngine.Object;

namespace xFrame.Tests
{
    /// <summary>
    ///     旧启动入口兼容告警回归测试。
    /// </summary>
    [TestFixture]
    public class LegacyStartupCompatibilityTests
    {
        [SetUp]
        public void SetUp()
        {
            CleanupTestObjects();
        }

        /// <summary>
        ///     清理测试对象与旧入口单例状态。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            CleanupTestObjects();
        }

        private static void CleanupTestObjects()
        {
            SetStaticInstance(typeof(xFrameBootstrapper), "_instance", null);
            SetStaticInstance(typeof(xFrameApplication), "_instance", null);

            foreach (var scope in Resources.FindObjectsOfTypeAll<VContainer.Unity.LifetimeScope>())
                if (scope != null && scope.gameObject.scene.IsValid())
                    Object.DestroyImmediate(scope.gameObject);

            foreach (var gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
                if (gameObject != null &&
                    gameObject.scene.IsValid() &&
                    gameObject.name.StartsWith("Test", System.StringComparison.Ordinal))
                    Object.DestroyImmediate(gameObject);
        }

        /// <summary>
        ///     验证存在 UnityStartupEntry 时兼容层检测会返回真。
        /// </summary>
        [Test]
        public void HasModernStartupEntryInLoadedScenes_WithUnityStartupEntry_ShouldReturnTrue()
        {
            var root = new GameObject("TestModernStartupRoot");
            root.SetActive(false);
            root.AddComponent<UnityStartupEntry>();

            Assert.IsTrue(LegacyStartupCompatibility.HasModernStartupEntryInLoadedScenes());
        }

        /// <summary>
        ///     验证旧启动器在检测到新入口共存时会输出统一告警。
        /// </summary>
        [Test]
        public void Bootstrapper_AwakeWithModernStartupEntry_ShouldLogLegacyWarning()
        {
            var modernRoot = new GameObject("TestModernStartupRoot");
            modernRoot.SetActive(false);
            modernRoot.AddComponent<UnityStartupEntry>();

            var bootstrapperRoot = new GameObject("TestLegacyBootstrapperRoot");
            bootstrapperRoot.SetActive(false);
            var bootstrapper = bootstrapperRoot.AddComponent<xFrameBootstrapper>();
            SetPrivateField(bootstrapper, "autoInitialize", false);
            SetPrivateField(bootstrapper, "dontDestroyOnLoad", false);

            LogAssert.Expect(
                LogType.Warning,
                LegacyStartupCompatibility.CreateLegacyEntryWarning(nameof(xFrameBootstrapper)));

            InvokeLifecycleMethod(bootstrapper, "Awake");

            Assert.AreSame(bootstrapper, xFrameBootstrapper.Instance);
        }

        /// <summary>
        ///     验证新入口在检测到旧入口共存时会输出统一告警。
        /// </summary>
        [Test]
        public void UnityStartupEntry_AwakeWithLegacyBootstrapper_ShouldLogModernWarning()
        {
            var legacyRoot = new GameObject("TestLegacyBootstrapperRoot");
            legacyRoot.SetActive(false);
            legacyRoot.AddComponent<xFrameBootstrapper>();

            var modernRoot = new GameObject("TestModernStartupRoot");
            modernRoot.SetActive(false);
            var modernEntry = modernRoot.AddComponent<UnityStartupEntry>();

            LogAssert.Expect(
                LogType.Warning,
                LegacyStartupCompatibility.CreateModernEntryWarning(nameof(UnityStartupEntry), nameof(xFrameBootstrapper)));

            InvokeLifecycleMethod(modernEntry, "Awake");
        }

        private static void SetStaticInstance(System.Type type, string fieldName, Object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            field.SetValue(null, value);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static void InvokeLifecycleMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, null);
        }
    }
}
