using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using xFrame.Runtime.EventBus;
using xFrame.Runtime.EventBus.Events;

namespace xFrame.Tests.EditMode.EventBusTests
{
    /// <summary>
    /// 事件总线核心功能测试
    /// </summary>
    [TestFixture]
    public class EventBusTests
    {
        /// <summary>
        /// 测试初始化
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _eventBus = EventBusFactory.Create();
            _lastReceivedEvent = null;
            _eventHandleCount = 0;
        }

        /// <summary>
        /// 测试清理
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _eventBus?.Clear();
            _eventBus = null;
        }

        private IEventBus _eventBus;
        private TestEvent _lastReceivedEvent;
        private int _eventHandleCount;

        /// <summary>
        /// 测试基础事件订阅和发布
        /// </summary>
        [Test]
        public void TestBasicSubscribeAndPublish()
        {
            // Arrange
            var testEvent = new TestEvent("Test Message");

            // Act
            _eventBus.Subscribe<TestEvent>(OnTestEvent);
            _eventBus.Publish(testEvent);

            // Assert
            Assert.IsNotNull(_lastReceivedEvent, "事件应该被接收");
            Assert.AreEqual(testEvent.Data.Message, _lastReceivedEvent.Data.Message, "事件数据应该匹配");
            Assert.AreEqual(1, _eventHandleCount, "事件应该被处理一次");
        }

        /// <summary>
        /// 测试委托订阅
        /// </summary>
        [Test]
        public void TestDelegateSubscription()
        {
            // Arrange
            var testEvent = new TestEvent("Delegate Test");
            var subscriptionId = _eventBus.Subscribe<TestEvent>(OnTestEvent);

            // Act
            _eventBus.Publish(testEvent);

            // Assert
            Assert.IsNotNull(subscriptionId, "订阅ID不应该为空");
            Assert.AreEqual(1, _eventHandleCount, "事件应该被处理一次");

            // 测试取消订阅
            _eventBus.Unsubscribe(subscriptionId);
            _eventHandleCount = 0;
            _eventBus.Publish(testEvent);

            Assert.AreEqual(0, _eventHandleCount, "取消订阅后事件不应该被处理");
        }

        /// <summary>
        /// 测试事件处理器优先级
        /// </summary>
        [Test]
        public void TestEventHandlerPriority()
        {
            // Arrange
            var executionOrder = new List<int>();
            var testEvent = new TestEvent("Priority Test");

            // 订阅不同优先级的处理器
            _eventBus.Subscribe<TestEvent>(e => executionOrder.Add(1), 1);
            _eventBus.Subscribe<TestEvent>(e => executionOrder.Add(0), 0);
            _eventBus.Subscribe<TestEvent>(e => executionOrder.Add(2), 2);

            // Act
            _eventBus.Publish(testEvent);

            // Assert
            Assert.AreEqual(3, executionOrder.Count, "应该有3个处理器被执行");
            Assert.AreEqual(0, executionOrder[0], "优先级0应该最先执行");
            Assert.AreEqual(1, executionOrder[1], "优先级1应该第二执行");
            Assert.AreEqual(2, executionOrder[2], "优先级2应该最后执行");
        }

        /// <summary>
        /// 测试异步事件处理
        /// </summary>
        [Test]
        public async Task TestAsyncEventHandling()
        {
            // Arrange
            var testEvent = new TestEvent("Async Test");
            var asyncHandled = false;

            _eventBus.SubscribeAsync<TestEvent>(async e =>
            {
                await Task.Delay(10);
                asyncHandled = true;
            });

            // Act
            await _eventBus.PublishAsync(testEvent);

            // Assert
            Assert.IsTrue(asyncHandled, "异步事件应该被处理");
        }

        /// <summary>
        /// 测试事件过滤器
        /// </summary>
        [Test]
        public void TestEventFiltering()
        {
            // Arrange
            var filter = new TestEventFilter(5);
            _eventBus.AddFilter(filter);
            _eventBus.Subscribe<TestEvent>(OnTestEvent);

            var shortEvent = new TestEvent("Hi"); // 长度 < 5，应该被过滤
            var longEvent = new TestEvent("Hello World"); // 长度 >= 5，应该通过

            // Act
            _eventBus.Publish(shortEvent);
            var shortEventCount = _eventHandleCount;

            _eventBus.Publish(longEvent);
            var longEventCount = _eventHandleCount;

            // Assert
            Assert.AreEqual(0, shortEventCount, "短消息应该被过滤掉");
            Assert.AreEqual(1, longEventCount, "长消息应该通过过滤器");
        }

        /// <summary>
        /// 测试事件拦截器
        /// </summary>
        [Test]
        public void TestEventInterception()
        {
            // Arrange
            var interceptor = new TestEventInterceptor();
            _eventBus.AddInterceptor(interceptor);
            _eventBus.Subscribe<TestEvent>(OnTestEvent);

            var testEvent = new TestEvent("Interceptor Test");

            // Act
            _eventBus.Publish(testEvent);

            // Assert
            Assert.IsTrue(interceptor.BeforeHandleCalled, "拦截器的BeforeHandle应该被调用");
            Assert.IsTrue(interceptor.AfterHandleCalled, "拦截器的AfterHandle应该被调用");
            Assert.AreEqual(1, _eventHandleCount, "事件应该被正常处理");
        }

        /// <summary>
        /// 测试批量事件发布
        /// </summary>
        [Test]
        public void TestBatchEventPublishing()
        {
            // Arrange
            _eventBus.Subscribe<TestEvent>(OnTestEvent);

            var events = new[]
            {
                new TestEvent("Event 1"),
                new TestEvent("Event 2"),
                new TestEvent("Event 3")
            };

            // Act
            _eventBus.PublishBatch(events);

            // Assert
            Assert.AreEqual(3, _eventHandleCount, "应该处理3个事件");
        }

        /// <summary>
        /// 测试延迟事件发布
        /// </summary>
        [Test]
        public async Task TestDelayedEventPublishing()
        {
            // Arrange
            _eventBus.Subscribe<TestEvent>(OnTestEvent);
            var testEvent = new TestEvent("Delayed Test");
            var startTime = DateTime.Now;

            // Act
            _eventBus.PublishDelayed(testEvent, 100); // 100ms延迟

            // 等待事件被处理
            await Task.Delay(200);

            // Assert
            Assert.AreEqual(1, _eventHandleCount, "延迟事件应该被处理");
            var elapsed = DateTime.Now - startTime;
            Assert.GreaterOrEqual(elapsed.TotalMilliseconds, 100, "事件应该在延迟后才被处理");
        }

        /// <summary>
        /// 测试订阅者数量统计
        /// </summary>
        [Test]
        public void TestSubscriberCount()
        {
            // Arrange & Act
            Assert.AreEqual(0, _eventBus.GetSubscriberCount<TestEvent>(), "初始订阅者数量应该为0");
            Assert.IsFalse(_eventBus.HasSubscribers<TestEvent>(), "初始状态不应该有订阅者");

            var sub1 = _eventBus.Subscribe<TestEvent>(OnTestEvent);
            Assert.AreEqual(1, _eventBus.GetSubscriberCount<TestEvent>(), "添加一个订阅者后数量应该为1");
            Assert.IsTrue(_eventBus.HasSubscribers<TestEvent>(), "应该有订阅者");

            var sub2 = _eventBus.Subscribe<TestEvent>(OnTestEvent);
            Assert.AreEqual(2, _eventBus.GetSubscriberCount<TestEvent>(), "添加两个订阅者后数量应该为2");

            _eventBus.Unsubscribe(sub1);
            Assert.AreEqual(1, _eventBus.GetSubscriberCount<TestEvent>(), "取消一个订阅者后数量应该为1");

            _eventBus.UnsubscribeAll<TestEvent>();
            Assert.AreEqual(0, _eventBus.GetSubscriberCount<TestEvent>(), "取消所有订阅者后数量应该为0");
            Assert.IsFalse(_eventBus.HasSubscribers<TestEvent>(), "不应该有订阅者");
        }

        /// <summary>
        /// 测试事件历史记录
        /// </summary>
        [Test]
        public void TestEventHistory()
        {
            // Arrange
            _eventBus.SetHistoryEnabled(true);

            var event1 = new TestEvent("History 1");
            var event2 = new TestEvent("History 2");
            var event3 = new TestEvent("History 3");

            // Act
            _eventBus.Publish(event1);
            _eventBus.Publish(event2);
            _eventBus.Publish(event3);

            var history = _eventBus.GetEventHistory<TestEvent>(2);
            var historyList = new List<TestEvent>(history);

            // Assert
            Assert.AreEqual(2, historyList.Count, "应该返回最新的2个事件");
            Assert.AreEqual("History 1", historyList[0].Data.Message, "第一个历史事件应该匹配");
            Assert.AreEqual("History 2", historyList[1].Data.Message, "第二个历史事件应该匹配");
        }

        /// <summary>
        /// 测试事件取消
        /// </summary>
        [Test]
        public void TestEventCancellation()
        {
            // Arrange
            var handlerCallCount = 0;

            _eventBus.Subscribe<TestEvent>(e =>
            {
                handlerCallCount++;
                e.IsCancelled = true; // 取消事件
            });

            _eventBus.Subscribe<TestEvent>(e => { handlerCallCount++; }, 1);

            var testEvent = new TestEvent("Cancel Test");

            // Act
            _eventBus.Publish(testEvent);

            // Assert
            Assert.AreEqual(1, handlerCallCount, "事件被取消后，后续处理器不应该被调用");
            Assert.IsTrue(testEvent.IsCancelled, "事件应该被标记为已取消");
        }

        /// <summary>
        /// 测试异常处理
        /// </summary>
        [Test]
        public void TestExceptionHandling()
        {
            // Arrange
            var exceptionThrown = false;

            _eventBus.Subscribe<TestEvent>(e => { throw new InvalidOperationException("Test exception"); });

            var testEvent = new TestEvent("Exception Test");

            // Act & Assert
            Assert.Throws<EventBusException>(() =>
            {
                try
                {
                    _eventBus.Publish(testEvent);
                }
                catch (EventBusException)
                {
                    exceptionThrown = true;
                    throw;
                }
            });

            Assert.IsTrue(exceptionThrown, "应该抛出EventBusException");
        }

        /// <summary>
        /// 测试清空功能
        /// </summary>
        [Test]
        public void TestClear()
        {
            // Arrange
            _eventBus.Subscribe<TestEvent>(OnTestEvent);
            _eventBus.Subscribe<LogEvent>(e => { });

            Assert.IsTrue(_eventBus.HasSubscribers<TestEvent>(), "应该有TestEvent订阅者");
            Assert.IsTrue(_eventBus.HasSubscribers<LogEvent>(), "应该有LogEvent订阅者");

            // Act
            _eventBus.Clear();

            // Assert
            Assert.IsFalse(_eventBus.HasSubscribers<TestEvent>(), "清空后不应该有TestEvent订阅者");
            Assert.IsFalse(_eventBus.HasSubscribers<LogEvent>(), "清空后不应该有LogEvent订阅者");
        }

        /// <summary>
        /// 测试事件处理方法
        /// </summary>
        private void OnTestEvent(TestEvent eventData)
        {
            _lastReceivedEvent = eventData;
            _eventHandleCount++;
        }

        /// <summary>
        /// 测试事件类
        /// </summary>
        public class TestEvent : BaseEvent<TestEventData>
        {
            public TestEvent(string message)
            {
                Data = new TestEventData { Message = message };
            }
        }

        /// <summary>
        /// 测试事件数据
        /// </summary>
        public class TestEventData
        {
            public string Message { get; set; }
        }

        /// <summary>
        /// 测试事件过滤器
        /// </summary>
        public class TestEventFilter : IEventFilter<TestEvent>
        {
            private readonly int _minLength;

            public TestEventFilter(int minLength)
            {
                _minLength = minLength;
            }

            public bool ShouldHandle(TestEvent eventData)
            {
                return eventData.Data.Message?.Length >= _minLength;
            }
        }

        /// <summary>
        /// 测试事件拦截器
        /// </summary>
        public class TestEventInterceptor : IEventInterceptor<TestEvent>
        {
            public bool BeforeHandleCalled { get; private set; }
            public bool AfterHandleCalled { get; private set; }
            public bool ExceptionHandleCalled { get; private set; }
            public int Priority => 0;

            public bool OnBeforeHandle(TestEvent eventData)
            {
                BeforeHandleCalled = true;
                return true;
            }

            public void OnAfterHandle(TestEvent eventData)
            {
                AfterHandleCalled = true;
            }

            public bool OnException(TestEvent eventData, Exception exception)
            {
                ExceptionHandleCalled = true;
                return false;
            }
        }
    }
}