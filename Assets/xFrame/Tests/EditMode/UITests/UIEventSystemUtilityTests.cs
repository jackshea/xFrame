using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
using xFrame.Runtime.UI;

namespace xFrame.Tests
{
    /// <summary>
    ///     UIEventSystemUtility 回归测试。
    /// </summary>
    [TestFixture]
    public class UIEventSystemUtilityTests
    {
        private GameObject _parent;

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject("UIRoot");
        }

        [TearDown]
        public void TearDown()
        {
            var eventSystem = Object.FindObjectOfType<EventSystem>();
            if (eventSystem != null) Object.DestroyImmediate(eventSystem.gameObject);

            if (_parent != null) Object.DestroyImmediate(_parent);
        }

        [Test]
        public void EnsureEventSystem_WithoutExistingEventSystem_ShouldCreateEventSystemWithStandaloneInputModule()
        {
            var eventSystem = UIEventSystemUtility.EnsureEventSystem(_parent.transform);

            Assert.IsNotNull(eventSystem, "应创建EventSystem");
            Assert.AreEqual(_parent.transform, eventSystem.transform.parent, "应挂载到指定父节点");
            Assert.IsNotNull(eventSystem.GetComponent<StandaloneInputModule>(), "应创建StandaloneInputModule");
        }

        [Test]
        public void EnsureEventSystem_ExistingEventSystemWithoutInputModule_ShouldAddStandaloneInputModule()
        {
            var eventSystemGO = new GameObject("EventSystem");
            var eventSystem = eventSystemGO.AddComponent<EventSystem>();

            var ensuredEventSystem = UIEventSystemUtility.EnsureEventSystem();

            Assert.AreSame(eventSystem, ensuredEventSystem, "应复用现有EventSystem");
            Assert.IsNotNull(eventSystem.GetComponent<StandaloneInputModule>(), "应补充StandaloneInputModule");
        }
    }
}
