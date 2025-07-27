using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using xFrame.Core.EventBus;
using xFrame.Core.EventBus.Events;

namespace xFrame.Tests.EditMode.EventBusTests
{
    /// <summary>
    /// 事件总线管理器测试
    /// </summary>
    [TestFixture]
    public class EventBusManagerTests
    {
        private EventBusManager _manager;
        
        /// <summary>
        /// 测试初始化
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            // 创建新的管理器实例用于测试
            _manager = EventBusManager.Instance;
            _manager.ClearAll(); // 清空所有现有的事件总线
        }
        
        /// <summary>
        /// 测试清理
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _manager?.ClearAll();
        }
        
        /// <summary>
        /// 测试单例模式
        /// </summary>
        [Test]
        public void TestSingletonPattern()
        {
            // Act
            var instance1 = EventBusManager.Instance;
            var instance2 = EventBusManager.Instance;
            
            // Assert
            Assert.AreSame(instance1, instance2, "EventBusManager应该是单例");
        }
        
        /// <summary>
        /// 测试预定义事件总线
        /// </summary>
        [Test]
        public void TestPredefinedEventBuses()
        {
            // Assert
            Assert.IsNotNull(_manager.Default, "Default事件总线应该存在");
            Assert.IsNotNull(_manager.Global, "Global事件总线应该存在");
            Assert.IsNotNull(_manager.UI, "UI事件总线应该存在");
            Assert.IsNotNull(_manager.Game, "Game事件总线应该存在");
            Assert.IsNotNull(_manager.Network, "Network事件总线应该存在");
            
            // 验证它们是不同的实例
            Assert.AreNotSame(_manager.Default, _manager.Global, "不同的事件总线应该是不同的实例");
            Assert.AreNotSame(_manager.UI, _manager.Game, "不同的事件总线应该是不同的实例");
        }
        
        /// <summary>
        /// 测试注册自定义事件总线
        /// </summary>
        [Test]
        public void TestRegisterCustomEventBus()
        {
            // Arrange
            var customBus = EventBusFactory.Create();
            const string busName = "CustomTest";
            
            // Act
            var registered = _manager.RegisterEventBus(busName, customBus);
            var retrievedBus = _manager.GetEventBus(busName);
            
            // Assert
            Assert.IsTrue(registered, "自定义事件总线应该注册成功");
            Assert.AreSame(customBus, retrievedBus, "检索到的事件总线应该是注册的实例");
            Assert.IsTrue(_manager.ContainsEventBus(busName), "管理器应该包含注册的事件总线");
        }
        
        /// <summary>
        /// 测试使用配置注册事件总线
        /// </summary>
        [Test]
        public void TestRegisterEventBusWithConfig()
        {
            // Arrange
            const string busName = "ConfigTest";
            var config = EventBusConfig.HighPerformance;
            
            // Act
            var registered = _manager.RegisterEventBus(busName, config);
            var retrievedBus = _manager.GetEventBus(busName);
            
            // Assert
            Assert.IsTrue(registered, "使用配置注册应该成功");
            Assert.IsNotNull(retrievedBus, "应该能检索到注册的事件总线");
        }
        
        /// <summary>
        /// 测试使用构建器注册事件总线
        /// </summary>
        [Test]
        public void TestRegisterEventBusWithBuilder()
        {
            // Arrange
            const string busName = "BuilderTest";
            
            // Act
            var registered = _manager.RegisterEventBus(busName, builder =>
                builder.WithThreadSafety(true)
                       .WithMaxConcurrentAsync(5)
                       .WithHistory(false));
            
            var retrievedBus = _manager.GetEventBus(busName);
            
            // Assert
            Assert.IsTrue(registered, "使用构建器注册应该成功");
            Assert.IsNotNull(retrievedBus, "应该能检索到注册的事件总线");
        }
        
        /// <summary>
        /// 测试重复注册
        /// </summary>
        [Test]
        public void TestDuplicateRegistration()
        {
            // Arrange
            const string busName = "DuplicateTest";
            var bus1 = EventBusFactory.Create();
            var bus2 = EventBusFactory.Create();
            
            // Act
            var firstRegistration = _manager.RegisterEventBus(busName, bus1);
            var secondRegistration = _manager.RegisterEventBus(busName, bus2);
            var retrievedBus = _manager.GetEventBus(busName);
            
            // Assert
            Assert.IsTrue(firstRegistration, "第一次注册应该成功");
            Assert.IsFalse(secondRegistration, "重复注册应该失败");
            Assert.AreSame(bus1, retrievedBus, "应该保留第一次注册的实例");
        }
        
        /// <summary>
        /// 测试取消注册事件总线
        /// </summary>
        [Test]
        public void TestUnregisterEventBus()
        {
            // Arrange
            const string busName = "UnregisterTest";
            var customBus = EventBusFactory.Create();
            _manager.RegisterEventBus(busName, customBus);
            
            // Act
            var unregistered = _manager.UnregisterEventBus(busName);
            var retrievedBus = _manager.GetEventBus(busName);
            
            // Assert
            Assert.IsTrue(unregistered, "取消注册应该成功");
            Assert.IsNull(retrievedBus, "取消注册后应该无法检索到事件总线");
            Assert.IsFalse(_manager.ContainsEventBus(busName), "管理器不应该包含已取消注册的事件总线");
        }
        
        /// <summary>
        /// 测试获取或创建事件总线
        /// </summary>
        [Test]
        public void TestGetOrCreateEventBus()
        {
            // Arrange
            const string busName = "GetOrCreateTest";
            
            // Act
            var bus1 = _manager.GetOrCreateEventBus(busName);
            var bus2 = _manager.GetOrCreateEventBus(busName);
            
            // Assert
            Assert.IsNotNull(bus1, "应该创建新的事件总线");
            Assert.AreSame(bus1, bus2, "第二次调用应该返回相同的实例");
            Assert.IsTrue(_manager.ContainsEventBus(busName), "管理器应该包含创建的事件总线");
        }
        
        /// <summary>
        /// 测试使用自定义工厂获取或创建事件总线
        /// </summary>
        [Test]
        public void TestGetOrCreateEventBusWithFactory()
        {
            // Arrange
            const string busName = "FactoryTest";
            var factoryCalled = false;
            
            // Act
            var bus = _manager.GetOrCreateEventBus(busName, () =>
            {
                factoryCalled = true;
                return EventBusFactory.CreateHighPerformance();
            });
            
            // Assert
            Assert.IsNotNull(bus, "应该创建新的事件总线");
            Assert.IsTrue(factoryCalled, "自定义工厂应该被调用");
        }
        
        /// <summary>
        /// 测试广播事件到所有总线
        /// </summary>
        [Test]
        public void TestBroadcastToAll()
        {
            // Arrange
            var receivedEvents = new System.Collections.Generic.List<LogEvent>();
            
            // 为多个事件总线添加处理器
            _manager.Default.Subscribe<LogEvent>(e => receivedEvents.Add(e));
            _manager.UI.Subscribe<LogEvent>(e => receivedEvents.Add(e));
            _manager.Game.Subscribe<LogEvent>(e => receivedEvents.Add(e));
            
            var testEvent = new LogEvent("Broadcast Test", LogLevel.Info);
            
            // Act
            _manager.BroadcastToAll(testEvent);
            
            // Assert
            Assert.GreaterOrEqual(receivedEvents.Count, 3, "事件应该被广播到多个总线");
        }
        
        /// <summary>
        /// 测试排除特定总线的广播
        /// </summary>
        [Test]
        public void TestBroadcastWithExclusion()
        {
            // Arrange
            var defaultReceived = false;
            var uiReceived = false;
            var gameReceived = false;
            
            _manager.Default.Subscribe<LogEvent>(e => defaultReceived = true);
            _manager.UI.Subscribe<LogEvent>(e => uiReceived = true);
            _manager.Game.Subscribe<LogEvent>(e => gameReceived = true);
            
            var testEvent = new LogEvent("Exclusion Test", LogLevel.Info);
            
            // Act
            _manager.BroadcastToAll(testEvent, EventBusManager.UIBusName);
            
            // Assert
            Assert.IsTrue(defaultReceived, "Default总线应该接收到事件");
            Assert.IsFalse(uiReceived, "UI总线应该被排除");
            Assert.IsTrue(gameReceived, "Game总线应该接收到事件");
        }
        

        
        /// <summary>
        /// 测试扩展方法
        /// </summary>
        [Test]
        public void TestExtensionMethods()
        {
            // Arrange
            var eventReceived = false;
            const string busName = "ExtensionTest";
            
            _manager.RegisterEventBus(busName, EventBusFactory.Create());
            var subscriptionId = _manager.Subscribe<LogEvent>(busName, e => eventReceived = true);
            
            var testEvent = new LogEvent("Extension Test", LogLevel.Info);
            
            // Act
            _manager.Publish(busName, testEvent);
            
            // Assert
            Assert.IsTrue(eventReceived, "使用扩展方法发布的事件应该被接收");
            Assert.IsNotNull(subscriptionId, "扩展方法订阅应该返回订阅ID");
            
            // 测试取消订阅
            _manager.Unsubscribe(busName, subscriptionId);
            eventReceived = false;
            _manager.Publish(busName, testEvent);
            
            Assert.IsFalse(eventReceived, "取消订阅后事件不应该被接收");
        }
        
        /// <summary>
        /// 测试统计信息
        /// </summary>
        [Test]
        public void TestStatistics()
        {
            // Arrange
            _manager.Default.Subscribe<LogEvent>(e => { });
            _manager.UI.Subscribe<LogEvent>(e => { });
            _manager.Default.Publish(new LogEvent("Stats Test", LogLevel.Info));
            
            // Act
            var allStats = _manager.GetAllStatistics();
            var managerStats = _manager.GetManagerStatistics();
            
            // Assert
            Assert.IsNotNull(allStats, "所有统计信息不应该为空");
            Assert.Greater(allStats.Count, 0, "应该有统计信息");
            Assert.IsNotNull(managerStats, "管理器统计信息不应该为空");
            Assert.IsTrue(managerStats.Contains("EventBusManager"), "管理器统计信息应该包含标识");
        }
        
        /// <summary>
        /// 测试事件总线名称列表
        /// </summary>
        [Test]
        public void TestBusNames()
        {
            // Arrange
            const string customBusName = "CustomBusNameTest";
            _manager.RegisterEventBus(customBusName, EventBusFactory.Create());
            
            // Act
            var busNames = _manager.BusNames.ToList();
            
            // Assert
            Assert.Contains(EventBusManager.DefaultBusName, busNames, "应该包含默认总线名称");
            Assert.Contains(EventBusManager.GlobalBusName, busNames, "应该包含全局总线名称");
            Assert.Contains(customBusName, busNames, "应该包含自定义总线名称");
        }
        
        /// <summary>
        /// 测试清空所有总线
        /// </summary>
        [Test]
        public void TestClearAll()
        {
            // Arrange
            _manager.Default.Subscribe<LogEvent>(e => { });
            _manager.UI.Subscribe<LogEvent>(e => { });
            
            Assert.IsTrue(_manager.Default.HasSubscribers<LogEvent>(), "Default总线应该有订阅者");
            Assert.IsTrue(_manager.UI.HasSubscribers<LogEvent>(), "UI总线应该有订阅者");
            
            // Act
            _manager.ClearAll();
            
            // Assert
            Assert.IsFalse(_manager.Default.HasSubscribers<LogEvent>(), "清空后Default总线不应该有订阅者");
            Assert.IsFalse(_manager.UI.HasSubscribers<LogEvent>(), "清空后UI总线不应该有订阅者");
        }
        
        /// <summary>
        /// 测试参数验证
        /// </summary>
        [Test]
        public void TestParameterValidation()
        {
            // 测试空名称
            Assert.Throws<ArgumentException>(() => _manager.RegisterEventBus("", EventBusFactory.Create()));
            Assert.Throws<ArgumentException>(() => _manager.RegisterEventBus(null, EventBusFactory.Create()));
            
            // 测试空事件总线
            Assert.Throws<ArgumentNullException>(() => _manager.RegisterEventBus("Test", (IEventBus)null));
            
            // 测试空配置
            Assert.Throws<ArgumentNullException>(() => _manager.RegisterEventBus("Test", (EventBusConfig)null));
            
            // 测试空构建器操作
            Assert.Throws<ArgumentNullException>(() => _manager.RegisterEventBus("Test", (Action<EventBusBuilder>)null));
            
            // 测试空事件数据
            Assert.Throws<ArgumentNullException>(() => _manager.BroadcastToAll<LogEvent>(null));
        }
    }
}
