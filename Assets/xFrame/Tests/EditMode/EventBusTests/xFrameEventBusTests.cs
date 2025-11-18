using System;
using NUnit.Framework;
using xFrame.Runtime.EventBus;
using GenericEventBus;
using UnityEngine;

namespace xFrame.Tests
{
    /// <summary>
    /// xFrame事件总线单元测试
    /// 测试事件总线的核心功能,包括订阅、触发、优先级和消费等
    /// </summary>
    [TestFixture]
    public class xFrameEventBusTests
    {
        /// <summary>
        /// 测试用的简单事件
        /// </summary>
        private class TestEvent : IEvent
        {
            public string Message { get; set; }
            public int Value { get; set; }
        }

        /// <summary>
        /// 另一个测试事件类型
        /// </summary>
        private class AnotherTestEvent : IEvent
        {
            public bool Flag { get; set; }
        }

        private int _eventCallCount;
        private string _lastMessage;
        private int _lastValue;
        private bool _lastFlag;

        [SetUp]
        public void SetUp()
        {
            // 重置计数器和状态
            _eventCallCount = 0;
            _lastMessage = null;
            _lastValue = 0;
            _lastFlag = false;
            
            // 清理所有监听器
            xFrameEventBus.ClearListeners<TestEvent>();
            xFrameEventBus.ClearListeners<AnotherTestEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            // 清理所有监听器
            xFrameEventBus.ClearListeners<TestEvent>();
            xFrameEventBus.ClearListeners<AnotherTestEvent>();
        }

        #region 基础订阅和触发测试

        /// <summary>
        /// 测试基本的事件订阅和触发
        /// </summary>
        [Test]
        public void SubscribeAndRaise_ShouldInvokeHandler()
        {
            // Arrange
            xFrameEventBus.SubscribeTo<TestEvent>(OnTestEvent);
            var testEvent = new TestEvent { Message = "Hello", Value = 42 };

            // Act
            xFrameEventBus.Raise(testEvent);

            // Assert
            Assert.AreEqual(1, _eventCallCount, "事件处理器应该被调用一次");
            Assert.AreEqual("Hello", _lastMessage, "消息应该正确传递");
            Assert.AreEqual(42, _lastValue, "值应该正确传递");
        }

        /// <summary>
        /// 测试立即触发事件
        /// </summary>
        [Test]
        public void RaiseImmediately_ShouldInvokeHandler()
        {
            // Arrange
            GenericEventBus<IEvent>.EventHandler<TestEvent> handler = (ref TestEvent e) => OnTestEvent(ref e);
            xFrameEventBus.SubscribeTo<TestEvent>(handler);
            var testEvent = new TestEvent { Message = "Immediate", Value = 100 };

            // Act
            xFrameEventBus.RaiseImmediately(testEvent);

            // Assert
            Assert.AreEqual(1, _eventCallCount, "事件处理器应该被立即调用");
            Assert.AreEqual("Immediate", _lastMessage);
            Assert.AreEqual(100, _lastValue);
        }

        /// <summary>
        /// 测试使用ref参数立即触发事件
        /// </summary>
        [Test]
        public void RaiseImmediately_WithRef_ShouldInvokeHandler()
        {
            // Arrange
            GenericEventBus<IEvent>.EventHandler<TestEvent> handler = (ref TestEvent e) => OnTestEvent(ref e);
            xFrameEventBus.SubscribeTo<TestEvent>(handler);
            var testEvent = new TestEvent { Message = "RefTest", Value = 200 };

            // Act
            xFrameEventBus.RaiseImmediately(ref testEvent);

            // Assert
            Assert.AreEqual(1, _eventCallCount);
            Assert.AreEqual("RefTest", _lastMessage);
            Assert.AreEqual(200, _lastValue);
        }

        /// <summary>
        /// 测试取消订阅
        /// </summary>
        [Test]
        public void Unsubscribe_ShouldStopInvokingHandler()
        {
            // Arrange
            GenericEventBus<IEvent>.EventHandler<TestEvent> handler = (ref TestEvent e) => OnTestEvent(ref e);
            xFrameEventBus.SubscribeTo<TestEvent>(handler);
            var testEvent = new TestEvent { Message = "Test", Value = 1 };

            // Act - 第一次触发
            xFrameEventBus.Raise(testEvent);
            Assert.AreEqual(1, _eventCallCount, "第一次应该被调用");

            // 取消订阅
            xFrameEventBus.UnsubscribeFrom<TestEvent>(handler);

            // 第二次触发
            xFrameEventBus.Raise(testEvent);

            // Assert
            Assert.AreEqual(1, _eventCallCount, "取消订阅后不应该再被调用");
        }

        /// <summary>
        /// 测试多个订阅者
        /// </summary>
        [Test]
        public void MultipleSubscribers_ShouldAllBeInvoked()
        {
            // Arrange
            int count1 = 0, count2 = 0, count3 = 0;
            
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => count1++);
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => count2++);
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => count3++);

            var testEvent = new TestEvent();

            // Act
            xFrameEventBus.Raise(testEvent);

            // Assert
            Assert.AreEqual(1, count1, "第一个订阅者应该被调用");
            Assert.AreEqual(1, count2, "第二个订阅者应该被调用");
            Assert.AreEqual(1, count3, "第三个订阅者应该被调用");
        }

        #endregion

        #region 优先级测试

        /// <summary>
        /// 测试事件处理器的优先级顺序
        /// </summary>
        [Test]
        public void Priority_ShouldDetermineInvocationOrder()
        {
            // Arrange
            var callOrder = "";
            
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => callOrder += "A", priority: 0);
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => callOrder += "B", priority: 10);
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => callOrder += "C", priority: 5);

            var testEvent = new TestEvent();

            // Act
            xFrameEventBus.Raise(testEvent);

            // Assert
            Assert.AreEqual("BCA", callOrder, "应该按照优先级从高到低调用: B(10) -> C(5) -> A(0)");
        }

        /// <summary>
        /// 测试相同优先级按订阅顺序调用
        /// </summary>
        [Test]
        public void SamePriority_ShouldInvokeInSubscriptionOrder()
        {
            // Arrange
            var callOrder = "";
            
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => callOrder += "1", priority: 0);
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => callOrder += "2", priority: 0);
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => callOrder += "3", priority: 0);

            var testEvent = new TestEvent();

            // Act
            xFrameEventBus.Raise(testEvent);

            // Assert
            Assert.AreEqual("123", callOrder, "相同优先级应该按订阅顺序调用");
        }

        #endregion

        #region 事件消费测试

        /// <summary>
        /// 测试消费当前事件
        /// </summary>
        [Test]
        public void ConsumeCurrentEvent_ShouldStopPropagation()
        {
            // Arrange
            int count1 = 0, count2 = 0, count3 = 0;
            
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => 
            {
                count1++;
                xFrameEventBus.ConsumeCurrentEvent(); // 消费事件
            }, priority: 10);
            
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => count2++, priority: 5);
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => count3++, priority: 0);

            var testEvent = new TestEvent();

            // Act
            bool consumed = xFrameEventBus.Raise(testEvent);

            // Assert
            Assert.IsTrue(consumed, "事件应该被标记为已消费");
            Assert.AreEqual(1, count1, "第一个处理器应该被调用");
            Assert.AreEqual(0, count2, "第二个处理器不应该被调用");
            Assert.AreEqual(0, count3, "第三个处理器不应该被调用");
        }

        /// <summary>
        /// 测试CurrentEventIsConsumed属性
        /// </summary>
        [Test]
        public void CurrentEventIsConsumed_ShouldReflectConsumptionState()
        {
            // Arrange
            bool wasConsumedInHandler = false;
            
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => 
            {
                xFrameEventBus.ConsumeCurrentEvent();
                wasConsumedInHandler = xFrameEventBus.CurrentEventIsConsumed;
            });

            var testEvent = new TestEvent();

            // Act
            xFrameEventBus.Raise(testEvent);

            // Assert
            Assert.IsTrue(wasConsumedInHandler, "在处理器中应该能检测到事件已被消费");
        }

        #endregion

        #region 目标和来源测试

        /// <summary>
        /// 测试带目标和来源的事件触发
        /// </summary>
        [Test]
        public void RaiseWithTargetAndSource_ShouldInvokeTargetedHandler()
        {
            // Arrange
            GameObject target = new GameObject("Target");
            GameObject source = new GameObject("Source");
            int callCount = 0;
            GameObject receivedTarget = null;
            GameObject receivedSource = null;

            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e, GameObject t, GameObject s) =>
            {
                callCount++;
                receivedTarget = t;
                receivedSource = s;
            });

            var testEvent = new TestEvent { Message = "Targeted" };

            // Act
            xFrameEventBus.Raise(testEvent, target, source);

            // Assert
            Assert.AreEqual(1, callCount, "处理器应该被调用");
            Assert.AreEqual(target, receivedTarget, "目标对象应该正确传递");
            Assert.AreEqual(source, receivedSource, "来源对象应该正确传递");

            // Cleanup
            GameObject.DestroyImmediate(target);
            GameObject.DestroyImmediate(source);
        }

        /// <summary>
        /// 测试订阅特定目标的事件
        /// </summary>
        [Test]
        public void SubscribeToTarget_ShouldOnlyInvokeForSpecificTarget()
        {
            // Arrange
            GameObject target1 = new GameObject("Target1");
            GameObject target2 = new GameObject("Target2");
            GameObject source = new GameObject("Source");
            
            int count1 = 0, count2 = 0;

            xFrameEventBus.SubscribeToTarget<TestEvent>(target1, 
                (ref TestEvent e, GameObject t, GameObject s) => count1++);
            
            xFrameEventBus.SubscribeToTarget<TestEvent>(target2, 
                (ref TestEvent e, GameObject t, GameObject s) => count2++);

            var testEvent = new TestEvent();

            // Act
            xFrameEventBus.Raise(testEvent, target1, source);

            // Assert
            Assert.AreEqual(1, count1, "target1的处理器应该被调用");
            Assert.AreEqual(0, count2, "target2的处理器不应该被调用");

            // Cleanup
            GameObject.DestroyImmediate(target1);
            GameObject.DestroyImmediate(target2);
            GameObject.DestroyImmediate(source);
        }

        /// <summary>
        /// 测试订阅特定来源的事件
        /// </summary>
        [Test]
        public void SubscribeToSource_ShouldOnlyInvokeForSpecificSource()
        {
            // Arrange
            GameObject target = new GameObject("Target");
            GameObject source1 = new GameObject("Source1");
            GameObject source2 = new GameObject("Source2");
            
            int count1 = 0, count2 = 0;

            xFrameEventBus.SubscribeToSource<TestEvent>(source1, 
                (ref TestEvent e, GameObject t, GameObject s) => count1++);
            
            xFrameEventBus.SubscribeToSource<TestEvent>(source2, 
                (ref TestEvent e, GameObject t, GameObject s) => count2++);

            var testEvent = new TestEvent();

            // Act
            xFrameEventBus.Raise(testEvent, target, source1);

            // Assert
            Assert.AreEqual(1, count1, "source1的处理器应该被调用");
            Assert.AreEqual(0, count2, "source2的处理器不应该被调用");

            // Cleanup
            GameObject.DestroyImmediate(target);
            GameObject.DestroyImmediate(source1);
            GameObject.DestroyImmediate(source2);
        }

        /// <summary>
        /// 测试取消订阅特定目标
        /// </summary>
        [Test]
        public void UnsubscribeFromTarget_ShouldStopInvokingHandler()
        {
            // Arrange
            GameObject target = new GameObject("Target");
            GameObject source = new GameObject("Source");
            int callCount = 0;

            GenericEventBus<IEvent, GameObject>.TargetedEventHandler<TestEvent> handler = 
                (ref TestEvent e, GameObject t, GameObject s) => callCount++;

            xFrameEventBus.SubscribeToTarget<TestEvent>(target, handler);

            var testEvent = new TestEvent();

            // Act - 第一次触发
            xFrameEventBus.Raise(testEvent, target, source);
            Assert.AreEqual(1, callCount, "第一次应该被调用");

            // 取消订阅
            xFrameEventBus.UnsubscribeFromTarget<TestEvent>(target, handler);

            // 第二次触发
            xFrameEventBus.Raise(testEvent, target, source);

            // Assert
            Assert.AreEqual(1, callCount, "取消订阅后不应该再被调用");

            // Cleanup
            GameObject.DestroyImmediate(target);
            GameObject.DestroyImmediate(source);
        }

        /// <summary>
        /// 测试取消订阅特定来源
        /// </summary>
        [Test]
        public void UnsubscribeFromSource_ShouldStopInvokingHandler()
        {
            // Arrange
            GameObject target = new GameObject("Target");
            GameObject source = new GameObject("Source");
            int callCount = 0;

            GenericEventBus<IEvent, GameObject>.TargetedEventHandler<TestEvent> handler = 
                (ref TestEvent e, GameObject t, GameObject s) => callCount++;

            xFrameEventBus.SubscribeToSource<TestEvent>(source, handler);

            var testEvent = new TestEvent();

            // Act - 第一次触发
            xFrameEventBus.Raise(testEvent, target, source);
            Assert.AreEqual(1, callCount, "第一次应该被调用");

            // 取消订阅
            xFrameEventBus.UnsubscribeFromSource<TestEvent>(source, handler);

            // 第二次触发
            xFrameEventBus.Raise(testEvent, target, source);

            // Assert
            Assert.AreEqual(1, callCount, "取消订阅后不应该再被调用");

            // Cleanup
            GameObject.DestroyImmediate(target);
            GameObject.DestroyImmediate(source);
        }

        #endregion

        #region 状态检查测试

        /// <summary>
        /// 测试IsEventBeingRaised属性
        /// </summary>
        [Test]
        public void IsEventBeingRaised_ShouldReflectEventState()
        {
            // Arrange
            bool wasRaisingInHandler = false;
            
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => 
            {
                wasRaisingInHandler = xFrameEventBus.IsEventBeingRaised;
            });

            var testEvent = new TestEvent();

            // Act
            bool beforeRaise = xFrameEventBus.IsEventBeingRaised;
            xFrameEventBus.Raise(testEvent);
            bool afterRaise = xFrameEventBus.IsEventBeingRaised;

            // Assert
            Assert.IsFalse(beforeRaise, "触发前不应该有事件正在触发");
            Assert.IsTrue(wasRaisingInHandler, "处理器执行时应该有事件正在触发");
            Assert.IsFalse(afterRaise, "触发后不应该有事件正在触发");
        }

        #endregion

        #region 清理监听器测试

        /// <summary>
        /// 测试清理所有监听器
        /// </summary>
        [Test]
        public void ClearListeners_ShouldRemoveAllHandlers()
        {
            // Arrange
            int count1 = 0, count2 = 0;
            
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => count1++);
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => count2++);

            var testEvent = new TestEvent();

            // Act - 第一次触发
            xFrameEventBus.Raise(testEvent);
            Assert.AreEqual(1, count1, "清理前第一个处理器应该被调用");
            Assert.AreEqual(1, count2, "清理前第二个处理器应该被调用");

            // 清理监听器
            xFrameEventBus.ClearListeners<TestEvent>();

            // 第二次触发
            xFrameEventBus.Raise(testEvent);

            // Assert
            Assert.AreEqual(1, count1, "清理后第一个处理器不应该再被调用");
            Assert.AreEqual(1, count2, "清理后第二个处理器不应该再被调用");
        }

        /// <summary>
        /// 测试清理监听器只影响指定类型
        /// </summary>
        [Test]
        public void ClearListeners_ShouldOnlyAffectSpecificEventType()
        {
            // Arrange
            int testEventCount = 0;
            int anotherEventCount = 0;
            
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => testEventCount++);
            xFrameEventBus.SubscribeTo<AnotherTestEvent>((ref AnotherTestEvent e) => anotherEventCount++);

            // Act
            xFrameEventBus.ClearListeners<TestEvent>();

            xFrameEventBus.Raise(new TestEvent());
            xFrameEventBus.Raise(new AnotherTestEvent());

            // Assert
            Assert.AreEqual(0, testEventCount, "TestEvent的监听器应该被清理");
            Assert.AreEqual(1, anotherEventCount, "AnotherTestEvent的监听器不应该被影响");
        }

        #endregion

        #region 不同事件类型隔离测试

        /// <summary>
        /// 测试不同事件类型的订阅是隔离的
        /// </summary>
        [Test]
        public void DifferentEventTypes_ShouldBeIsolated()
        {
            // Arrange
            int testEventCount = 0;
            int anotherEventCount = 0;
            
            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => testEventCount++);
            xFrameEventBus.SubscribeTo<AnotherTestEvent>((ref AnotherTestEvent e) => anotherEventCount++);

            // Act
            xFrameEventBus.Raise(new TestEvent());
            xFrameEventBus.Raise(new AnotherTestEvent());
            xFrameEventBus.Raise(new TestEvent());

            // Assert
            Assert.AreEqual(2, testEventCount, "TestEvent应该被触发2次");
            Assert.AreEqual(1, anotherEventCount, "AnotherTestEvent应该被触发1次");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 测试事件处理器
        /// </summary>
        private void OnTestEvent(ref TestEvent e)
        {
            _eventCallCount++;
            _lastMessage = e.Message;
            _lastValue = e.Value;
        }

        /// <summary>
        /// 另一个测试事件处理器
        /// </summary>
        private void OnAnotherTestEvent(ref AnotherTestEvent e)
        {
            _eventCallCount++;
            _lastFlag = e.Flag;
        }

        #endregion
    }
}
