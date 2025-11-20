using System;
using NUnit.Framework;
using xFrame.Runtime.StateMachine;

namespace xFrame.Tests
{
    /// <summary>
    /// 状态机服务单元测试
    /// 测试状态机服务的管理功能，包括创建、获取、移除状态机和自动更新等
    /// </summary>
    [TestFixture]
    public class StateMachineServiceTests
    {
        #region 测试用类

        /// <summary>
        /// 测试用的上下文
        /// </summary>
        private class TestContext
        {
            public int Value { get; set; }
            public string Name { get; set; }
        }

        /// <summary>
        /// 测试用的状态A
        /// </summary>
        private class StateA : StateBase<TestContext>
        {
            public int EnterCount { get; private set; }
            public int UpdateCount { get; private set; }

            public override void OnEnter(TestContext context)
            {
                EnterCount++;
            }

            public override void OnUpdate(TestContext context)
            {
                UpdateCount++;
            }
        }

        /// <summary>
        /// 测试用的状态B
        /// </summary>
        private class StateB : StateBase<TestContext>
        {
            public int EnterCount { get; private set; }

            public override void OnEnter(TestContext context)
            {
                EnterCount++;
            }
        }

        /// <summary>
        /// 测试用的简单状态（不带上下文）
        /// </summary>
        private class SimpleState : StateBase
        {
            public int UpdateCount { get; private set; }

            public override void OnUpdate()
            {
                UpdateCount++;
            }
        }

        #endregion

        private StateMachineServiceService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new StateMachineServiceService();
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            _service = null;
        }

        #region 创建状态机测试

        /// <summary>
        /// 测试创建不带上下文的状态机
        /// </summary>
        [Test]
        public void CreateStateMachine_WithoutContext_ShouldCreateSuccessfully()
        {
            // Arrange
            const string name = "TestStateMachine";

            // Act
            var stateMachine = _service.CreateStateMachine(name, autoUpdate: false);

            // Assert
            Assert.IsNotNull(stateMachine, "状态机应该被成功创建");
            Assert.AreEqual(name, stateMachine.Name, "状态机名称应该正确设置");
        }

        /// <summary>
        /// 测试创建带上下文的状态机
        /// </summary>
        [Test]
        public void CreateStateMachine_WithContext_ShouldCreateSuccessfully()
        {
            // Arrange
            const string name = "TestStateMachineWithContext";
            var context = new TestContext { Value = 42, Name = "Test" };

            // Act
            var stateMachine = _service.CreateStateMachine(name, context, autoUpdate: false);

            // Assert
            Assert.IsNotNull(stateMachine, "状态机应该被成功创建");
            Assert.AreEqual(name, stateMachine.Name, "状态机名称应该正确设置");
            Assert.AreSame(context, stateMachine.Context, "上下文应该正确设置");
        }

        /// <summary>
        /// 测试创建重复名称的状态机应该抛出异常
        /// </summary>
        [Test]
        public void CreateStateMachine_DuplicateName_ShouldThrowException()
        {
            // Arrange
            const string name = "DuplicateStateMachine";
            _service.CreateStateMachine(name, autoUpdate: false);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _service.CreateStateMachine(name, autoUpdate: false),
                "创建重复名称的状态机应该抛出InvalidOperationException");
        }

        /// <summary>
        /// 测试创建带自动更新的状态机
        /// </summary>
        [Test]
        public void CreateStateMachine_WithAutoUpdate_ShouldAutoUpdate()
        {
            // Arrange
            const string name = "AutoUpdateStateMachine";
            var context = new TestContext();
            var stateMachine = _service.CreateStateMachine(name, context, autoUpdate: true);
            var state = new StateA();
            stateMachine.AddState(state);
            stateMachine.ChangeState<StateA>();

            // Act - 模拟VContainer的Tick调用
            ((VContainer.Unity.ITickable)_service).Tick();
            ((VContainer.Unity.ITickable)_service).Tick();
            ((VContainer.Unity.ITickable)_service).Tick();

            // Assert
            Assert.AreEqual(3, state.UpdateCount, "自动更新应该调用状态的OnUpdate方法");
        }

        /// <summary>
        /// 测试创建不带自动更新的状态机
        /// </summary>
        [Test]
        public void CreateStateMachine_WithoutAutoUpdate_ShouldNotAutoUpdate()
        {
            // Arrange
            const string name = "ManualUpdateStateMachine";
            var context = new TestContext();
            var stateMachine = _service.CreateStateMachine(name, context, autoUpdate: false);
            var state = new StateA();
            stateMachine.AddState(state);
            stateMachine.ChangeState<StateA>();

            // Act - 模拟VContainer的Tick调用
            ((VContainer.Unity.ITickable)_service).Tick();
            ((VContainer.Unity.ITickable)_service).Tick();

            // Assert
            Assert.AreEqual(0, state.UpdateCount, "不自动更新的状态机不应该调用OnUpdate");
        }

        #endregion

        #region 获取状态机测试

        /// <summary>
        /// 测试获取不带上下文的状态机
        /// </summary>
        [Test]
        public void GetStateMachine_WithoutContext_ShouldReturnCorrectInstance()
        {
            // Arrange
            const string name = "TestStateMachine";
            var createdStateMachine = _service.CreateStateMachine(name, autoUpdate: false);

            // Act
            var retrievedStateMachine = _service.GetStateMachine(name);

            // Assert
            Assert.IsNotNull(retrievedStateMachine, "应该能够获取已创建的状态机");
            Assert.AreSame(createdStateMachine, retrievedStateMachine, "获取的状态机应该是同一个实例");
        }

        /// <summary>
        /// 测试获取带上下文的状态机
        /// </summary>
        [Test]
        public void GetStateMachine_WithContext_ShouldReturnCorrectInstance()
        {
            // Arrange
            const string name = "TestStateMachineWithContext";
            var context = new TestContext();
            var createdStateMachine = _service.CreateStateMachine(name, context, autoUpdate: false);

            // Act
            var retrievedStateMachine = _service.GetStateMachine<TestContext>(name);

            // Assert
            Assert.IsNotNull(retrievedStateMachine, "应该能够获取已创建的状态机");
            Assert.AreSame(createdStateMachine, retrievedStateMachine, "获取的状态机应该是同一个实例");
        }

        /// <summary>
        /// 测试获取不存在的状态机
        /// </summary>
        [Test]
        public void GetStateMachine_NonExistent_ShouldReturnNull()
        {
            // Act
            var stateMachine = _service.GetStateMachine("NonExistent");

            // Assert
            Assert.IsNull(stateMachine, "获取不存在的状态机应该返回null");
        }

        /// <summary>
        /// 测试用错误的类型获取状态机
        /// </summary>
        [Test]
        public void GetStateMachine_WrongType_ShouldReturnNull()
        {
            // Arrange
            const string name = "TestStateMachine";
            _service.CreateStateMachine(name, autoUpdate: false); // 创建不带上下文的状态机

            // Act
            var stateMachine = _service.GetStateMachine<TestContext>(name); // 尝试用带上下文的类型获取

            // Assert
            Assert.IsNull(stateMachine, "用错误的类型获取状态机应该返回null");
        }

        #endregion

        #region 移除状态机测试

        /// <summary>
        /// 测试移除状态机
        /// </summary>
        [Test]
        public void RemoveStateMachine_ShouldRemoveSuccessfully()
        {
            // Arrange
            const string name = "TestStateMachine";
            _service.CreateStateMachine(name, autoUpdate: false);

            // Act
            _service.RemoveStateMachine(name);
            var stateMachine = _service.GetStateMachine(name);

            // Assert
            Assert.IsNull(stateMachine, "状态机应该被成功移除");
        }

        /// <summary>
        /// 测试移除状态机时应该停止状态机
        /// </summary>
        [Test]
        public void RemoveStateMachine_ShouldStopStateMachine()
        {
            // Arrange
            const string name = "TestStateMachine";
            var context = new TestContext();
            var stateMachine = _service.CreateStateMachine(name, context, autoUpdate: false);
            var state = new StateA();
            stateMachine.AddState(state);
            stateMachine.ChangeState<StateA>();

            // Act
            _service.RemoveStateMachine(name);

            // Assert
            Assert.IsFalse(stateMachine.IsRunning, "移除时状态机应该被停止");
        }

        /// <summary>
        /// 测试移除不存在的状态机
        /// </summary>
        [Test]
        public void RemoveStateMachine_NonExistent_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _service.RemoveStateMachine("NonExistent"),
                "移除不存在的状态机不应该抛出异常");
        }

        /// <summary>
        /// 测试移除状态机后不应该再自动更新
        /// </summary>
        [Test]
        public void RemoveStateMachine_ShouldStopAutoUpdate()
        {
            // Arrange
            const string name = "AutoUpdateStateMachine";
            var context = new TestContext();
            var stateMachine = _service.CreateStateMachine(name, context, autoUpdate: true);
            var state = new StateA();
            stateMachine.AddState(state);
            stateMachine.ChangeState<StateA>();

            // Act
            _service.RemoveStateMachine(name);
            ((VContainer.Unity.ITickable)_service).Tick();
            ((VContainer.Unity.ITickable)_service).Tick();

            // Assert
            Assert.AreEqual(0, state.UpdateCount, "移除后不应该再自动更新");
        }

        #endregion

        #region 自动更新测试

        /// <summary>
        /// 测试多个状态机的自动更新
        /// </summary>
        [Test]
        public void Tick_MultipleStateMachines_ShouldUpdateAll()
        {
            // Arrange
            var context1 = new TestContext();
            var context2 = new TestContext();
            
            var sm1 = _service.CreateStateMachine("SM1", context1, autoUpdate: true);
            var sm2 = _service.CreateStateMachine("SM2", context2, autoUpdate: true);
            
            var state1 = new StateA();
            var state2 = new StateA();
            
            sm1.AddState(state1);
            sm2.AddState(state2);
            
            sm1.ChangeState<StateA>();
            sm2.ChangeState<StateA>();

            // Act
            ((VContainer.Unity.ITickable)_service).Tick();
            ((VContainer.Unity.ITickable)_service).Tick();

            // Assert
            Assert.AreEqual(2, state1.UpdateCount, "第一个状态机应该被更新");
            Assert.AreEqual(2, state2.UpdateCount, "第二个状态机应该被更新");
        }

        /// <summary>
        /// 测试混合自动更新和手动更新的状态机
        /// </summary>
        [Test]
        public void Tick_MixedUpdateModes_ShouldOnlyUpdateAutoUpdateStateMachines()
        {
            // Arrange
            var context1 = new TestContext();
            var context2 = new TestContext();
            
            var sm1 = _service.CreateStateMachine("SM1", context1, autoUpdate: true);
            var sm2 = _service.CreateStateMachine("SM2", context2, autoUpdate: false);
            
            var state1 = new StateA();
            var state2 = new StateA();
            
            sm1.AddState(state1);
            sm2.AddState(state2);
            
            sm1.ChangeState<StateA>();
            sm2.ChangeState<StateA>();

            // Act
            ((VContainer.Unity.ITickable)_service).Tick();

            // Assert
            Assert.AreEqual(1, state1.UpdateCount, "自动更新的状态机应该被更新");
            Assert.AreEqual(0, state2.UpdateCount, "手动更新的状态机不应该被更新");
        }

        /// <summary>
        /// 测试不带上下文的状态机自动更新
        /// </summary>
        [Test]
        public void Tick_StateMachineWithoutContext_ShouldUpdate()
        {
            // Arrange
            var sm = _service.CreateStateMachine("SM", autoUpdate: true);
            var state = new SimpleState();
            sm.AddState(state);
            sm.ChangeState<SimpleState>();

            // Act
            ((VContainer.Unity.ITickable)_service).Tick();
            ((VContainer.Unity.ITickable)_service).Tick();

            // Assert
            Assert.AreEqual(2, state.UpdateCount, "不带上下文的状态机应该被自动更新");
        }

        #endregion

        #region 资源释放测试

        /// <summary>
        /// 测试Dispose应该停止所有状态机
        /// </summary>
        [Test]
        public void Dispose_ShouldStopAllStateMachines()
        {
            // Arrange
            var context1 = new TestContext();
            var context2 = new TestContext();
            
            var sm1 = _service.CreateStateMachine("SM1", context1, autoUpdate: true);
            var sm2 = _service.CreateStateMachine("SM2", context2, autoUpdate: true);
            
            sm1.AddState(new StateA());
            sm2.AddState(new StateA());
            
            sm1.ChangeState<StateA>();
            sm2.ChangeState<StateA>();

            // Act
            _service.Dispose();

            // Assert
            Assert.IsFalse(sm1.IsRunning, "第一个状态机应该被停止");
            Assert.IsFalse(sm2.IsRunning, "第二个状态机应该被停止");
        }

        /// <summary>
        /// 测试Dispose后应该清空所有状态机
        /// </summary>
        [Test]
        public void Dispose_ShouldClearAllStateMachines()
        {
            // Arrange
            _service.CreateStateMachine("SM1", autoUpdate: false);
            _service.CreateStateMachine("SM2", autoUpdate: false);

            // Act
            _service.Dispose();
            var sm1 = _service.GetStateMachine("SM1");
            var sm2 = _service.GetStateMachine("SM2");

            // Assert
            Assert.IsNull(sm1, "所有状态机应该被清空");
            Assert.IsNull(sm2, "所有状态机应该被清空");
        }

        /// <summary>
        /// 测试Dispose后Tick不应该抛出异常
        /// </summary>
        [Test]
        public void Tick_AfterDispose_ShouldNotThrow()
        {
            // Arrange
            var context = new TestContext();
            var sm = _service.CreateStateMachine("SM", context, autoUpdate: true);
            sm.AddState(new StateA());
            sm.ChangeState<StateA>();

            // Act
            _service.Dispose();

            // Assert
            Assert.DoesNotThrow(() => ((VContainer.Unity.ITickable)_service).Tick(),
                "Dispose后调用Tick不应该抛出异常");
        }

        #endregion

        #region 边界情况测试

        /// <summary>
        /// 测试空名称创建状态机
        /// </summary>
        [Test]
        public void CreateStateMachine_EmptyName_ShouldCreateSuccessfully()
        {
            // Act
            var sm = _service.CreateStateMachine("", autoUpdate: false);

            // Assert
            Assert.IsNotNull(sm, "使用空名称应该能够创建状态机");
            Assert.AreEqual("", sm.Name, "状态机名称应该是空字符串");
        }

        /// <summary>
        /// 测试在没有状态机时调用Tick
        /// </summary>
        [Test]
        public void Tick_WithNoStateMachines_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => ((VContainer.Unity.ITickable)_service).Tick(),
                "在没有状态机时调用Tick不应该抛出异常");
        }

        /// <summary>
        /// 测试创建大量状态机
        /// </summary>
        [Test]
        public void CreateStateMachine_ManyStateMachines_ShouldHandleCorrectly()
        {
            // Arrange & Act
            const int count = 100;
            for (int i = 0; i < count; i++)
            {
                _service.CreateStateMachine($"SM{i}", autoUpdate: false);
            }

            // Assert
            for (int i = 0; i < count; i++)
            {
                var sm = _service.GetStateMachine($"SM{i}");
                Assert.IsNotNull(sm, $"状态机SM{i}应该存在");
            }
        }

        /// <summary>
        /// 测试创建和移除状态机的循环操作
        /// </summary>
        [Test]
        public void CreateAndRemove_Repeatedly_ShouldWorkCorrectly()
        {
            // Arrange
            const string name = "RepeatedSM";

            // Act & Assert
            for (int i = 0; i < 10; i++)
            {
                var sm = _service.CreateStateMachine(name, autoUpdate: false);
                Assert.IsNotNull(sm, $"第{i}次创建应该成功");
                
                _service.RemoveStateMachine(name);
                var retrieved = _service.GetStateMachine(name);
                Assert.IsNull(retrieved, $"第{i}次移除应该成功");
            }
        }

        #endregion

        #region 集成测试

        /// <summary>
        /// 测试完整的状态机生命周期
        /// </summary>
        [Test]
        public void FullLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            const string name = "FullLifecycleSM";
            var context = new TestContext { Value = 0 };
            
            // Act - 创建状态机
            var sm = _service.CreateStateMachine(name, context, autoUpdate: true);
            Assert.IsNotNull(sm, "状态机应该被创建");

            // Act - 添加状态
            var stateA = new StateA();
            var stateB = new StateB();
            sm.AddState(stateA);
            sm.AddState(stateB);

            // Act - 切换状态
            sm.ChangeState<StateA>();
            Assert.AreEqual(1, stateA.EnterCount, "StateA应该被进入");

            // Act - 自动更新
            ((VContainer.Unity.ITickable)_service).Tick();
            ((VContainer.Unity.ITickable)_service).Tick();
            Assert.AreEqual(2, stateA.UpdateCount, "StateA应该被更新");

            // Act - 切换状态
            sm.ChangeState<StateB>();
            Assert.AreEqual(1, stateB.EnterCount, "StateB应该被进入");

            // Act - 获取状态机
            var retrieved = _service.GetStateMachine<TestContext>(name);
            Assert.AreSame(sm, retrieved, "应该能够获取状态机");

            // Act - 移除状态机
            _service.RemoveStateMachine(name);
            var afterRemove = _service.GetStateMachine<TestContext>(name);
            Assert.IsNull(afterRemove, "状态机应该被移除");
            Assert.IsFalse(sm.IsRunning, "状态机应该被停止");
        }

        #endregion
    }
}
