using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using xFrame.Runtime.EventBus;

namespace xFrame.Tests.EditMode.EventBusTests
{
    /// <summary>
    /// 线程安全事件总线测试
    /// </summary>
    [TestFixture]
    public class ThreadSafeEventBusTests
    {
        /// <summary>
        /// 测试初始化
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _eventBus = new ThreadSafeEventBus(10);
            _receivedEvents = new List<TestEvent>();
            _eventHandleCount = 0;
        }

        /// <summary>
        /// 测试清理
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _eventBus?.Clear();
            _eventBus?.Dispose();
            _eventBus = null;
            _receivedEvents?.Clear();
        }

        private ThreadSafeEventBus _eventBus;
        private readonly object _lockObject = new();
        private List<TestEvent> _receivedEvents;
        private int _eventHandleCount;

        /// <summary>
        /// 测试多线程并发订阅
        /// </summary>
        [Test]
        public void TestConcurrentSubscription()
        {
            // Arrange
            const int threadCount = 10;
            const int subscriptionsPerThread = 5;
            var subscriptionIds = new List<string>();
            var tasks = new List<Task>();

            // Act
            for (var i = 0; i < threadCount; i++)
            {
                var threadIndex = i;
                var task = Task.Run(() =>
                {
                    for (var j = 0; j < subscriptionsPerThread; j++)
                    {
                        var subscriptionId = _eventBus.Subscribe<TestEvent>(e =>
                        {
                            lock (_lockObject)
                            {
                                _receivedEvents.Add(e);
                                _eventHandleCount++;
                            }
                        });

                        lock (subscriptionIds)
                        {
                            subscriptionIds.Add(subscriptionId);
                        }
                    }
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.AreEqual(threadCount * subscriptionsPerThread, subscriptionIds.Count,
                "所有订阅都应该成功");
            Assert.AreEqual(threadCount * subscriptionsPerThread, _eventBus.GetSubscriberCount<TestEvent>(),
                "订阅者数量应该正确");
        }

        /// <summary>
        /// 测试多线程并发发布
        /// </summary>
        [Test]
        public void TestConcurrentPublishing()
        {
            // Arrange
            const int threadCount = 5;
            const int eventsPerThread = 10;
            const int expectedTotalEvents = threadCount * eventsPerThread;

            _eventBus.Subscribe<TestEvent>(e =>
            {
                lock (_lockObject)
                {
                    _receivedEvents.Add(e);
                    _eventHandleCount++;
                }
            });

            var tasks = new List<Task>();

            // Act
            for (var i = 0; i < threadCount; i++)
            {
                var threadIndex = i;
                var task = Task.Run(() =>
                {
                    for (var j = 0; j < eventsPerThread; j++)
                    {
                        var testEvent = new TestEvent($"Thread-{threadIndex}-Event-{j}");
                        _eventBus.Publish(testEvent);
                    }
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.AreEqual(expectedTotalEvents, _eventHandleCount,
                "所有事件都应该被处理");
            Assert.AreEqual(expectedTotalEvents, _receivedEvents.Count,
                "应该接收到所有事件");
        }

        /// <summary>
        /// 测试多线程异步事件处理
        /// </summary>
        [Test]
        public async Task TestConcurrentAsyncPublishing()
        {
            // Arrange
            const int threadCount = 5;
            const int eventsPerThread = 10;
            const int expectedTotalEvents = threadCount * eventsPerThread;

            _eventBus.SubscribeAsync<TestEvent>(async e =>
            {
                await Task.Delay(1); // 模拟异步操作
                lock (_lockObject)
                {
                    _receivedEvents.Add(e);
                    _eventHandleCount++;
                }
            });

            var tasks = new List<Task>();

            // Act
            for (var i = 0; i < threadCount; i++)
            {
                var threadIndex = i;
                var task = Task.Run(async () =>
                {
                    for (var j = 0; j < eventsPerThread; j++)
                    {
                        var testEvent = new TestEvent($"AsyncThread-{threadIndex}-Event-{j}");
                        await _eventBus.PublishAsync(testEvent);
                    }
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.AreEqual(expectedTotalEvents, _eventHandleCount,
                "所有异步事件都应该被处理");
            Assert.AreEqual(expectedTotalEvents, _receivedEvents.Count,
                "应该接收到所有异步事件");
        }

        /// <summary>
        /// 测试并发订阅和取消订阅
        /// </summary>
        [Test]
        public void TestConcurrentSubscribeAndUnsubscribe()
        {
            // Arrange
            const int operationCount = 100;
            var subscriptionIds = new List<string>();
            var tasks = new List<Task>();
            var random = new Random();

            // Act
            for (var i = 0; i < operationCount; i++)
            {
                var task = Task.Run(() =>
                {
                    if (random.Next(2) == 0) // 50% 概率订阅
                    {
                        var subscriptionId = _eventBus.Subscribe<TestEvent>(e =>
                        {
                            Interlocked.Increment(ref _eventHandleCount);
                        });

                        lock (subscriptionIds)
                        {
                            subscriptionIds.Add(subscriptionId);
                        }
                    }
                    else // 50% 概率取消订阅
                    {
                        string idToUnsubscribe = null;
                        lock (subscriptionIds)
                        {
                            if (subscriptionIds.Count > 0)
                            {
                                var index = random.Next(subscriptionIds.Count);
                                idToUnsubscribe = subscriptionIds[index];
                                subscriptionIds.RemoveAt(index);
                            }
                        }

                        if (idToUnsubscribe != null) _eventBus.Unsubscribe(idToUnsubscribe);
                    }
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            // 发布测试事件
            _eventBus.Publish(new TestEvent("Concurrent Test"));

            // Assert
            var finalSubscriberCount = _eventBus.GetSubscriberCount<TestEvent>();
            Assert.AreEqual(subscriptionIds.Count, finalSubscriberCount,
                "最终订阅者数量应该与记录的订阅ID数量一致");
            Assert.AreEqual(finalSubscriberCount, _eventHandleCount,
                "事件处理次数应该等于订阅者数量");
        }

        /// <summary>
        /// 测试主线程调度功能
        /// </summary>
        [Test]
        public void TestMainThreadScheduling()
        {
            // Arrange
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;
            var handlerThreadId = -1;
            var resetEvent = new ManualResetEventSlim(false);

            // 订阅到主线程执行的事件
            _eventBus.SubscribeOnMainThread<TestEvent>(e =>
            {
                handlerThreadId = Thread.CurrentThread.ManagedThreadId;
                resetEvent.Set();
            });

            // Act
            Task.Run(() => { _eventBus.Publish(new TestEvent("MainThread Test")); });

            // 手动处理主线程队列
            _eventBus.ProcessMainThreadQueue();

            // 等待事件处理完成
            var completed = resetEvent.Wait(TimeSpan.FromSeconds(1));

            // Assert
            Assert.IsTrue(completed, "事件应该在超时时间内被处理");
            // 注意：在单元测试环境中，主线程调度可能不会切换到真正的主线程
            // 这里主要测试功能是否正常工作
        }

        /// <summary>
        /// 测试并发统计信息获取
        /// </summary>
        [Test]
        public void TestConcurrentStatistics()
        {
            // Arrange
            const int threadCount = 10;
            var tasks = new List<Task>();

            // 添加一些订阅者
            for (var i = 0; i < 5; i++) _eventBus.Subscribe<TestEvent>(e => { });

            // Act - 多线程同时获取统计信息
            for (var i = 0; i < threadCount; i++)
            {
                var task = Task.Run(() =>
                {
                    for (var j = 0; j < 10; j++)
                    {
                        var subscriberCount = _eventBus.GetSubscriberCount<TestEvent>();
                        var hasSubscribers = _eventBus.HasSubscribers<TestEvent>();
                        var statistics = _eventBus.GetStatistics();

                        // 基本验证
                        Assert.GreaterOrEqual(subscriberCount, 0, "订阅者数量应该非负");
                        Assert.IsNotNull(statistics, "统计信息不应该为空");
                    }
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.AreEqual(5, _eventBus.GetSubscriberCount<TestEvent>(),
                "最终订阅者数量应该正确");
        }

        /// <summary>
        /// 测试高并发压力
        /// </summary>
        [Test]
        public async Task TestHighConcurrencyStress()
        {
            // Arrange
            const int threadCount = 20;
            const int operationsPerThread = 50;
            var totalOperations = threadCount * operationsPerThread;
            var completedOperations = 0;

            // 添加多个处理器
            for (var i = 0; i < 5; i++)
                _eventBus.Subscribe<TestEvent>(e => { Interlocked.Increment(ref completedOperations); });

            var tasks = new List<Task>();

            // Act - 高并发发布事件
            for (var i = 0; i < threadCount; i++)
            {
                var threadIndex = i;
                var task = Task.Run(async () =>
                {
                    for (var j = 0; j < operationsPerThread; j++)
                    {
                        var testEvent = new TestEvent($"Stress-{threadIndex}-{j}");

                        if (j % 2 == 0)
                            _eventBus.Publish(testEvent);
                        else
                            await _eventBus.PublishAsync(testEvent);
                    }
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            // 等待所有事件处理完成
            var timeout = DateTime.Now.AddSeconds(5);
            while (completedOperations < totalOperations * 5 && DateTime.Now < timeout) await Task.Delay(10);

            // Assert
            Assert.AreEqual(totalOperations * 5, completedOperations,
                "所有事件都应该被所有处理器处理");
        }

        /// <summary>
        /// 测试内存泄漏预防
        /// </summary>
        [Test]
        public void TestMemoryLeakPrevention()
        {
            // Arrange
            const int iterationCount = 100;
            var initialSubscriberCount = _eventBus.GetSubscriberCount<TestEvent>();

            // Act - 重复订阅和取消订阅
            for (var i = 0; i < iterationCount; i++)
            {
                var subscriptionId = _eventBus.Subscribe<TestEvent>(e => { });
                _eventBus.Unsubscribe(subscriptionId);
            }

            // 强制垃圾回收
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Assert
            var finalSubscriberCount = _eventBus.GetSubscriberCount<TestEvent>();
            Assert.AreEqual(initialSubscriberCount, finalSubscriberCount,
                "订阅者数量应该回到初始状态，没有内存泄漏");
        }

        /// <summary>
        /// 测试异常情况下的线程安全
        /// </summary>
        [Test]
        public void TestThreadSafetyWithExceptions()
        {
            // Arrange
            const int threadCount = 10;
            var successfulHandles = 0;
            var tasks = new List<Task>();

            // 添加会抛出异常的处理器
            _eventBus.Subscribe<TestEvent>(e =>
            {
                if (e.Data.Message.Contains("Exception")) throw new InvalidOperationException("Test exception");
                Interlocked.Increment(ref successfulHandles);
            });

            // Act
            for (var i = 0; i < threadCount; i++)
            {
                var threadIndex = i;
                var task = Task.Run(() =>
                {
                    try
                    {
                        // 一半事件会抛出异常
                        var message = threadIndex % 2 == 0 ? "Normal" : "Exception";
                        var testEvent = new TestEvent($"{message}-{threadIndex}");
                        _eventBus.Publish(testEvent);
                    }
                    catch (EventBusException)
                    {
                        // 预期的异常，忽略
                    }
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.AreEqual(threadCount / 2, successfulHandles,
                "只有不抛出异常的事件应该被成功处理");
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
    }
}