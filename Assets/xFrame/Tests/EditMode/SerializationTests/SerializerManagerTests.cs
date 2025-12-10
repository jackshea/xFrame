using System;
using NUnit.Framework;
using xFrame.Runtime.EventBus;
using xFrame.Runtime.Serialization;

namespace xFrame.Tests.SerializationTests
{
    /// <summary>
    /// 序列化管理器单元测试
    /// 测试序列化管理器的核心功能
    /// </summary>
    [TestFixture]
    public class SerializerManagerTests
    {
        /// <summary>
        /// 测试用的简单数据类
        /// </summary>
        [Serializable]
        private class TestData
        {
            public string Name;
            public int Value;
            public float Score;
        }

        /// <summary>
        /// 测试用的嵌套数据类
        /// </summary>
        [Serializable]
        private class NestedTestData
        {
            public string Title;
            public TestData Inner;
        }

        private ISerializerManager _serializerManager;
        private int _registeredEventCount;
        private int _unregisteredEventCount;
        private int _defaultChangedEventCount;

        [SetUp]
        public void SetUp()
        {
            _serializerManager = new SerializerManager();
            _registeredEventCount = 0;
            _unregisteredEventCount = 0;
            _defaultChangedEventCount = 0;

            // 清理事件监听器
            xFrameEventBus.ClearListeners<SerializerRegisteredEvent>();
            xFrameEventBus.ClearListeners<SerializerUnregisteredEvent>();
            xFrameEventBus.ClearListeners<DefaultSerializerChangedEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            // 清理事件监听器
            xFrameEventBus.ClearListeners<SerializerRegisteredEvent>();
            xFrameEventBus.ClearListeners<SerializerUnregisteredEvent>();
            xFrameEventBus.ClearListeners<DefaultSerializerChangedEvent>();
        }

        #region 基础功能测试

        /// <summary>
        /// 测试默认序列化器是否正确初始化
        /// </summary>
        [Test]
        public void DefaultSerializer_ShouldBeJsonSerializer()
        {
            // Assert
            Assert.IsNotNull(_serializerManager.DefaultSerializer, "默认序列化器不应为空");
            Assert.AreEqual(JsonSerializer.SerializerName, _serializerManager.DefaultSerializer.Name, 
                "默认序列化器应该是JSON序列化器");
        }

        /// <summary>
        /// 测试获取已注册的序列化器
        /// </summary>
        [Test]
        public void GetSerializer_WithValidName_ShouldReturnSerializer()
        {
            // Act
            var serializer = _serializerManager.GetSerializer(JsonSerializer.SerializerName);

            // Assert
            Assert.IsNotNull(serializer, "应该能获取到JSON序列化器");
            Assert.AreEqual(JsonSerializer.SerializerName, serializer.Name);
        }

        /// <summary>
        /// 测试获取未注册的序列化器
        /// </summary>
        [Test]
        public void GetSerializer_WithInvalidName_ShouldReturnNull()
        {
            // Act
            var serializer = _serializerManager.GetSerializer("NonExistent");

            // Assert
            Assert.IsNull(serializer, "未注册的序列化器应该返回null");
        }

        /// <summary>
        /// 测试注册新的序列化器
        /// </summary>
        [Test]
        public void RegisterSerializer_ShouldAddSerializer()
        {
            // Arrange
            var customSerializer = new JsonSerializer(true);

            // Act
            _serializerManager.RegisterSerializer("CustomJson", customSerializer);
            var retrieved = _serializerManager.GetSerializer("CustomJson");

            // Assert
            Assert.IsNotNull(retrieved, "应该能获取到注册的序列化器");
            Assert.AreSame(customSerializer, retrieved, "获取到的应该是同一个实例");
        }

        /// <summary>
        /// 测试注销序列化器
        /// </summary>
        [Test]
        public void UnregisterSerializer_ShouldRemoveSerializer()
        {
            // Arrange
            var customSerializer = new JsonSerializer();
            _serializerManager.RegisterSerializer("ToRemove", customSerializer);

            // Act
            var result = _serializerManager.UnregisterSerializer("ToRemove");
            var retrieved = _serializerManager.GetSerializer("ToRemove");

            // Assert
            Assert.IsTrue(result, "注销应该成功");
            Assert.IsNull(retrieved, "注销后应该无法获取到序列化器");
        }

        /// <summary>
        /// 测试设置默认序列化器
        /// </summary>
        [Test]
        public void SetDefaultSerializer_ShouldChangeDefault()
        {
            // Arrange
            var customSerializer = new JsonSerializer(true);
            _serializerManager.RegisterSerializer("CustomJson", customSerializer);

            // Act
            _serializerManager.SetDefaultSerializer("CustomJson");

            // Assert
            Assert.AreSame(customSerializer, _serializerManager.DefaultSerializer, 
                "默认序列化器应该被更改");
        }

        /// <summary>
        /// 测试设置未注册的序列化器为默认时抛出异常
        /// </summary>
        [Test]
        public void SetDefaultSerializer_WithInvalidName_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                _serializerManager.SetDefaultSerializer("NonExistent"));
        }

        #endregion

        #region 序列化测试

        /// <summary>
        /// 测试使用默认序列化器序列化对象为字符串
        /// </summary>
        [Test]
        public void SerializeToString_WithDefaultSerializer_ShouldWork()
        {
            // Arrange
            var data = new TestData { Name = "Test", Value = 42, Score = 3.14f };

            // Act
            var json = _serializerManager.SerializeToString(data);

            // Assert
            Assert.IsNotNull(json, "序列化结果不应为空");
            Assert.IsTrue(json.Contains("Test"), "JSON应该包含Name字段值");
            Assert.IsTrue(json.Contains("42"), "JSON应该包含Value字段值");
        }

        /// <summary>
        /// 测试使用默认序列化器序列化对象为字节数组
        /// </summary>
        [Test]
        public void Serialize_WithDefaultSerializer_ShouldWork()
        {
            // Arrange
            var data = new TestData { Name = "Test", Value = 42, Score = 3.14f };

            // Act
            var bytes = _serializerManager.Serialize(data);

            // Assert
            Assert.IsNotNull(bytes, "序列化结果不应为空");
            Assert.IsTrue(bytes.Length > 0, "序列化结果应该有内容");
        }

        /// <summary>
        /// 测试使用指定序列化器序列化对象
        /// </summary>
        [Test]
        public void SerializeToString_WithSpecificSerializer_ShouldWork()
        {
            // Arrange
            var data = new TestData { Name = "Specific", Value = 100, Score = 2.5f };

            // Act
            var json = _serializerManager.SerializeToString(JsonSerializer.SerializerName, data);

            // Assert
            Assert.IsNotNull(json, "序列化结果不应为空");
            Assert.IsTrue(json.Contains("Specific"), "JSON应该包含Name字段值");
        }

        /// <summary>
        /// 测试使用未注册的序列化器时抛出异常
        /// </summary>
        [Test]
        public void SerializeToString_WithInvalidSerializer_ShouldThrowException()
        {
            // Arrange
            var data = new TestData { Name = "Test", Value = 42, Score = 3.14f };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                _serializerManager.SerializeToString("NonExistent", data));
        }

        #endregion

        #region 反序列化测试

        /// <summary>
        /// 测试使用默认序列化器从字符串反序列化
        /// </summary>
        [Test]
        public void DeserializeFromString_WithDefaultSerializer_ShouldWork()
        {
            // Arrange
            var json = "{\"Name\":\"Test\",\"Value\":42,\"Score\":3.14}";

            // Act
            var data = _serializerManager.DeserializeFromString<TestData>(json);

            // Assert
            Assert.IsNotNull(data, "反序列化结果不应为空");
            Assert.AreEqual("Test", data.Name);
            Assert.AreEqual(42, data.Value);
            Assert.AreEqual(3.14f, data.Score, 0.01f);
        }

        /// <summary>
        /// 测试使用默认序列化器从字节数组反序列化
        /// </summary>
        [Test]
        public void Deserialize_WithDefaultSerializer_ShouldWork()
        {
            // Arrange
            var data = new TestData { Name = "ByteTest", Value = 99, Score = 1.5f };
            var bytes = _serializerManager.Serialize(data);

            // Act
            var result = _serializerManager.Deserialize<TestData>(bytes);

            // Assert
            Assert.IsNotNull(result, "反序列化结果不应为空");
            Assert.AreEqual(data.Name, result.Name);
            Assert.AreEqual(data.Value, result.Value);
            Assert.AreEqual(data.Score, result.Score, 0.01f);
        }

        /// <summary>
        /// 测试序列化和反序列化的往返一致性
        /// </summary>
        [Test]
        public void SerializeAndDeserialize_ShouldBeConsistent()
        {
            // Arrange
            var original = new TestData { Name = "RoundTrip", Value = 123, Score = 9.99f };

            // Act
            var json = _serializerManager.SerializeToString(original);
            var restored = _serializerManager.DeserializeFromString<TestData>(json);

            // Assert
            Assert.AreEqual(original.Name, restored.Name);
            Assert.AreEqual(original.Value, restored.Value);
            Assert.AreEqual(original.Score, restored.Score, 0.01f);
        }

        /// <summary>
        /// 测试嵌套对象的序列化和反序列化
        /// </summary>
        [Test]
        public void SerializeAndDeserialize_NestedObject_ShouldWork()
        {
            // Arrange
            var original = new NestedTestData
            {
                Title = "Parent",
                Inner = new TestData { Name = "Child", Value = 50, Score = 5.5f }
            };

            // Act
            var json = _serializerManager.SerializeToString(original);
            var restored = _serializerManager.DeserializeFromString<NestedTestData>(json);

            // Assert
            Assert.AreEqual(original.Title, restored.Title);
            Assert.IsNotNull(restored.Inner);
            Assert.AreEqual(original.Inner.Name, restored.Inner.Name);
            Assert.AreEqual(original.Inner.Value, restored.Inner.Value);
        }

        /// <summary>
        /// 测试反序列化空字符串
        /// </summary>
        [Test]
        public void DeserializeFromString_WithEmptyString_ShouldReturnDefault()
        {
            // Act
            var result = _serializerManager.DeserializeFromString<TestData>("");

            // Assert
            Assert.IsNull(result, "空字符串应该返回默认值");
        }

        /// <summary>
        /// 测试反序列化null字符串
        /// </summary>
        [Test]
        public void DeserializeFromString_WithNullString_ShouldReturnDefault()
        {
            // Act
            var result = _serializerManager.DeserializeFromString<TestData>("null");

            // Assert
            Assert.IsNull(result, "null字符串应该返回默认值");
        }

        #endregion

        #region 事件测试

        /// <summary>
        /// 测试注册序列化器时触发事件
        /// </summary>
        [Test]
        public void RegisterSerializer_ShouldRaiseEvent()
        {
            // Arrange
            string receivedName = null;
            xFrameEventBus.SubscribeTo<SerializerRegisteredEvent>((ref SerializerRegisteredEvent e) =>
            {
                receivedName = e.SerializerName;
                _registeredEventCount++;
            });

            var customSerializer = new JsonSerializer();

            // Act
            _serializerManager.RegisterSerializer("EventTest", customSerializer);

            // Assert
            Assert.AreEqual(1, _registeredEventCount, "应该触发一次注册事件");
            Assert.AreEqual("EventTest", receivedName, "事件应该包含正确的序列化器名称");
        }

        /// <summary>
        /// 测试注销序列化器时触发事件
        /// </summary>
        [Test]
        public void UnregisterSerializer_ShouldRaiseEvent()
        {
            // Arrange
            string receivedName = null;
            xFrameEventBus.SubscribeTo<SerializerUnregisteredEvent>((ref SerializerUnregisteredEvent e) =>
            {
                receivedName = e.SerializerName;
                _unregisteredEventCount++;
            });

            var customSerializer = new JsonSerializer();
            _serializerManager.RegisterSerializer("ToUnregister", customSerializer);

            // Act
            _serializerManager.UnregisterSerializer("ToUnregister");

            // Assert
            Assert.AreEqual(1, _unregisteredEventCount, "应该触发一次注销事件");
            Assert.AreEqual("ToUnregister", receivedName, "事件应该包含正确的序列化器名称");
        }

        /// <summary>
        /// 测试设置默认序列化器时触发事件
        /// </summary>
        [Test]
        public void SetDefaultSerializer_ShouldRaiseEvent()
        {
            // Arrange
            string previousName = null;
            string newName = null;
            xFrameEventBus.SubscribeTo<DefaultSerializerChangedEvent>((ref DefaultSerializerChangedEvent e) =>
            {
                previousName = e.PreviousSerializerName;
                newName = e.NewSerializerName;
                _defaultChangedEventCount++;
            });

            var customSerializer = new JsonSerializer();
            _serializerManager.RegisterSerializer("NewDefault", customSerializer);

            // Act
            _serializerManager.SetDefaultSerializer("NewDefault");

            // Assert
            Assert.AreEqual(1, _defaultChangedEventCount, "应该触发一次默认序列化器变更事件");
            Assert.AreEqual(JsonSerializer.SerializerName, previousName, "之前的默认序列化器名称应该正确");
            Assert.AreEqual("NewDefault", newName, "新的默认序列化器名称应该正确");
        }

        #endregion

        #region 边界条件测试

        /// <summary>
        /// 测试注册空名称的序列化器时抛出异常
        /// </summary>
        [Test]
        public void RegisterSerializer_WithNullName_ShouldThrowException()
        {
            // Arrange
            var serializer = new JsonSerializer();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _serializerManager.RegisterSerializer(null, serializer));
        }

        /// <summary>
        /// 测试注册空实例的序列化器时抛出异常
        /// </summary>
        [Test]
        public void RegisterSerializer_WithNullSerializer_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _serializerManager.RegisterSerializer("Test", null));
        }

        /// <summary>
        /// 测试注销不存在的序列化器
        /// </summary>
        [Test]
        public void UnregisterSerializer_WithNonExistentName_ShouldReturnFalse()
        {
            // Act
            var result = _serializerManager.UnregisterSerializer("NonExistent");

            // Assert
            Assert.IsFalse(result, "注销不存在的序列化器应该返回false");
        }

        /// <summary>
        /// 测试注销默认序列化器后默认序列化器变为null
        /// </summary>
        [Test]
        public void UnregisterDefaultSerializer_ShouldClearDefault()
        {
            // Act
            _serializerManager.UnregisterSerializer(JsonSerializer.SerializerName);

            // Assert
            Assert.IsNull(_serializerManager.DefaultSerializer, "注销默认序列化器后应该变为null");
        }

        #endregion
    }
}
