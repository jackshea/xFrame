using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using xFrame.Runtime.UI;

namespace xFrame.Tests.PlayMode.UITests
{
    /// <summary>
    /// UIComponent组件基类的PlayMode测试
    /// </summary>
    [TestFixture]
    public class UIComponentTests
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

        #region 组件创建测试

        /// <summary>
        /// 测试组件创建时自动生成ComponentId
        /// </summary>
        [UnityTest]
        public IEnumerator Awake_ShouldGenerateComponentId()
        {
            var go = new GameObject("TestComponent");
            go.transform.SetParent(_testRoot.transform);
            var component = go.AddComponent<TestUIComponent>();

            yield return null;

            Assert.IsNotNull(component.ComponentId, "ComponentId应被生成");
            Assert.IsTrue(component.ComponentId.StartsWith("TestUIComponent_"), "ComponentId应以类名开头");
        }

        /// <summary>
        /// 测试组件创建时自动添加CanvasGroup
        /// </summary>
        [UnityTest]
        public IEnumerator Awake_ShouldAddCanvasGroup()
        {
            var go = new GameObject("TestComponent");
            go.transform.SetParent(_testRoot.transform);
            var component = go.AddComponent<TestUIComponent>();

            yield return null;

            var canvasGroup = go.GetComponent<CanvasGroup>();
            Assert.IsNotNull(canvasGroup, "应自动添加CanvasGroup");
        }

        /// <summary>
        /// 测试组件创建时已有CanvasGroup不重复添加
        /// </summary>
        [UnityTest]
        public IEnumerator Awake_ExistingCanvasGroup_ShouldNotDuplicate()
        {
            var go = new GameObject("TestComponent");
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<CanvasGroup>();
            var component = go.AddComponent<TestUIComponent>();

            yield return null;

            var canvasGroups = go.GetComponents<CanvasGroup>();
            Assert.AreEqual(1, canvasGroups.Length, "不应重复添加CanvasGroup");
        }

        #endregion

        #region 初始化测试

        /// <summary>
        /// 测试组件初始化
        /// </summary>
        [UnityTest]
        public IEnumerator Initialize_ShouldSetIsInitialized()
        {
            var component = CreateTestComponent<TestUIComponent>();
            yield return null;

            Assert.IsFalse(component.IsInitialized, "初始时应未初始化");

            component.Initialize();

            Assert.IsTrue(component.IsInitialized, "初始化后应为true");
        }

        /// <summary>
        /// 测试组件重复初始化
        /// </summary>
        [UnityTest]
        public IEnumerator Initialize_CalledTwice_ShouldOnlyInitializeOnce()
        {
            var component = CreateTestComponent<TestUIComponentWithCounter>();
            yield return null;

            component.Initialize();
            component.Initialize();

            Assert.AreEqual(1, component.InitializeCount, "应只初始化一次");
        }

        #endregion

        #region 显示/隐藏测试

        /// <summary>
        /// 测试组件显示
        /// </summary>
        [UnityTest]
        public IEnumerator Show_ShouldSetIsVisible()
        {
            var component = CreateTestComponent<TestUIComponent>();
            yield return null;

            component.Initialize();
            component.Show();

            Assert.IsTrue(component.IsVisible, "显示后IsVisible应为true");
            Assert.IsTrue(component.gameObject.activeSelf, "GameObject应激活");
        }

        /// <summary>
        /// 测试组件隐藏
        /// </summary>
        [UnityTest]
        public IEnumerator Hide_ShouldSetIsVisibleFalse()
        {
            var component = CreateTestComponent<TestUIComponent>();
            yield return null;

            component.Initialize();
            component.Show();
            component.Hide();

            Assert.IsFalse(component.IsVisible, "隐藏后IsVisible应为false");
            Assert.IsFalse(component.gameObject.activeSelf, "GameObject应禁用");
        }

        /// <summary>
        /// 测试重复显示
        /// </summary>
        [UnityTest]
        public IEnumerator Show_CalledTwice_ShouldOnlyShowOnce()
        {
            var component = CreateTestComponent<TestUIComponentWithCounter>();
            yield return null;

            component.Initialize();
            component.Show();
            component.Show();

            Assert.AreEqual(1, component.ShowCount, "应只显示一次");
        }

        /// <summary>
        /// 测试重复隐藏
        /// </summary>
        [UnityTest]
        public IEnumerator Hide_CalledTwice_ShouldOnlyHideOnce()
        {
            var component = CreateTestComponent<TestUIComponentWithCounter>();
            yield return null;

            component.Initialize();
            component.Show();
            component.Hide();
            component.Hide();

            Assert.AreEqual(1, component.HideCount, "应只隐藏一次");
        }

        /// <summary>
        /// 测试SetVisible方法
        /// </summary>
        [UnityTest]
        public IEnumerator SetVisible_ShouldToggleVisibility()
        {
            var component = CreateTestComponent<TestUIComponent>();
            yield return null;

            component.Initialize();

            component.SetVisible(true);
            Assert.IsTrue(component.IsVisible, "SetVisible(true)应显示组件");

            component.SetVisible(false);
            Assert.IsFalse(component.IsVisible, "SetVisible(false)应隐藏组件");
        }

        #endregion

        #region 数据设置测试

        /// <summary>
        /// 测试设置数据
        /// </summary>
        [UnityTest]
        public IEnumerator SetData_ShouldCallOnSetData()
        {
            var component = CreateTestComponent<TestUIComponentWithData>();
            yield return null;

            component.Initialize();
            component.SetData("TestData");

            Assert.AreEqual("TestData", component.ReceivedData, "数据应被正确传递");
        }

        #endregion

        #region 刷新测试

        /// <summary>
        /// 测试刷新组件
        /// </summary>
        [UnityTest]
        public IEnumerator Refresh_ShouldCallOnRefresh()
        {
            var component = CreateTestComponent<TestUIComponentWithCounter>();
            yield return null;

            component.Initialize();
            component.Refresh();

            Assert.AreEqual(1, component.RefreshCount, "Refresh应被调用");
        }

        #endregion

        #region 重置测试

        /// <summary>
        /// 测试重置组件
        /// </summary>
        [UnityTest]
        public IEnumerator Reset_ShouldResetState()
        {
            var component = CreateTestComponent<TestUIComponent>();
            yield return null;

            component.Initialize();
            component.Show();
            component.Reset();

            Assert.IsFalse(component.IsVisible, "重置后IsVisible应为false");
        }

        #endregion

        #region 销毁测试

        /// <summary>
        /// 测试销毁组件
        /// </summary>
        [UnityTest]
        public IEnumerator DestroyComponent_ShouldResetIsInitialized()
        {
            var component = CreateTestComponent<TestUIComponent>();
            yield return null;

            component.Initialize();
            component.DestroyComponent();

            Assert.IsFalse(component.IsInitialized, "销毁后IsInitialized应为false");
        }

        #endregion

        #region 交互性测试

        /// <summary>
        /// 测试设置交互性
        /// </summary>
        [UnityTest]
        public IEnumerator SetInteractable_ShouldSetCanvasGroupInteractable()
        {
            var component = CreateTestComponent<TestUIComponent>();
            yield return null;

            component.Initialize();

            component.SetInteractable(false);
            var canvasGroup = component.GetComponent<CanvasGroup>();

            Assert.IsFalse(canvasGroup.interactable, "应不可交互");
            Assert.IsFalse(canvasGroup.blocksRaycasts, "应不阻挡射线");

            component.SetInteractable(true);
            Assert.IsTrue(canvasGroup.interactable, "应可交互");
            Assert.IsTrue(canvasGroup.blocksRaycasts, "应阻挡射线");
        }

        #endregion

        #region 泛型组件测试

        /// <summary>
        /// 测试泛型组件设置数据
        /// </summary>
        [UnityTest]
        public IEnumerator GenericComponent_SetData_ShouldSetCurrentData()
        {
            var component = CreateTestComponent<TestGenericUIComponent>();
            yield return null;

            component.Initialize();
            var testData = new TestData { Name = "Test", Value = 42 };
            component.SetData(testData);

            Assert.AreEqual(testData, component.GetData(), "数据应被正确设置");
        }

        /// <summary>
        /// 测试泛型组件HasData
        /// </summary>
        [UnityTest]
        public IEnumerator GenericComponent_HasData_ShouldReturnCorrectly()
        {
            var component = CreateTestComponent<TestGenericUIComponent>();
            yield return null;

            component.Initialize();

            Assert.IsFalse(component.HasData(), "初始时应无数据");

            component.SetData(new TestData());
            Assert.IsTrue(component.HasData(), "设置数据后应有数据");
        }

        /// <summary>
        /// 测试泛型组件重置时清空数据
        /// </summary>
        [UnityTest]
        public IEnumerator GenericComponent_Reset_ShouldClearData()
        {
            var component = CreateTestComponent<TestGenericUIComponent>();
            yield return null;

            component.Initialize();
            component.SetData(new TestData());
            component.Reset();

            Assert.IsFalse(component.HasData(), "重置后应无数据");
        }

        /// <summary>
        /// 测试泛型组件设置错误类型数据
        /// </summary>
        [UnityTest]
        public IEnumerator GenericComponent_SetData_WrongType_ShouldLogWarning()
        {
            var component = CreateTestComponent<TestGenericUIComponent>();
            yield return null;

            component.Initialize();

            // 通过基类方法设置错误类型数据
            ((UIComponent)component).SetData("wrong type");

            Assert.IsFalse(component.HasData(), "错误类型数据不应被设置");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建测试组件
        /// </summary>
        private T CreateTestComponent<T>() where T : UIComponent
        {
            var go = new GameObject($"TestComponent_{typeof(T).Name}");
            go.transform.SetParent(_testRoot.transform);
            return go.AddComponent<T>();
        }

        #endregion

        #region 测试辅助类

        /// <summary>
        /// 基础测试组件
        /// </summary>
        private class TestUIComponent : UIComponent
        {
        }

        /// <summary>
        /// 带计数器的测试组件
        /// </summary>
        private class TestUIComponentWithCounter : UIComponent
        {
            public int InitializeCount { get; private set; }
            public int ShowCount { get; private set; }
            public int HideCount { get; private set; }
            public int RefreshCount { get; private set; }

            protected override void OnInitialize()
            {
                base.OnInitialize();
                InitializeCount++;
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

            protected override void OnRefresh()
            {
                base.OnRefresh();
                RefreshCount++;
            }
        }

        /// <summary>
        /// 带数据的测试组件
        /// </summary>
        private class TestUIComponentWithData : UIComponent
        {
            public object ReceivedData { get; private set; }

            protected override void OnSetData(object data)
            {
                base.OnSetData(data);
                ReceivedData = data;
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
        /// 泛型测试组件
        /// </summary>
        private class TestGenericUIComponent : UIComponent<TestData>
        {
        }

        #endregion
    }
}
