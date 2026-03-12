using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace xFrame.Tests
{
    /// <summary>
    ///     启动入口单例回归测试。
    /// </summary>
    [TestFixture]
    public class BootstrapperSingletonTests
    {
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            SetStaticInstance(typeof(xFrame.Runtime.xFrameBootstrapper), "_instance", null);
            SetStaticInstance(typeof(xFrame.Runtime.xFrameApplication), "_instance", null);

            foreach (var gameObject in Object.FindObjectsOfType<GameObject>())
                if (gameObject.name.StartsWith("Test", System.StringComparison.Ordinal))
                    Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Bootstrapper_Instance_ShouldReturnBackingField()
        {
            var gameObject = new GameObject("TestBootstrapper");
            gameObject.SetActive(false);

            var bootstrapper = gameObject.AddComponent<xFrame.Runtime.xFrameBootstrapper>();
            SetStaticInstance(typeof(xFrame.Runtime.xFrameBootstrapper), "_instance", bootstrapper);

            Assert.AreSame(bootstrapper, xFrame.Runtime.xFrameBootstrapper.Instance);
        }

        [Test]
        public void Application_Instance_ShouldReturnBackingField()
        {
            var gameObject = new GameObject("TestApplication");
            gameObject.SetActive(false);

            var application = gameObject.AddComponent<xFrame.Runtime.xFrameApplication>();
            SetStaticInstance(typeof(xFrame.Runtime.xFrameApplication), "_instance", application);

            Assert.AreSame(application, xFrame.Runtime.xFrameApplication.Instance);
        }

        private static void SetStaticInstance(System.Type type, string fieldName, Object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            field.SetValue(null, value);
        }
    }
}
