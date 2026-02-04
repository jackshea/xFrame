using System;
using System.Text;
using NUnit.Framework;
using xFrame.Runtime.Serialization;

namespace xFrame.Tests.SerializationTests
{
    /// <summary>
    /// 自定义序列化器测试
    /// 演示如何实现和测试自定义序列化器
    /// </summary>
    [TestFixture]
    public class CustomSerializerTests
    {
        /// <summary>
        /// 测试用的简单数据类
        /// </summary>
        [Serializable]
        private class TestData
        {
            public string Name;
            public int Value;
        }

        /// <summary>
        /// 自定义的Base64包装序列化器
        /// 将JSON序列化结果转换为Base64编码
        /// </summary>
        private class Base64JsonSerializer : ISerializer
        {
            public const string SerializerName = "Base64Json";
            private readonly JsonSerializer _innerSerializer = new JsonSerializer();

            public string Name => SerializerName;

            public byte[] Serialize<T>(T obj)
            {
                var json = _innerSerializer.SerializeToString(obj);
                var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
                return Encoding.UTF8.GetBytes(base64);
            }

            public string SerializeToString<T>(T obj)
            {
                var json = _innerSerializer.SerializeToString(obj);
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            }

            public T Deserialize<T>(byte[] data)
            {
                if (data == null || data.Length == 0) return default;
                var base64 = Encoding.UTF8.GetString(data);
                return DeserializeFromString<T>(base64);
            }

            public T DeserializeFromString<T>(string data)
            {
                if (string.IsNullOrEmpty(data)) return default;
                var jsonBytes = Convert.FromBase64String(data);
                var json = Encoding.UTF8.GetString(jsonBytes);
                return _innerSerializer.DeserializeFromString<T>(json);
            }

            public object Deserialize(Type type, byte[] data)
            {
                if (data == null || data.Length == 0) return null;
                var base64 = Encoding.UTF8.GetString(data);
                return DeserializeFromString(type, base64);
            }

            public object DeserializeFromString(Type type, string data)
            {
                if (string.IsNullOrEmpty(data)) return null;
                var jsonBytes = Convert.FromBase64String(data);
                var json = Encoding.UTF8.GetString(jsonBytes);
                return _innerSerializer.DeserializeFromString(type, json);
            }
        }

        /// <summary>
        /// 压缩序列化器示例（简化版，仅移除空格）
        /// </summary>
        private class CompactJsonSerializer : ISerializer
        {
            public const string SerializerName = "CompactJson";
            private readonly JsonSerializer _innerSerializer = new JsonSerializer();

            public string Name => SerializerName;

            public byte[] Serialize<T>(T obj)
            {
                var json = SerializeToString(obj);
                return Encoding.UTF8.GetBytes(json);
            }

            public string SerializeToString<T>(T obj)
            {
                var json = _innerSerializer.SerializeToString(obj);
                // 简单的压缩：移除不必要的空格
                return json.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");
            }

            public T Deserialize<T>(byte[] data)
            {
                if (data == null || data.Length == 0) return default;
                var json = Encoding.UTF8.GetString(data);
                return _innerSerializer.DeserializeFromString<T>(json);
            }

            public T DeserializeFromString<T>(string data)
            {
                return _innerSerializer.DeserializeFromString<T>(data);
            }

            public object Deserialize(Type type, byte[] data)
            {
                if (data == null || data.Length == 0) return null;
                var json = Encoding.UTF8.GetString(data);
                return _innerSerializer.DeserializeFromString(type, json);
            }

            public object DeserializeFromString(Type type, string data)
            {
                return _innerSerializer.DeserializeFromString(type, data);
            }
        }

        private ISerializerManager _serializerManager;

        [SetUp]
        public void SetUp()
        {
            _serializerManager = new SerializerManager();
        }

        #region Base64序列化器测试

        /// <summary>
        /// 测试Base64序列化器的名称
        /// </summary>
        [Test]
        public void Base64JsonSerializer_Name_ShouldBeCorrect()
        {
            // Arrange
            var serializer = new Base64JsonSerializer();

            // Assert
            Assert.AreEqual(Base64JsonSerializer.SerializerName, serializer.Name);
            Assert.AreEqual("Base64Json", serializer.Name);
        }

        /// <summary>
        /// 测试Base64序列化器的序列化
        /// </summary>
        [Test]
        public void Base64JsonSerializer_Serialize_ShouldReturnBase64()
        {
            // Arrange
            var serializer = new Base64JsonSerializer();
            var data = new TestData { Name = "Test", Value = 42 };

            // Act
            var base64 = serializer.SerializeToString(data);

            // Assert
            Assert.IsNotNull(base64);
            // 验证是有效的Base64
            Assert.DoesNotThrow(() => Convert.FromBase64String(base64));
        }

        /// <summary>
        /// 测试Base64序列化器的往返
        /// </summary>
        [Test]
        public void Base64JsonSerializer_RoundTrip_ShouldWork()
        {
            // Arrange
            var serializer = new Base64JsonSerializer();
            var original = new TestData { Name = "Base64Test", Value = 123 };

            // Act
            var base64 = serializer.SerializeToString(original);
            var restored = serializer.DeserializeFromString<TestData>(base64);

            // Assert
            Assert.IsNotNull(restored);
            Assert.AreEqual(original.Name, restored.Name);
            Assert.AreEqual(original.Value, restored.Value);
        }

        /// <summary>
        /// 测试在SerializerManager中注册Base64序列化器
        /// </summary>
        [Test]
        public void Base64JsonSerializer_RegisterAndUse_ShouldWork()
        {
            // Arrange
            var base64Serializer = new Base64JsonSerializer();
            _serializerManager.RegisterSerializer(Base64JsonSerializer.SerializerName, base64Serializer);

            var data = new TestData { Name = "ManagerTest", Value = 456 };

            // Act
            var base64 = _serializerManager.SerializeToString(Base64JsonSerializer.SerializerName, data);
            var restored = _serializerManager.DeserializeFromString<TestData>(Base64JsonSerializer.SerializerName, base64);

            // Assert
            Assert.IsNotNull(restored);
            Assert.AreEqual(data.Name, restored.Name);
            Assert.AreEqual(data.Value, restored.Value);
        }

        /// <summary>
        /// 测试设置Base64序列化器为默认
        /// </summary>
        [Test]
        public void Base64JsonSerializer_SetAsDefault_ShouldWork()
        {
            // Arrange
            var base64Serializer = new Base64JsonSerializer();
            _serializerManager.RegisterSerializer(Base64JsonSerializer.SerializerName, base64Serializer);
            _serializerManager.SetDefaultSerializer(Base64JsonSerializer.SerializerName);

            var data = new TestData { Name = "DefaultTest", Value = 789 };

            // Act
            var base64 = _serializerManager.SerializeToString(data);
            var restored = _serializerManager.DeserializeFromString<TestData>(base64);

            // Assert
            Assert.AreEqual(Base64JsonSerializer.SerializerName, _serializerManager.DefaultSerializer.Name);
            Assert.IsNotNull(restored);
            Assert.AreEqual(data.Name, restored.Name);
        }

        #endregion

        #region Compact序列化器测试

        /// <summary>
        /// 测试Compact序列化器的名称
        /// </summary>
        [Test]
        public void CompactJsonSerializer_Name_ShouldBeCorrect()
        {
            // Arrange
            var serializer = new CompactJsonSerializer();

            // Assert
            Assert.AreEqual(CompactJsonSerializer.SerializerName, serializer.Name);
        }

        /// <summary>
        /// 测试Compact序列化器的往返
        /// </summary>
        [Test]
        public void CompactJsonSerializer_RoundTrip_ShouldWork()
        {
            // Arrange
            var serializer = new CompactJsonSerializer();
            var original = new TestData { Name = "CompactTest", Value = 999 };

            // Act
            var compact = serializer.SerializeToString(original);
            var restored = serializer.DeserializeFromString<TestData>(compact);

            // Assert
            Assert.IsNotNull(restored);
            Assert.AreEqual(original.Name, restored.Name);
            Assert.AreEqual(original.Value, restored.Value);
        }

        #endregion

        #region 多序列化器协同测试

        /// <summary>
        /// 测试多个自定义序列化器同时注册
        /// </summary>
        [Test]
        public void MultipleCustomSerializers_RegisterAll_ShouldWork()
        {
            // Arrange
            var base64Serializer = new Base64JsonSerializer();
            var compactSerializer = new CompactJsonSerializer();

            _serializerManager.RegisterSerializer(Base64JsonSerializer.SerializerName, base64Serializer);
            _serializerManager.RegisterSerializer(CompactJsonSerializer.SerializerName, compactSerializer);

            var data = new TestData { Name = "MultiTest", Value = 100 };

            // Act
            var jsonResult = _serializerManager.SerializeToString(JsonSerializer.SerializerName, data);
            var base64Result = _serializerManager.SerializeToString(Base64JsonSerializer.SerializerName, data);
            var compactResult = _serializerManager.SerializeToString(CompactJsonSerializer.SerializerName, data);

            // Assert
            Assert.IsNotNull(jsonResult);
            Assert.IsNotNull(base64Result);
            Assert.IsNotNull(compactResult);

            // 三种格式应该不同
            Assert.AreNotEqual(jsonResult, base64Result);

            // 但都能正确反序列化
            var fromJson = _serializerManager.DeserializeFromString<TestData>(JsonSerializer.SerializerName, jsonResult);
            var fromBase64 = _serializerManager.DeserializeFromString<TestData>(Base64JsonSerializer.SerializerName, base64Result);
            var fromCompact = _serializerManager.DeserializeFromString<TestData>(CompactJsonSerializer.SerializerName, compactResult);

            Assert.AreEqual(data.Name, fromJson.Name);
            Assert.AreEqual(data.Name, fromBase64.Name);
            Assert.AreEqual(data.Name, fromCompact.Name);
        }

        /// <summary>
        /// 测试在不同序列化器之间切换
        /// </summary>
        [Test]
        public void SwitchBetweenSerializers_ShouldWork()
        {
            // Arrange
            var base64Serializer = new Base64JsonSerializer();
            _serializerManager.RegisterSerializer(Base64JsonSerializer.SerializerName, base64Serializer);

            var data = new TestData { Name = "SwitchTest", Value = 200 };

            // Act & Assert - 使用默认JSON序列化器
            Assert.AreEqual(JsonSerializer.SerializerName, _serializerManager.DefaultSerializer.Name);
            var json1 = _serializerManager.SerializeToString(data);

            // 切换到Base64
            _serializerManager.SetDefaultSerializer(Base64JsonSerializer.SerializerName);
            Assert.AreEqual(Base64JsonSerializer.SerializerName, _serializerManager.DefaultSerializer.Name);
            var base64 = _serializerManager.SerializeToString(data);

            // 切换回JSON
            _serializerManager.SetDefaultSerializer(JsonSerializer.SerializerName);
            Assert.AreEqual(JsonSerializer.SerializerName, _serializerManager.DefaultSerializer.Name);
            var json2 = _serializerManager.SerializeToString(data);

            // 验证JSON结果一致
            Assert.AreEqual(json1, json2);

            // 验证Base64不同于JSON
            Assert.AreNotEqual(json1, base64);
        }

        #endregion

        #region 边界条件测试

        /// <summary>
        /// 测试自定义序列化器处理null
        /// </summary>
        [Test]
        public void CustomSerializer_HandleNull_ShouldWork()
        {
            // Arrange
            var serializer = new Base64JsonSerializer();

            // Act
            var result = serializer.DeserializeFromString<TestData>(null);

            // Assert
            Assert.IsNull(result);
        }

        /// <summary>
        /// 测试自定义序列化器处理空字符串
        /// </summary>
        [Test]
        public void CustomSerializer_HandleEmptyString_ShouldWork()
        {
            // Arrange
            var serializer = new Base64JsonSerializer();

            // Act
            var result = serializer.DeserializeFromString<TestData>("");

            // Assert
            Assert.IsNull(result);
        }

        /// <summary>
        /// 测试自定义序列化器处理空字节数组
        /// </summary>
        [Test]
        public void CustomSerializer_HandleEmptyBytes_ShouldWork()
        {
            // Arrange
            var serializer = new Base64JsonSerializer();

            // Act
            var result = serializer.Deserialize<TestData>(new byte[0]);

            // Assert
            Assert.IsNull(result);
        }

        #endregion
    }
}
