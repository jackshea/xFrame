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

        #region 边界情况和高级测试

        /// <summary>
        /// 测试事件参数可以被修改并传递给后续处理器
        /// </summary>
        [Test]
        public void EventParameterModification_ShouldPropagateToNextHandler()
        {
            // Arrange
            var capturedValue1 = 0;
            var capturedValue2 = 0;

            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) =>
            {
                capturedValue1 = e.Value;
                e.Value = 999; // 修改参数
            }, priority: 10);

            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) =>
            {
                capturedValue2 = e.Value;
            }, priority: 5);

            var testEvent = new TestEvent { Message = "Test", Value = 100 };

            // Act
            xFrameEventBus.Raise(testEvent);

            // Assert
            Assert.AreEqual(100, capturedValue1, "第一个处理器应该收到原始值");
            Assert.AreEqual(999, capturedValue2, "第二个处理器应该收到修改后的值");
        }

        /// <summary>
        /// 测试嵌套事件触发
        /// </summary>
        [Test]
        public void NestedEventRaising_ShouldWorkCorrectly()
        {
            // Arrange
            int outerCount = 0;
            int innerCount = 0;
            string executionOrder = "";

            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) =>
            {
                outerCount++;
                executionOrder += "O1";

                // 在处理器内部触发另一个事件
                xFrameEventBus.Raise(new AnotherTestEvent { Flag = true });
            }, priority: 10);

            xFrameEventBus.SubscribeTo<AnotherTestEvent>((ref AnotherTestEvent e) =>
            {
                innerCount++;
                executionOrder += "I1";
            });

            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) =>
            {
                outerCount++;
                executionOrder += "O2";
            }, priority: 0);

            // Act
            xFrameEventBus.Raise(new TestEvent());

            // Assert
            Assert.AreEqual(2, outerCount, "外部事件应该被触发2次");
            Assert.AreEqual(1, innerCount, "内部事件应该被触发1次");
            Assert.AreEqual("O1I1O2", executionOrder, "执行顺序应该正确");
        }

        /// <summary>
        /// 测试重复订阅同一处理器
        /// </summary>
        [Test]
        public void DuplicateSubscription_ShouldCallHandlerMultipleTimes()
        {
            // Arrange
            int callCount = 0;
            GenericEventBus<IEvent>.EventHandler<TestEvent> handler = (ref TestEvent e) => callCount++;

            // 重复订阅同一处理器
            xFrameEventBus.SubscribeTo<TestEvent>(handler);
            xFrameEventBus.SubscribeTo<TestEvent>(handler);
            xFrameEventBus.SubscribeTo<TestEvent>(handler);

            var testEvent = new TestEvent();

            // Act
            xFrameEventBus.Raise(testEvent);

            // Assert
            Assert.AreEqual(3, callCount, "重复订阅的处理器应该被调用3次");
        }

        /// <summary>
        /// 测试取消不存在的处理器不应该抛出异常
        /// </summary>
        [Test]
        public void UnsubscribeNonExistentHandler_ShouldNotThrow()
        {
            // Arrange
            GenericEventBus<IEvent>.EventHandler<TestEvent> handler = (ref TestEvent e) => { };
            var testEvent = new TestEvent();

            // Act & Assert - 不应该抛出异常
            Assert.DoesNotThrow(() =>
            {
                xFrameEventBus.UnsubscribeFrom<TestEvent>(handler);
                xFrameEventBus.Raise(testEvent);
            }, "取消不存在的处理器不应该抛出异常");
        }

        /// <summary>
        /// 测试大量订阅者的性能
        /// </summary>
        [Test]
        public void LargeNumberOfSubscribers_ShouldPerformReasonably()
        {
            // Arrange
            const int subscriberCount = 1000;
            int callCount = 0;

            for (int i = 0; i < subscriberCount; i++)
            {
                xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) => callCount++);
            }

            var testEvent = new TestEvent();

            // Act
            xFrameEventBus.Raise(testEvent);

            // Assert
            Assert.AreEqual(subscriberCount, callCount, $"所有 {subscriberCount} 个订阅者都应该被调用");
        }

        /// <summary>
        /// 测试在处理器中订阅新处理器
        /// </summary>
        [Test]
        public void SubscribeDuringEventProcessing_ShouldNotAffectCurrentEvent()
        {
            // Arrange
            int initialCount = 0;
            int newCount = 0;

            xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent e) =>
            {
                initialCount++;
                // 在处理器中订阅新处理器
                xFrameEventBus.SubscribeTo<TestEvent>((ref TestEvent ev) => newCount++);
            });

            var testEvent = new TestEvent();

            // Act - 第一次触发
            xFrameEventBus.Raise(testEvent);
            // Assert - 初始处理器应该被调用，新处理器不应该被调用
            Assert.AreEqual(1, initialCount, "初始处理器应该被调用");
            Assert.AreEqual(0, newCount, "在处理期间订阅的新处理器不应该被调用");

            // 第二次触发
            xFrameEventBus.Raise(testEvent);
            // Assert - 现在两个处理器都应该被调用
            Assert.AreEqual(2, initialCount, "初始处理器应该被调用2次");
            Assert.AreEqual(1, newCount, "新处理器应该被调用");
        }

        /// <summary>
        /// 测试在处理器中取消订阅当前处理器
        /// </summary>
        [Test]
        public void UnsubscribeSelfDuringProcessing_ShouldCompleteExecution()
        {
            // Arrange
            int callCount = 0;
            GenericEventBus<IEvent>.EventHandler<TestEvent> handler = null;

            handler = (ref TestEvent e) =>
            {
                callCount++;
                xFrameEventBus.UnsubscribeFrom<TestEvent>(handler);
            };

            xFrameEventBus.SubscribeTo<TestEvent>(handler);
            var testEvent = new TestEvent();

            // Act
            xFrameEventBus.Raise(testEvent);

            // Assert
            Assert.AreEqual(1, callCount, "处理器应该被调用一次");

            // 第二次触发不应该调用
            xFrameEventBus.Raise(testEvent);
            Assert.AreEqual(1, callCount, "取消订阅后不应该再被调用");
        }

        /// <summary>
        /// 测试目标和来源同时匹配
        /// </summary>
        [Test]
        public void SubscribeToBothTargetAndSource_ShouldMatchCorrectly()
        {
            // Arrange
            GameObject target = new GameObject("Target");
            GameObject source = new GameObject("Source");
            GameObject other = new GameObject("Other");

            int targetCount = 0;
            int sourceCount = 0;
            int bothCount = 0;

            xFrameEventBus.SubscribeToTarget<TestEvent>(target,
                (ref TestEvent e, GameObject t, GameObject s) => targetCount++);

            xFrameEventBus.SubscribeToSource<TestEvent>(source,
                (ref TestEvent e, GameObject t, GameObject s) => sourceCount++);

            var testEvent = new TestEvent();

            // Act 1: 匹配目标但不匹配来源
            xFrameEventBus.Raise(testEvent, target, other);
            Assert.AreEqual(1, targetCount, "目标订阅者应该被调用");
            Assert.AreEqual(0, sourceCount, "来源订阅者不应该被调用");

            // Act 2: 匹配来源但不匹配目标
            xFrameEventBus.Raise(testEvent, other, source);
            Assert.AreEqual(1, targetCount, "目标订阅者应该仍为1");
            Assert.AreEqual(1, sourceCount, "来源订阅者应该被调用");

            // Act 3: 同时匹配目标和来源
            xFrameEventBus.Raise(testEvent, target, source);
            Assert.AreEqual(2, targetCount, "目标订阅者应该被调用2次");
            Assert.AreEqual(2, sourceCount, "来源订阅者应该被调用2次");

            // Cleanup
            GameObject.DestroyImmediate(target);
            GameObject.DestroyImmediate(source);
            GameObject.DestroyImmediate(other);
        }

        /// <summary>
        /// 测试带有目标的立即触发事件
        /// </summary>
        [Test]
        public void RaiseImmediatelyWithTargetAndSource_ShouldWork()
        {
            // Arrange
            GameObject target = new GameObject("Target");
            GameObject source = new GameObject("Source");
            int callCount = 0;

            xFrameEventBus.SubscribeTo<TestEvent>(
                (ref TestEvent e, GameObject t, GameObject s) => callCount++);

            var testEvent = new TestEvent { Message = "Immediate" };

            // Act
            xFrameEventBus.RaiseImmediately(testEvent, target, source);

            // Assert
            Assert.AreEqual(1, callCount, "处理器应该被立即调用");

            // Cleanup
            GameObject.DestroyImmediate(target);
            GameObject.DestroyImmediate(source);
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
