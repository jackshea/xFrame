using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using xFrame.Runtime.UI;

namespace xFrame.Tests.PlayMode.UITests
{
    /// <summary>
    /// UITabContainer标签页容器的PlayMode测试
    /// </summary>
    [TestFixture]
    public class UITabContainerTests
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

        #region 页面管理测试

        /// <summary>
        /// 测试添加页面
        /// </summary>
        [UnityTest]
        public IEnumerator AddPage_ShouldAddPageToContainer()
        {
            var container = CreateTestContainer();
            var page = CreateTestPage<TestTabPage1>();
            yield return null;

            var index = container.AddPage(page);

            Assert.AreEqual(0, index, "第一个页面索引应为0");
            Assert.AreEqual(1, container.PageCount, "页面数量应为1");
        }

        /// <summary>
        /// 测试添加多个页面
        /// </summary>
        [UnityTest]
        public IEnumerator AddPage_MultiplePages_ShouldAddAll()
        {
            var container = CreateTestContainer();
            var page1 = CreateTestPage<TestTabPage1>();
            var page2 = CreateTestPage<TestTabPage2>();
            var page3 = CreateTestPage<TestTabPage1>();
            yield return null;

            container.AddPage(page1);
            container.AddPage(page2);
            container.AddPage(page3);

            Assert.AreEqual(3, container.PageCount, "页面数量应为3");
        }

        /// <summary>
        /// 测试添加null页面
        /// </summary>
        [UnityTest]
        public IEnumerator AddPage_NullPage_ShouldReturnNegativeOne()
        {
            var container = CreateTestContainer();
            yield return null;

            LogAssert.Expect(LogType.Error, "[UITabContainer] 添加页面失败：页面为空");
            var index = container.AddPage<TestTabPage1>(null);

            Assert.AreEqual(-1, index, "添加null页面应返回-1");
            Assert.AreEqual(0, container.PageCount, "页面数量应为0");
        }

        /// <summary>
        /// 测试添加页面后页面被初始化
        /// </summary>
        [UnityTest]
        public IEnumerator AddPage_ShouldInitializePage()
        {
            var container = CreateTestContainer();
            var page = CreateTestPage<TestTabPage1>();
            yield return null;

            container.AddPage(page);

            Assert.IsTrue(page.IsCreated, "页面应被创建");
            Assert.AreEqual(0, page.PageIndex, "页面索引应为0");
        }

        /// <summary>
        /// 测试移除页面
        /// </summary>
        [UnityTest]
        public IEnumerator RemovePage_ShouldRemoveFromContainer()
        {
            var container = CreateTestContainer();
            var page1 = CreateTestPage<TestTabPage1>();
            var page2 = CreateTestPage<TestTabPage2>();
            yield return null;

            container.AddPage(page1);
            container.AddPage(page2);
            container.RemovePage(0);

            Assert.AreEqual(1, container.PageCount, "页面数量应为1");
            // 验证page1不再是容器中的页面
            Assert.IsNull(container.GetPage<TestTabPage1>(), "被移除的页面不应在容器中");
        }

        /// <summary>
        /// 测试移除页面后索引更新
        /// </summary>
        [UnityTest]
        public IEnumerator RemovePage_ShouldUpdateIndices()
        {
            var container = CreateTestContainer();
            var page1 = CreateTestPage<TestTabPage1>();
            var page2 = CreateTestPage<TestTabPage2>();
            var page3 = CreateTestPage<TestTabPage1>();
            yield return null;

            container.AddPage(page1);
            container.AddPage(page2);
            container.AddPage(page3);
            container.RemovePage(0);

            Assert.AreEqual(0, page2.PageIndex, "page2索引应更新为0");
            Assert.AreEqual(1, page3.PageIndex, "page3索引应更新为1");
        }

        #endregion

        #region 页面获取测试

        /// <summary>
        /// 测试通过索引获取页面
        /// </summary>
        [UnityTest]
        public IEnumerator GetPage_ByIndex_ShouldReturnCorrectPage()
        {
            var container = CreateTestContainer();
            var page1 = CreateTestPage<TestTabPage1>();
            var page2 = CreateTestPage<TestTabPage2>();
            yield return null;

            container.AddPage(page1);
            container.AddPage(page2);

            Assert.AreEqual(page1, container.GetPage(0), "索引0应返回page1");
            Assert.AreEqual(page2, container.GetPage(1), "索引1应返回page2");
        }

        /// <summary>
        /// 测试通过无效索引获取页面
        /// </summary>
        [UnityTest]
        public IEnumerator GetPage_InvalidIndex_ShouldReturnNull()
        {
            var container = CreateTestContainer();
            var page = CreateTestPage<TestTabPage1>();
            yield return null;

            container.AddPage(page);

            Assert.IsNull(container.GetPage(-1), "负索引应返回null");
            Assert.IsNull(container.GetPage(10), "超出范围的索引应返回null");
        }

        /// <summary>
        /// 测试通过类型获取页面
        /// </summary>
        [UnityTest]
        public IEnumerator GetPage_ByType_ShouldReturnCorrectPage()
        {
            var container = CreateTestContainer();
            var page1 = CreateTestPage<TestTabPage1>();
            var page2 = CreateTestPage<TestTabPage2>();
            yield return null;

            container.AddPage(page1);
            container.AddPage(page2);

            var result = container.GetPage<TestTabPage2>();
            Assert.AreEqual(page2, result, "应返回正确类型的页面");
        }

        /// <summary>
        /// 测试通过名称获取页面
        /// </summary>
        [UnityTest]
        public IEnumerator GetPage_ByName_ShouldReturnCorrectPage()
        {
            var container = CreateTestContainer();
            var page = CreateTestPage<TestTabPage1>();
            yield return null;

            container.AddPage(page);

            var result = container.GetPage("TestTabPage1");
            Assert.AreEqual(page, result, "应返回正确名称的页面");
        }

        /// <summary>
        /// 测试获取所有页面
        /// </summary>
        [UnityTest]
        public IEnumerator GetAllPages_ShouldReturnAllPages()
        {
            var container = CreateTestContainer();
            var page1 = CreateTestPage<TestTabPage1>();
            var page2 = CreateTestPage<TestTabPage2>();
            yield return null;

            container.AddPage(page1);
            container.AddPage(page2);

            var pages = container.GetAllPages();
            Assert.AreEqual(2, pages.Count, "应返回所有页面");
            Assert.Contains(page1, pages);
            Assert.Contains(page2, pages);
        }

        #endregion

        #region 页面切换测试

        /// <summary>
        /// 测试切换页面
        /// </summary>
        [UnityTest]
        public IEnumerator SwitchPage_ByIndex_ShouldSwitchToPage()
        {
            var container = CreateTestContainer();
            var page1 = CreateTestPage<TestTabPage1>();
            var page2 = CreateTestPage<TestTabPage2>();
            yield return null;

            container.AddPage(page1);
            container.AddPage(page2);
            container.SwitchPage(1);

            Assert.AreEqual(1, container.CurrentPageIndex, "当前页面索引应为1");
            Assert.AreEqual(page2, container.CurrentPage, "当前页面应为page2");
        }

        /// <summary>
        /// 测试切换到同一页面
        /// </summary>
        [UnityTest]
        public IEnumerator SwitchPage_SamePage_ShouldNotSwitch()
        {
            var container = CreateTestContainer();
            var page = CreateTestPage<TestTabPageWithCounter>();
            yield return null;

            container.AddPage(page);
            container.SwitchPage(0);
            var enterCount = page.EnterCount;
            container.SwitchPage(0);

            Assert.AreEqual(enterCount, page.EnterCount, "切换到同一页面不应触发OnPageEnter");
        }

        /// <summary>
        /// 测试切换页面触发事件
        /// </summary>
        [UnityTest]
        public IEnumerator SwitchPage_ShouldTriggerOnPageChanged()
        {
            var container = CreateTestContainer();
            var page1 = CreateTestPage<TestTabPage1>();
            var page2 = CreateTestPage<TestTabPage2>();
            yield return null;

            container.AddPage(page1);
            container.AddPage(page2);

            var oldIndex = -1;
            var newIndex = -1;
            container.OnPageChanged += (old, @new) =>
            {
                oldIndex = old;
                newIndex = @new;
            };

            container.SwitchPage(0);
            container.SwitchPage(1);

            Assert.AreEqual(0, oldIndex, "旧索引应为0");
            Assert.AreEqual(1, newIndex, "新索引应为1");
        }

        /// <summary>
        /// 测试通过类型切换页面
        /// </summary>
        [UnityTest]
        public IEnumerator SwitchPage_ByType_ShouldSwitchToPage()
        {
            var container = CreateTestContainer();
            var page1 = CreateTestPage<TestTabPage1>();
            var page2 = CreateTestPage<TestTabPage2>();
            yield return null;

            container.AddPage(page1);
            container.AddPage(page2);
            container.SwitchPage<TestTabPage2>();

            Assert.AreEqual(page2, container.CurrentPage, "应切换到TestTabPage2");
        }

        /// <summary>
        /// 测试通过名称切换页面
        /// </summary>
        [UnityTest]
        public IEnumerator SwitchPage_ByName_ShouldSwitchToPage()
        {
            var container = CreateTestContainer();
            var page1 = CreateTestPage<TestTabPage1>();
            var page2 = CreateTestPage<TestTabPage2>();
            yield return null;

            container.AddPage(page1);
            container.AddPage(page2);
            container.SwitchPage("TestTabPage2");

            Assert.AreEqual(page2, container.CurrentPage, "应切换到TestTabPage2");
        }

        /// <summary>
        /// 测试切换到下一页
        /// </summary>
        [UnityTest]
        public IEnumerator NextPage_ShouldSwitchToNextPage()
        {
            var container = CreateTestContainer();
            var page1 = CreateTestPage<TestTabPage1>();
            var page2 = CreateTestPage<TestTabPage2>();
            yield return null;

            container.AddPage(page1);
            container.AddPage(page2);
            container.SwitchPage(0);
            container.NextPage();

            Assert.AreEqual(1, container.CurrentPageIndex, "应切换到下一页");
        }

        /// <summary>
        /// 测试切换到下一页循环
        /// </summary>
        [UnityTest]
        public IEnumerator NextPage_AtLastPage_ShouldWrapToFirst()
        {
            var container = CreateTestContainer();
            var page1 = CreateTestPage<TestTabPage1>();
            var page2 = CreateTestPage<TestTabPage2>();
            yield return null;

            container.AddPage(page1);
            container.AddPage(page2);
            container.SwitchPage(1);
            container.NextPage();

            Assert.AreEqual(0, container.CurrentPageIndex, "应循环到第一页");
        }

        /// <summary>
        /// 测试切换到上一页
        /// </summary>
        [UnityTest]
        public IEnumerator PreviousPage_ShouldSwitchToPreviousPage()
        {
            var container = CreateTestContainer();
            var page1 = CreateTestPage<TestTabPage1>();
            var page2 = CreateTestPage<TestTabPage2>();
            yield return null;

            container.AddPage(page1);
            container.AddPage(page2);
            container.SwitchPage(1);
            container.PreviousPage();

            Assert.AreEqual(0, container.CurrentPageIndex, "应切换到上一页");
        }

        /// <summary>
        /// 测试切换到上一页循环
        /// </summary>
        [UnityTest]
        public IEnumerator PreviousPage_AtFirstPage_ShouldWrapToLast()
        {
            var container = CreateTestContainer();
            var page1 = CreateTestPage<TestTabPage1>();
            var page2 = CreateTestPage<TestTabPage2>();
            yield return null;

            container.AddPage(page1);
            container.AddPage(page2);
            container.SwitchPage(0);
            container.PreviousPage();

            Assert.AreEqual(1, container.CurrentPageIndex, "应循环到最后一页");
        }

        #endregion

        #region 页面生命周期测试

        /// <summary>
        /// 测试切换页面时调用OnPageEnter
        /// </summary>
        [UnityTest]
        public IEnumerator SwitchPage_ShouldCallOnPageEnter()
        {
            var container = CreateTestContainer();
            var page = CreateTestPage<TestTabPageWithCounter>();
            yield return null;

            container.AddPage(page);
            container.SwitchPage(0);

            Assert.AreEqual(1, page.EnterCount, "OnPageEnter应被调用");
        }

        /// <summary>
        /// 测试切换页面时调用OnPageExit
        /// </summary>
        [UnityTest]
        public IEnumerator SwitchPage_ShouldCallOnPageExit()
        {
            var container = CreateTestContainer();
            var page1 = CreateTestPage<TestTabPageWithCounter>();
            var page2 = CreateTestPage<TestTabPage2>();
            yield return null;

            container.AddPage(page1);
            container.AddPage(page2);
            container.SwitchPage(0);
            container.SwitchPage(1);

            Assert.AreEqual(1, page1.ExitCount, "OnPageExit应被调用");
        }

        #endregion

        #region 初始状态测试

        /// <summary>
        /// 测试初始CurrentPageIndex
        /// </summary>
        [UnityTest]
        public IEnumerator CurrentPageIndex_Initial_ShouldBeNegativeOne()
        {
            var container = CreateTestContainer();
            yield return null;

            Assert.AreEqual(-1, container.CurrentPageIndex, "初始CurrentPageIndex应为-1");
        }

        /// <summary>
        /// 测试初始CurrentPage
        /// </summary>
        [UnityTest]
        public IEnumerator CurrentPage_Initial_ShouldBeNull()
        {
            var container = CreateTestContainer();
            yield return null;

            Assert.IsNull(container.CurrentPage, "初始CurrentPage应为null");
        }

        /// <summary>
        /// 测试初始PageCount
        /// </summary>
        [UnityTest]
        public IEnumerator PageCount_Initial_ShouldBeZero()
        {
            var container = CreateTestContainer();
            yield return null;

            Assert.AreEqual(0, container.PageCount, "初始PageCount应为0");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建测试容器
        /// </summary>
        private TestTabContainer CreateTestContainer()
        {
            var go = new GameObject("TestContainer");
            go.transform.SetParent(_testRoot.transform);
            return go.AddComponent<TestTabContainer>();
        }

        /// <summary>
        /// 创建测试页面
        /// </summary>
        private T CreateTestPage<T>() where T : UITabPage
        {
            var go = new GameObject($"TestPage_{typeof(T).Name}");
            go.transform.SetParent(_testRoot.transform);
            return go.AddComponent<T>();
        }

        #endregion

        #region 测试辅助类

        /// <summary>
        /// 测试用容器
        /// </summary>
        private class TestTabContainer : UITabContainer
        {
        }

        /// <summary>
        /// 测试用页面1
        /// </summary>
        private class TestTabPage1 : UITabPage
        {
        }

        /// <summary>
        /// 测试用页面2
        /// </summary>
        private class TestTabPage2 : UITabPage
        {
        }

        /// <summary>
        /// 带计数器的测试页面
        /// </summary>
        private class TestTabPageWithCounter : UITabPage
        {
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }

            protected override void OnPageEnter()
            {
                base.OnPageEnter();
                EnterCount++;
            }

            protected override void OnPageExit()
            {
                base.OnPageExit();
                ExitCount++;
            }
        }

        #endregion
    }
}
