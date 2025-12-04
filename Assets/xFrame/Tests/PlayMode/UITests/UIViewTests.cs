using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using xFrame.Runtime.UI;

namespace xFrame.Tests.PlayMode.UITests
{
    /// <summary>
    /// UIView视图基类的PlayMode测试
    /// 注意：由于Internal*方法是internal的，这里只测试公开的行为
    /// </summary>
    [TestFixture]
    public class UIViewTests
    {
        private GameObject _testRoot;

        [SetUp]
        public void SetUp()
        {
            _testRoot = new GameObject("TestRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (_testRoot != null)
            {
                Object.Destroy(_testRoot);
            }
        }

        #region 组件初始化测试

        /// <summary>
        /// 测试UIView创建时自动添加CanvasGroup
        /// </summary>
        [UnityTest]
        public IEnumerator Awake_ShouldAddCanvasGroup()
        {
            var view = CreateTestView<TestUIView>();
            yield return null;

            Assert.IsNotNull(view.CanvasGroup, "应自动添加CanvasGroup");
        }

        /// <summary>
        /// 测试UIView创建时初始化ComponentManager
        /// </summary>
        [UnityTest]
        public IEnumerator Awake_ShouldInitializeComponentManager()
        {
            var view = CreateTestView<TestUIView>();
            yield return null;

            Assert.IsNotNull(view.ComponentManager, "ComponentManager应被初始化");
        }

        /// <summary>
        /// 测试UIView创建时获取RectTransform
        /// </summary>
        [UnityTest]
        public IEnumerator Awake_ShouldGetRectTransform()
        {
            var go = new GameObject("TestView");
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<RectTransform>();
            var view = go.AddComponent<TestUIView>();

            yield return null;

            Assert.IsNotNull(view.RectTransform, "RectTransform应被获取");
        }

        #endregion

        #region 初始状态测试

        /// <summary>
        /// 测试初始IsCreated状态
        /// </summary>
        [UnityTest]
        public IEnumerator IsCreated_Initial_ShouldBeFalse()
        {
            var view = CreateTestView<TestUIView>();
            yield return null;

            Assert.IsFalse(view.IsCreated, "初始IsCreated应为false");
        }

        /// <summary>
        /// 测试初始IsOpen状态
        /// </summary>
        [UnityTest]
        public IEnumerator IsOpen_Initial_ShouldBeFalse()
        {
            var view = CreateTestView<TestUIView>();
            yield return null;

            Assert.IsFalse(view.IsOpen, "初始IsOpen应为false");
        }

        #endregion

        #region 辅助方法测试

        /// <summary>
        /// 测试SetVisible方法
        /// </summary>
        [UnityTest]
        public IEnumerator SetVisible_ShouldSetCanvasGroupAlpha()
        {
            var view = CreateTestView<TestUIView>();
            yield return null;

            view.SetVisible(false);
            Assert.AreEqual(0f, view.CanvasGroup.alpha, "alpha应为0");

            view.SetVisible(true);
            Assert.AreEqual(1f, view.CanvasGroup.alpha, "alpha应为1");
        }

        /// <summary>
        /// 测试SetInteractable方法
        /// </summary>
        [UnityTest]
        public IEnumerator SetInteractable_ShouldSetCanvasGroupInteractable()
        {
            var view = CreateTestView<TestUIView>();
            yield return null;

            view.SetInteractable(false);
            Assert.IsFalse(view.CanvasGroup.interactable, "应不可交互");
            Assert.IsFalse(view.CanvasGroup.blocksRaycasts, "应不阻挡射线");

            view.SetInteractable(true);
            Assert.IsTrue(view.CanvasGroup.interactable, "应可交互");
            Assert.IsTrue(view.CanvasGroup.blocksRaycasts, "应阻挡射线");
        }

        #endregion

        #region IPoolable接口测试

        /// <summary>
        /// 测试OnGet激活GameObject
        /// </summary>
        [UnityTest]
        public IEnumerator OnGet_ShouldActivateGameObject()
        {
            var view = CreateTestView<TestUIView>();
            yield return null;

            view.gameObject.SetActive(false);
            view.OnGet();

            Assert.IsTrue(view.gameObject.activeSelf, "GameObject应被激活");
        }

        /// <summary>
        /// 测试OnRelease禁用GameObject
        /// </summary>
        [UnityTest]
        public IEnumerator OnRelease_ShouldDeactivateGameObject()
        {
            var view = CreateTestView<TestUIView>();
            yield return null;

            view.OnRelease();

            Assert.IsFalse(view.gameObject.activeSelf, "GameObject应被禁用");
        }

        #endregion

        #region 默认属性测试

        /// <summary>
        /// 测试默认Layer
        /// </summary>
        [UnityTest]
        public IEnumerator Layer_Default_ShouldBeNormal()
        {
            var view = CreateTestView<TestUIView>();
            yield return null;

            Assert.AreEqual(UILayer.Normal, view.Layer, "默认Layer应为Normal");
        }

        /// <summary>
        /// 测试默认Cacheable
        /// </summary>
        [UnityTest]
        public IEnumerator Cacheable_Default_ShouldBeTrue()
        {
            var view = CreateTestView<TestUIView>();
            yield return null;

            Assert.IsTrue(view.Cacheable, "默认Cacheable应为true");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建测试视图
        /// </summary>
        private T CreateTestView<T>() where T : UIView
        {
            var go = new GameObject($"TestView_{typeof(T).Name}");
            go.transform.SetParent(_testRoot.transform);
            return go.AddComponent<T>();
        }

        #endregion

        #region 测试辅助类

        /// <summary>
        /// 基础测试视图
        /// </summary>
        private class TestUIView : UIView
        {
        }

        /// <summary>
        /// 带计数器的测试视图
        /// </summary>
        private class TestUIViewWithCounter : UIView
        {
            public int CreateCount { get; private set; }
            public int OpenCount { get; private set; }
            public int ShowCount { get; private set; }
            public int HideCount { get; private set; }
            public int CloseCount { get; private set; }
            public int DestroyCount { get; private set; }

            protected override void OnCreate()
            {
                base.OnCreate();
                CreateCount++;
            }

            protected override void OnOpen(object data)
            {
                base.OnOpen(data);
                OpenCount++;
            }

            protected override void OnShow()
            {
                base.OnShow();
                ShowCount++;
            }

            protected override void OnHide()
            {
                base.OnHide();
                HideCount++;
            }

            protected override void OnClose()
            {
                base.OnClose();
                CloseCount++;
            }

            protected override void OnUIDestroy()
            {
                base.OnUIDestroy();
                DestroyCount++;
            }
        }

        /// <summary>
        /// 测试数据类
        /// </summary>
        private class TestData
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }

        /// <summary>
        /// 带数据的测试视图
        /// </summary>
        private class TestUIViewWithData : UIView
        {
            public object ReceivedData { get; private set; }

            protected override void OnOpen(object data)
            {
                base.OnOpen(data);
                ReceivedData = data;
            }
        }

        #endregion
    }
}
