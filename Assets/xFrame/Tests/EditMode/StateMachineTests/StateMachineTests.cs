using System;
using NUnit.Framework;
using xFrame.Runtime.StateMachine;
using xFrame.Runtime.EventBus;

namespace xFrame.Tests
{
    /// <summary>
    /// 状态机单元测试
    /// 测试状态机的核心功能，包括状态添加、切换、更新和事件触发等
    /// </summary>
    [TestFixture]
    public class StateMachineTests
    {
        #region 测试用状态类

        /// <summary>
        /// 测试用的简单上下文
        /// </summary>
        private class TestContext
        {
            public int EnterCount { get; set; }
            public int UpdateCount { get; set; }
            public int ExitCount { get; set; }
            public string LastState { get; set; }
        }

        /// <summary>
        /// 测试用的空状态（不带上下文）
        /// </summary>
        private class SimpleState : StateBase
        {
            public int EnterCount { get; private set; }
            public int UpdateCount { get; private set; }
            public int ExitCount { get; private set; }

            public override void OnEnter()
            {
                EnterCount++;
            }

            public override void OnUpdate()
            {
                UpdateCount++;
            }

            public override void OnExit()
            {
                ExitCount++;
            }
        }

        /// <summary>
        /// 另一个简单状态
        /// </summary>
        private class AnotherSimpleState : StateBase
        {
            public int EnterCount { get; private set; }

            public override void OnEnter()
            {
                EnterCount++;
            }
        }

        /// <summary>
        /// 测试用的状态A（带上下文）
        /// </summary>
        private class StateA : StateBase<TestContext>
        {
            public override void OnEnter(TestContext context)
            {
                context.EnterCount++;
                context.LastState = "StateA";
            }

            public override void OnUpdate(TestContext context)
            {
                context.UpdateCount++;
            }

            public override void OnExit(TestContext context)
            {
                context.ExitCount++;
            }
        }

        /// <summary>
        /// 测试用的状态B（带上下文）
        /// </summary>
        private class StateB : StateBase<TestContext>
        {
            public override void OnEnter(TestContext context)
            {
                context.EnterCount++;
                context.LastState = "StateB";
            }

            public override void OnUpdate(TestContext context)
            {
                context.UpdateCount++;
            }

            public override void OnExit(TestContext context)
            {
                context.ExitCount++;
            }
        }

        /// <summary>
        /// 测试用的状态C（带上下文）
        /// </summary>
        private class StateC : StateBase<TestContext>
        {
            public override void OnEnter(TestContext context)
            {
                context.EnterCount++;
                context.LastState = "StateC";
            }
        }

        #endregion

        #region 不带上下文的状态机测试

        /// <summary>
        /// 测试创建状态机
        /// </summary>
        [Test]
        public void CreateStateMachine_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var stateMachine = new StateMachine();
            stateMachine.Name = "TestStateMachine";

            // Assert
            Assert.IsNotNull(stateMachine, "状态机应该被成功创建");
            Assert.AreEqual("TestStateMachine", stateMachine.Name, "状态机名称应该正确设置");
            Assert.IsNull(stateMachine.CurrentState, "初始状态应该为null");
            Assert.IsFalse(stateMachine.IsRunning, "状态机初始时不应该运行");
        }

        /// <summary>
        /// 测试添加状态
        /// </summary>
        [Test]
        public void AddState_ShouldAddStateSuccessfully()
        {
            // Arrange
            var stateMachine = new StateMachine();
            var state = new SimpleState();

            // Act
            stateMachine.AddState(state);
            var retrievedState = stateMachine.GetState<SimpleState>();

            // Assert
            Assert.IsNotNull(retrievedState, "应该能够获取已添加的状态");
            Assert.AreSame(state, retrievedState, "获取的状态应该是同一个实例");
        }

        /// <summary>
        /// 测试添加重复状态应该抛出异常
        /// </summary>
        [Test]
        public void AddState_DuplicateState_ShouldThrowException()
        {
            // Arrange
            var stateMachine = new StateMachine();
            var state1 = new SimpleState();
            var state2 = new SimpleState();

            // Act
            stateMachine.AddState(state1);

            // Assert
            Assert.Throws<InvalidOperationException>(() => stateMachine.AddState(state2),
                "添加重复状态应该抛出InvalidOperationException");
        }

        /// <summary>
        /// 测试切换状态
        /// </summary>
        [Test]
        public void ChangeState_ShouldSwitchStateCorrectly()
        {
            // Arrange
            var stateMachine = new StateMachine();
            var state = new SimpleState();
            stateMachine.AddState(state);

            // Act
            stateMachine.ChangeState<SimpleState>();

            // Assert
            Assert.AreSame(state, stateMachine.CurrentState, "当前状态应该是切换后的状态");
            Assert.AreEqual(typeof(SimpleState), stateMachine.CurrentStateType, "当前状态类型应该正确");
            Assert.IsTrue(stateMachine.IsRunning, "状态机应该处于运行状态");
            Assert.AreEqual(1, state.EnterCount, "OnEnter应该被调用一次");
        }

        /// <summary>
        /// 测试切换到不存在的状态应该抛出异常
        /// </summary>
        [Test]
        public void ChangeState_NonExistentState_ShouldThrowException()
        {
            // Arrange
            var stateMachine = new StateMachine();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => stateMachine.ChangeState<SimpleState>(),
                "切换到不存在的状态应该抛出InvalidOperationException");
        }

        /// <summary>
        /// 测试状态切换时的生命周期
        /// </summary>
        [Test]
        public void ChangeState_ShouldCallExitAndEnter()
        {
            // Arrange
            var stateMachine = new StateMachine();
            var state1 = new SimpleState();
            var state2 = new AnotherSimpleState();
            stateMachine.AddState(state1);
            stateMachine.AddState(state2);

            // Act
            stateMachine.ChangeState<SimpleState>();
            stateMachine.ChangeState<AnotherSimpleState>();

            // Assert
            Assert.AreEqual(1, state1.EnterCount, "第一个状态的OnEnter应该被调用一次");
            Assert.AreEqual(1, state1.ExitCount, "第一个状态的OnExit应该被调用一次");
            Assert.AreEqual(1, state2.EnterCount, "第二个状态的OnEnter应该被调用一次");
            Assert.AreSame(state2, stateMachine.CurrentState, "当前状态应该是第二个状态");
        }

        /// <summary>
        /// 测试状态更新
        /// </summary>
        [Test]
        public void Update_ShouldCallCurrentStateUpdate()
        {
            // Arrange
            var stateMachine = new StateMachine();
            var state = new SimpleState();
            stateMachine.AddState(state);
            stateMachine.ChangeState<SimpleState>();

            // Act
            stateMachine.Update();
            stateMachine.Update();
            stateMachine.Update();

            // Assert
            Assert.AreEqual(3, state.UpdateCount, "OnUpdate应该被调用3次");
        }

        /// <summary>
        /// 测试停止状态机
        /// </summary>
        [Test]
        public void Stop_ShouldStopStateMachine()
        {
            // Arrange
            var stateMachine = new StateMachine();
            var state = new SimpleState();
            stateMachine.AddState(state);
            stateMachine.ChangeState<SimpleState>();

            // Act
            stateMachine.Stop();

            // Assert
            Assert.IsFalse(stateMachine.IsRunning, "状态机应该停止运行");
            Assert.IsNull(stateMachine.CurrentState, "当前状态应该为null");
            Assert.AreEqual(1, state.ExitCount, "OnExit应该被调用一次");
        }

        /// <summary>
        /// 测试移除状态
        /// </summary>
        [Test]
        public void RemoveState_ShouldRemoveStateSuccessfully()
        {
            // Arrange
            var stateMachine = new StateMachine();
            var state = new SimpleState();
            stateMachine.AddState(state);

            // Act
            stateMachine.RemoveState<SimpleState>();
            var retrievedState = stateMachine.GetState<SimpleState>();

            // Assert
            Assert.IsNull(retrievedState, "状态应该被成功移除");
        }

        /// <summary>
        /// 测试移除当前状态应该抛出异常
        /// </summary>
        [Test]
        public void RemoveState_CurrentState_ShouldThrowException()
        {
            // Arrange
            var stateMachine = new StateMachine();
            var state = new SimpleState();
            stateMachine.AddState(state);
            stateMachine.ChangeState<SimpleState>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => stateMachine.RemoveState<SimpleState>(),
                "移除当前状态应该抛出InvalidOperationException");
        }

        /// <summary>
        /// 测试清空状态机
        /// </summary>
        [Test]
        public void Clear_ShouldClearAllStates()
        {
            // Arrange
            var stateMachine = new StateMachine();
            var state1 = new SimpleState();
            var state2 = new AnotherSimpleState();
            stateMachine.AddState(state1);
            stateMachine.AddState(state2);
            stateMachine.ChangeState<SimpleState>();

            // Act
            stateMachine.Clear();

            // Assert
            Assert.IsFalse(stateMachine.IsRunning, "状态机应该停止运行");
            Assert.IsNull(stateMachine.CurrentState, "当前状态应该为null");
            Assert.IsNull(stateMachine.GetState<SimpleState>(), "所有状态应该被清空");
            Assert.IsNull(stateMachine.GetState<AnotherSimpleState>(), "所有状态应该被清空");
        }

        #endregion

        #region 带上下文的状态机测试

        /// <summary>
        /// 测试创建带上下文的状态机
        /// </summary>
        [Test]
        public void CreateStateMachineWithContext_ShouldInitializeCorrectly()
        {
            // Arrange
            var context = new TestContext();

            // Act
            var stateMachine = new StateMachine<TestContext>(context);
            stateMachine.Name = "TestStateMachineWithContext";

            // Assert
            Assert.IsNotNull(stateMachine, "状态机应该被成功创建");
            Assert.AreEqual("TestStateMachineWithContext", stateMachine.Name, "状态机名称应该正确设置");
            Assert.AreSame(context, stateMachine.Context, "上下文应该正确设置");
            Assert.IsNull(stateMachine.CurrentState, "初始状态应该为null");
            Assert.IsFalse(stateMachine.IsRunning, "状态机初始时不应该运行");
        }

        /// <summary>
        /// 测试带上下文的状态切换
        /// </summary>
        [Test]
        public void ChangeStateWithContext_ShouldPassContextCorrectly()
        {
            // Arrange
            var context = new TestContext();
            var stateMachine = new StateMachine<TestContext>(context);
            stateMachine.AddState(new StateA());

            // Act
            stateMachine.ChangeState<StateA>();

            // Assert
            Assert.AreEqual(1, context.EnterCount, "OnEnter应该被调用并更新上下文");
            Assert.AreEqual("StateA", context.LastState, "上下文应该记录最后的状态");
        }

        /// <summary>
        /// 测试带上下文的状态更新
        /// </summary>
        [Test]
        public void UpdateWithContext_ShouldPassContextCorrectly()
        {
            // Arrange
            var context = new TestContext();
            var stateMachine = new StateMachine<TestContext>(context);
            stateMachine.AddState(new StateA());
            stateMachine.ChangeState<StateA>();

            // Act
            stateMachine.Update();
            stateMachine.Update();

            // Assert
            Assert.AreEqual(2, context.UpdateCount, "OnUpdate应该被调用并更新上下文");
        }

        /// <summary>
        /// 测试带上下文的状态切换生命周期
        /// </summary>
        [Test]
        public void ChangeStateWithContext_ShouldCallExitAndEnter()
        {
            // Arrange
            var context = new TestContext();
            var stateMachine = new StateMachine<TestContext>(context);
            stateMachine.AddState(new StateA());
            stateMachine.AddState(new StateB());

            // Act
            stateMachine.ChangeState<StateA>();
            Assert.AreEqual("StateA", context.LastState, "应该进入StateA");
            
            stateMachine.ChangeState<StateB>();

            // Assert
            Assert.AreEqual(2, context.EnterCount, "OnEnter应该被调用两次");
            Assert.AreEqual(1, context.ExitCount, "OnExit应该被调用一次");
            Assert.AreEqual("StateB", context.LastState, "应该切换到StateB");
        }

        /// <summary>
        /// 测试带上下文的状态机停止
        /// </summary>
        [Test]
        public void StopWithContext_ShouldCallExitWithContext()
        {
            // Arrange
            var context = new TestContext();
            var stateMachine = new StateMachine<TestContext>(context);
            stateMachine.AddState(new StateA());
            stateMachine.ChangeState<StateA>();

            // Act
            stateMachine.Stop();

            // Assert
            Assert.AreEqual(1, context.ExitCount, "OnExit应该被调用并传递上下文");
            Assert.IsFalse(stateMachine.IsRunning, "状态机应该停止运行");
        }

        /// <summary>
        /// 测试修改上下文
        /// </summary>
        [Test]
        public void ModifyContext_ShouldUpdateContext()
        {
            // Arrange
            var context1 = new TestContext();
            var context2 = new TestContext();
            var stateMachine = new StateMachine<TestContext>(context1);

            // Act
            stateMachine.Context = context2;

            // Assert
            Assert.AreSame(context2, stateMachine.Context, "上下文应该被成功修改");
        }

        #endregion

        #region 事件测试

        private StateChangedEvent _lastEvent;
        private StateChangedEvent<TestContext> _lastEventWithContext;
        private int _eventCallCount;

        [SetUp]
        public void SetUp()
        {
            _lastEvent = default;
            _lastEventWithContext = default;
            _eventCallCount = 0;
            
            // 清理事件监听器
            xFrameEventBus.ClearListeners<StateChangedEvent>();
            xFrameEventBus.ClearListeners<StateChangedEvent<TestContext>>();
        }

        [TearDown]
        public void TearDown()
        {
            // 清理事件监听器
            xFrameEventBus.ClearListeners<StateChangedEvent>();
            xFrameEventBus.ClearListeners<StateChangedEvent<TestContext>>();
        }

        /// <summary>
        /// 测试状态改变事件（不带上下文）
        /// </summary>
        [Test]
        public void ChangeState_ShouldRaiseStateChangedEvent()
        {
            // Arrange
            var stateMachine = new StateMachine();
            stateMachine.Name = "TestSM";
            stateMachine.AddState(new SimpleState());
            stateMachine.AddState(new AnotherSimpleState());

            xFrameEventBus.SubscribeTo<StateChangedEvent>(OnStateChanged);

            // Act
            stateMachine.ChangeState<SimpleState>();

            // Assert
            Assert.AreEqual(1, _eventCallCount, "状态改变事件应该被触发一次");
            Assert.AreEqual("TestSM", _lastEvent.StateMachineName, "事件应该包含正确的状态机名称");
            Assert.IsNull(_lastEvent.PreviousState, "第一次切换时前一个状态应该为null");
            Assert.IsNotNull(_lastEvent.CurrentState, "当前状态不应该为null");

            // Act - 切换到另一个状态
            _eventCallCount = 0;
            stateMachine.ChangeState<AnotherSimpleState>();

            // Assert
            Assert.AreEqual(1, _eventCallCount, "状态改变事件应该再次被触发");
            Assert.IsNotNull(_lastEvent.PreviousState, "前一个状态不应该为null");
            Assert.IsNotNull(_lastEvent.CurrentState, "当前状态不应该为null");
        }

        /// <summary>
        /// 测试状态改变事件（带上下文）
        /// </summary>
        [Test]
        public void ChangeStateWithContext_ShouldRaiseStateChangedEvent()
        {
            // Arrange
            var context = new TestContext();
            var stateMachine = new StateMachine<TestContext>(context);
            stateMachine.Name = "TestSMWithContext";
            stateMachine.AddState(new StateA());
            stateMachine.AddState(new StateB());

            xFrameEventBus.SubscribeTo<StateChangedEvent<TestContext>>(OnStateChangedWithContext);

            // Act
            stateMachine.ChangeState<StateA>();

            // Assert
            Assert.AreEqual(1, _eventCallCount, "状态改变事件应该被触发一次");
            Assert.AreEqual("TestSMWithContext", _lastEventWithContext.StateMachineName, "事件应该包含正确的状态机名称");
            Assert.IsNull(_lastEventWithContext.PreviousState, "第一次切换时前一个状态应该为null");
            Assert.IsNotNull(_lastEventWithContext.CurrentState, "当前状态不应该为null");
            Assert.AreSame(context, _lastEventWithContext.Context, "事件应该包含正确的上下文");

            // Act - 切换到另一个状态
            _eventCallCount = 0;
            stateMachine.ChangeState<StateB>();

            // Assert
            Assert.AreEqual(1, _eventCallCount, "状态改变事件应该再次被触发");
            Assert.IsInstanceOf<StateA>(_lastEventWithContext.PreviousState, "前一个状态应该是StateA");
            Assert.IsInstanceOf<StateB>(_lastEventWithContext.CurrentState, "当前状态应该是StateB");
        }

        private void OnStateChanged(ref StateChangedEvent evt)
        {
            _lastEvent = evt;
            _eventCallCount++;
        }

        private void OnStateChangedWithContext(ref StateChangedEvent<TestContext> evt)
        {
            _lastEventWithContext = evt;
            _eventCallCount++;
        }

        #endregion

        #region 边界情况测试

        /// <summary>
        /// 测试在没有状态时更新状态机
        /// </summary>
        [Test]
        public void Update_WithoutState_ShouldNotThrow()
        {
            // Arrange
            var stateMachine = new StateMachine();

            // Act & Assert
            Assert.DoesNotThrow(() => stateMachine.Update(), "在没有状态时更新不应该抛出异常");
        }

        /// <summary>
        /// 测试在停止后更新状态机
        /// </summary>
        [Test]
        public void Update_AfterStop_ShouldNotCallUpdate()
        {
            // Arrange
            var stateMachine = new StateMachine();
            var state = new SimpleState();
            stateMachine.AddState(state);
            stateMachine.ChangeState<SimpleState>();
            stateMachine.Stop();

            // Act
            stateMachine.Update();

            // Assert
            Assert.AreEqual(0, state.UpdateCount, "停止后OnUpdate不应该被调用");
        }

        /// <summary>
        /// 测试多次停止状态机
        /// </summary>
        [Test]
        public void Stop_MultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var stateMachine = new StateMachine();
            var state = new SimpleState();
            stateMachine.AddState(state);
            stateMachine.ChangeState<SimpleState>();

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                stateMachine.Stop();
                stateMachine.Stop();
                stateMachine.Stop();
            }, "多次停止不应该抛出异常");

            Assert.AreEqual(1, state.ExitCount, "OnExit应该只被调用一次");
        }

        /// <summary>
        /// 测试获取不存在的状态
        /// </summary>
        [Test]
        public void GetState_NonExistentState_ShouldReturnNull()
        {
            // Arrange
            var stateMachine = new StateMachine();

            // Act
            var state = stateMachine.GetState<SimpleState>();

            // Assert
            Assert.IsNull(state, "获取不存在的状态应该返回null");
        }

        #endregion
    }
}
