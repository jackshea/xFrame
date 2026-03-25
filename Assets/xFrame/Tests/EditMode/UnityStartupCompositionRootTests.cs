using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer.Unity;
using xFrame.Runtime.DI;
using xFrame.Runtime.Startup;
using xFrame.Runtime.Unity.Startup;
using Object = UnityEngine.Object;

namespace xFrame.Tests
{
    /// <summary>
    ///     Unity 启动装配根回归测试。
    /// </summary>
    [TestFixture]
    public class UnityStartupCompositionRootTests
    {
        [SetUp]
        public void SetUp()
        {
            CleanupSceneObjects();
        }

        [TearDown]
        public void TearDown()
        {
            CleanupSceneObjects();
        }

        private static void CleanupSceneObjects()
        {
            foreach (var scope in Resources.FindObjectsOfTypeAll<LifetimeScope>())
                if (scope != null && scope.gameObject.scene.IsValid())
                    Object.DestroyImmediate(scope.gameObject);

            foreach (var eventSystem in Resources.FindObjectsOfTypeAll<EventSystem>())
                if (eventSystem != null && eventSystem.gameObject.scene.IsValid())
                    Object.DestroyImmediate(eventSystem.gameObject);

            foreach (var transform in Resources.FindObjectsOfTypeAll<Transform>())
                if (transform != null &&
                    transform.gameObject.scene.IsValid() &&
                    transform.name.StartsWith("Test"))
                    Object.DestroyImmediate(transform.gameObject);
        }

        [Test]
        public void EnsureInitialized_WithoutExistingScope_ShouldCreateDefaultScope()
        {
            var rootObject = new GameObject("TestStartupRoot");
            var compositionRoot = new UnityStartupCompositionRoot(rootObject.transform, null, false);

            compositionRoot.EnsureInitialized();

            Assert.IsNotNull(compositionRoot.LifetimeScope);
            Assert.IsInstanceOf<xFrameLifetimeScope>(compositionRoot.LifetimeScope);
            Assert.AreEqual(nameof(xFrameLifetimeScope), compositionRoot.LifetimeScope.gameObject.name);
            Assert.IsNotNull(Object.FindObjectOfType<EventSystem>());
        }

        [Test]
        public void EnsureInitialized_WithExistingScope_ShouldReuseExistingScope()
        {
            var scopeObject = new GameObject("ExistingScope");
            var existingScope = scopeObject.AddComponent<xFrameLifetimeScope>();

            var rootObject = new GameObject("TestStartupRoot");
            var compositionRoot = new UnityStartupCompositionRoot(rootObject.transform, null, false);

            compositionRoot.EnsureInitialized();

            Assert.AreSame(existingScope, compositionRoot.LifetimeScope);
            Assert.AreEqual(1, Object.FindObjectsOfType<LifetimeScope>().Length);
        }

        [Test]
        public void EnsureInitialized_WithMultipleExistingScopes_ShouldThrowInvalidOperationException()
        {
            var firstScopeObject = new GameObject("ExistingScopeA");
            firstScopeObject.AddComponent<xFrameLifetimeScope>();

            var secondScopeObject = new GameObject("ExistingScopeB");
            secondScopeObject.AddComponent<xFrameLifetimeScope>();

            var rootObject = new GameObject("TestStartupRoot");
            var compositionRoot = new UnityStartupCompositionRoot(rootObject.transform, null, false);

            var exception = Assert.Throws<InvalidOperationException>(() => compositionRoot.EnsureInitialized());
            StringAssert.Contains("多个 LifetimeScope", exception.Message);
        }

        [Test]
        public void Resolve_WithLocalService_ShouldReturnRegisteredInstance()
        {
            var rootObject = new GameObject("TestStartupRoot");
            var viewObject = new GameObject("TestStartupView");
            var startupView = viewObject.AddComponent<UnityStartupView>();
            var compositionRoot = new UnityStartupCompositionRoot(rootObject.transform, null, false);
            compositionRoot.RegisterLocalService<IStartupErrorPresentationService>(startupView);

            compositionRoot.EnsureInitialized();

            Assert.IsTrue(compositionRoot.TryResolve(out IStartupErrorPresentationService presentationService));
            Assert.AreSame(startupView, presentationService);
        }
    }
}
