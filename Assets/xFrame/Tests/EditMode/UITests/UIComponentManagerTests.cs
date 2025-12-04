using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using xFrame.Runtime.UI;

namespace xFrame.Tests.EditMode.UITests
{
    /// <summary>
    /// UIComponentManager组件管理器的单元测试
    /// </summary>
    [TestFixture]
    public class UIComponentManagerTests
    {
        private UIComponentManager _manager;
        private GameObject _testRoot;

        [SetUp]
        public void SetUp()
        {
            _manager = new UIComponentManager();
            _testRoot = new GameObject("TestRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (_testRoot != null)
            {
                Object.DestroyImmediate(_testRoot);
            }
        }

        #region 构造函数测试

        /// <summary>
        /// 测试默认构造函数
        /// </summary>
        [Test]
        public void Constructor_Default_ShouldCreateEmptyManager()
        {
            var manager = new UIComponentManager();
            Assert.AreEqual(0, manager.GetComponentCount(), "新创建的管理器应无组件");
        }

        #endregion

        #region 组件注册测试

        /// <summary>
        /// 测试注册组件
        /// </summary>
        [Test]
        public void RegisterComponent_ValidComponent_ShouldRegisterSuccessfully()
        {
            var component = CreateTestComponent<TestUIComponent>();

            _manager.RegisterComponent(component);

            Assert.AreEqual(1, _manager.GetComponentCount(), "应有1个组件");
            Assert.IsTrue(component.IsInitialized, "组件应已初始化");
        }

        /// <summary>
        /// 测试注册null组件
        /// </summary>
        [Test]
        public void RegisterComponent_NullComponent_ShouldNotThrow()
        {
            LogAssert.Expect(LogType.Error, "[UIComponentManager] 组件为空，无法注册");
            Assert.DoesNotThrow(() => _manager.RegisterComponent<TestUIComponent>(null));
            Assert.AreEqual(0, _manager.GetComponentCount(), "不应注册null组件");
        }

        /// <summary>
        /// 测试注册多个组件
        /// </summary>
        [Test]
        public void RegisterComponent_MultipleComponents_ShouldRegisterAll()
        {
            var component1 = CreateTestComponent<TestUIComponent>();
            var component2 = CreateTestComponent<TestUIComponent>();
            var component3 = CreateTestComponent<TestUIComponent2>();

            _manager.RegisterComponent(component1);
            _manager.RegisterComponent(component2);
            _manager.RegisterComponent(component3);

            Assert.AreEqual(3, _manager.GetComponentCount(), "应有3个组件");
        }

        /// <summary>
        /// 测试重复注册同一组件
        /// </summary>
        [Test]
        public void RegisterComponent_SameComponentTwice_ShouldNotDuplicate()
        {
            var component = CreateTestComponent<TestUIComponent>();

            _manager.RegisterComponent(component);
            _manager.RegisterComponent(component);

            Assert.AreEqual(1, _manager.GetComponentCount(), "不应重复注册同一组件");
        }

        #endregion

        #region 组件注销测试

        /// <summary>
        /// 测试注销组件
        /// </summary>
        [Test]
        public void UnregisterComponent_ExistingComponent_ShouldRemove()
        {
            var component = CreateTestComponent<TestUIComponent>();
            _manager.RegisterComponent(component);
            var componentId = component.ComponentId;

            _manager.UnregisterComponent(componentId);

            Assert.AreEqual(0, _manager.GetComponentCount(), "组件应被移除");
        }

        /// <summary>
        /// 测试注销不存在的组件
        /// </summary>
        [Test]
        public void UnregisterComponent_NonExistingId_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.UnregisterComponent("non_existing_id"));
        }

        #endregion

        #region 组件查询测试

        /// <summary>
        /// 测试通过ID获取组件
        /// </summary>
        [Test]
        public void GetComponent_ById_ShouldReturnCorrectComponent()
        {
            var component = CreateTestComponent<TestUIComponent>();
            _manager.RegisterComponent(component);

            var result = _manager.GetComponent(component.ComponentId);

            Assert.AreEqual(component, result, "应返回正确的组件");
        }

        /// <summary>
        /// 测试通过ID获取不存在的组件
        /// </summary>
        [Test]
        public void GetComponent_NonExistingId_ShouldReturnNull()
        {
            var result = _manager.GetComponent("non_existing_id");
            Assert.IsNull(result, "不存在的ID应返回null");
        }

        /// <summary>
        /// 测试通过ID获取组件（泛型）
        /// </summary>
        [Test]
        public void GetComponent_Generic_ShouldReturnTypedComponent()
        {
            var component = CreateTestComponent<TestUIComponent>();
            _manager.RegisterComponent(component);

            var result = _manager.GetComponent<TestUIComponent>(component.ComponentId);

            Assert.IsNotNull(result, "应返回组件");
            Assert.IsInstanceOf<TestUIComponent>(result, "应返回正确类型");
        }

        /// <summary>
        /// 测试获取指定类型的第一个组件
        /// </summary>
        [Test]
        public void GetComponentOfType_ExistingType_ShouldReturnFirst()
        {
            var component1 = CreateTestComponent<TestUIComponent>();
            var component2 = CreateTestComponent<TestUIComponent>();
            _manager.RegisterComponent(component1);
            _manager.RegisterComponent(component2);

            var result = _manager.GetComponentOfType<TestUIComponent>();

            Assert.IsNotNull(result, "应返回组件");
            Assert.AreEqual(component1, result, "应返回第一个注册的组件");
        }

        /// <summary>
        /// 测试获取不存在类型的组件
        /// </summary>
        [Test]
        public void GetComponentOfType_NonExistingType_ShouldReturnNull()
        {
            var component = CreateTestComponent<TestUIComponent>();
            _manager.RegisterComponent(component);

            var result = _manager.GetComponentOfType<TestUIComponent2>();

            Assert.IsNull(result, "不存在的类型应返回null");
        }

        /// <summary>
        /// 测试获取指定类型的所有组件
        /// </summary>
        [Test]
        public void GetComponentsOfType_MultipleComponents_ShouldReturnAll()
        {
            var component1 = CreateTestComponent<TestUIComponent>();
            var component2 = CreateTestComponent<TestUIComponent>();
            var component3 = CreateTestComponent<TestUIComponent2>();
            _manager.RegisterComponent(component1);
            _manager.RegisterComponent(component2);
            _manager.RegisterComponent(component3);

            var results = _manager.GetComponentsOfType<TestUIComponent>();

            Assert.AreEqual(2, results.Count, "应返回2个TestUIComponent");
            Assert.Contains(component1, results);
            Assert.Contains(component2, results);
        }

        /// <summary>
        /// 测试获取所有组件
        /// </summary>
        [Test]
        public void GetAllComponents_ShouldReturnAllRegistered()
        {
            var component1 = CreateTestComponent<TestUIComponent>();
            var component2 = CreateTestComponent<TestUIComponent2>();
            _manager.RegisterComponent(component1);
            _manager.RegisterComponent(component2);

            var results = _manager.GetAllComponents();

            Assert.AreEqual(2, results.Count, "应返回所有组件");
        }

        #endregion

        #region 统计信息测试

        /// <summary>
        /// 测试获取组件总数
        /// </summary>
        [Test]
        public void GetComponentCount_ShouldReturnCorrectCount()
        {
            Assert.AreEqual(0, _manager.GetComponentCount(), "初始应为0");

            var component1 = CreateTestComponent<TestUIComponent>();
            _manager.RegisterComponent(component1);
            Assert.AreEqual(1, _manager.GetComponentCount(), "注册后应为1");

            var component2 = CreateTestComponent<TestUIComponent>();
            _manager.RegisterComponent(component2);
            Assert.AreEqual(2, _manager.GetComponentCount(), "再次注册后应为2");
        }

        /// <summary>
        /// 测试获取指定类型的组件数量
        /// </summary>
        [Test]
        public void GetComponentCountOfType_ShouldReturnCorrectCount()
        {
            var component1 = CreateTestComponent<TestUIComponent>();
            var component2 = CreateTestComponent<TestUIComponent>();
            var component3 = CreateTestComponent<TestUIComponent2>();
            _manager.RegisterComponent(component1);
            _manager.RegisterComponent(component2);
            _manager.RegisterComponent(component3);

            Assert.AreEqual(2, _manager.GetComponentCountOfType<TestUIComponent>(), "TestUIComponent应有2个");
            Assert.AreEqual(1, _manager.GetComponentCountOfType<TestUIComponent2>(), "TestUIComponent2应有1个");
        }

        /// <summary>
        /// 测试获取不存在类型的组件数量
        /// </summary>
        [Test]
        public void GetComponentCountOfType_NonExistingType_ShouldReturnZero()
        {
            var component = CreateTestComponent<TestUIComponent>();
            _manager.RegisterComponent(component);

            Assert.AreEqual(0, _manager.GetComponentCountOfType<TestUIComponent2>(), "不存在的类型应返回0");
        }

        #endregion

        #region 生命周期传递测试

        /// <summary>
        /// 测试OnParentDestroy清理所有组件
        /// </summary>
        [Test]
        public void OnParentDestroy_ShouldClearAllComponents()
        {
            var component1 = CreateTestComponent<TestUIComponent>();
            var component2 = CreateTestComponent<TestUIComponent>();
            _manager.RegisterComponent(component1);
            _manager.RegisterComponent(component2);

            _manager.OnParentDestroy();

            Assert.AreEqual(0, _manager.GetComponentCount(), "所有组件应被清理");
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
        /// 测试用UIComponent类型1
        /// </summary>
        private class TestUIComponent : UIComponent
        {
        }

        /// <summary>
        /// 测试用UIComponent类型2
        /// </summary>
        private class TestUIComponent2 : UIComponent
        {
        }

        #endregion
    }
}
