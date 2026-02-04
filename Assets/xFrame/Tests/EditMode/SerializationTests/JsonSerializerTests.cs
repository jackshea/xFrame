using System;
using NUnit.Framework;
using xFrame.Runtime.Serialization;

namespace xFrame.Tests.SerializationTests
{
    /// <summary>
    /// JSON序列化器单元测试
    /// 测试JsonSerializer的核心功能
    /// </summary>
    [TestFixture]
    public class JsonSerializerTests
    {
        /// <summary>
        /// 测试用的简单数据类
        /// </summary>
        [Serializable]
        private class SimpleData
        {
            public string StringField;
            public int IntField;
            public float FloatField;
            public bool BoolField;
        }

        /// <summary>
        /// 测试用的数组数据类
        /// </summary>
        [Serializable]
        private class ArrayData
        {
            public int[] Numbers;
            public string[] Names;
        }

        /// <summary>
        /// 测试用的嵌套数据类
        /// </summary>
        [Serializable]
        private class NestedData
        {
            public string Name;
            public SimpleData Inner;
        }

        private JsonSerializer _serializer;
        private JsonSerializer _prettySerializer;

        [SetUp]
        public void SetUp()
        {
            _serializer = new JsonSerializer(false);
            _prettySerializer = new JsonSerializer(true);
        }

        #region 基础属性测试

        /// <summary>
        /// 测试序列化器名称
        /// </summary>
        [Test]
        public void Name_ShouldReturnJson()
        {
            // Assert
            Assert.AreEqual(JsonSerializer.SerializerName, _serializer.Name);
            Assert.AreEqual("Json", _serializer.Name);
        }

        #endregion

        #region 字符串序列化测试

        /// <summary>
        /// 测试简单对象序列化为字符串
        /// </summary>
        [Test]
        public void SerializeToString_SimpleObject_ShouldWork()
        {
            // Arrange
            var data = new SimpleData
            {
                StringField = "Hello",
                IntField = 42,
                FloatField = 3.14f,
                BoolField = true
            };

            // Act
            var json = _serializer.SerializeToString(data);

            // Assert
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("Hello"));
            Assert.IsTrue(json.Contains("42"));
            Assert.IsTrue(json.Contains("true"));
        }

        /// <summary>
        /// 测试null对象序列化
        /// </summary>
        [Test]
        public void SerializeToString_NullObject_ShouldReturnNull()
        {
            // Act
            var json = _serializer.SerializeToString<SimpleData>(null);

            // Assert
            Assert.AreEqual("null", json);
        }

        /// <summary>
        /// 测试格式化输出
        /// </summary>
        [Test]
        public void SerializeToString_WithPrettyPrint_ShouldBeFormatted()
        {
            // Arrange
            var data = new SimpleData { StringField = "Test", IntField = 1 };

            // Act
            var normalJson = _serializer.SerializeToString(data);
            var prettyJson = _prettySerializer.SerializeToString(data);

            // Assert
            Assert.IsTrue(prettyJson.Length >= normalJson.Length, 
                "格式化输出应该包含更多字符（换行和缩进）");
        }

        /// <summary>
        /// 测试数组序列化
        /// </summary>
        [Test]
        public void SerializeToString_ArrayData_ShouldWork()
        {
            // Arrange
            var data = new ArrayData
            {
                Numbers = new[] { 1, 2, 3, 4, 5 },
                Names = new[] { "Alice", "Bob", "Charlie" }
            };

            // Act
            var json = _serializer.SerializeToString(data);

            // Assert
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("1"));
            Assert.IsTrue(json.Contains("Alice"));
        }

        /// <summary>
        /// 测试嵌套对象序列化
        /// </summary>
        [Test]
        public void SerializeToString_NestedData_ShouldWork()
        {
            // Arrange
            var data = new NestedData
            {
                Name = "Parent",
                Inner = new SimpleData { StringField = "Child", IntField = 10 }
            };

            // Act
            var json = _serializer.SerializeToString(data);

            // Assert
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("Parent"));
            Assert.IsTrue(json.Contains("Child"));
        }

        #endregion

        #region 字节数组序列化测试

        /// <summary>
        /// 测试序列化为字节数组
        /// </summary>
        [Test]
        public void Serialize_ShouldReturnUtf8Bytes()
        {
            // Arrange
            var data = new SimpleData { StringField = "Test", IntField = 42 };

            // Act
            var bytes = _serializer.Serialize(data);

            // Assert
            Assert.IsNotNull(bytes);
            Assert.IsTrue(bytes.Length > 0);

            // 验证是UTF-8编码
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            Assert.IsTrue(json.Contains("Test"));
        }

        #endregion

        #region 字符串反序列化测试

        /// <summary>
        /// 测试从字符串反序列化简单对象
        /// </summary>
        [Test]
        public void DeserializeFromString_SimpleObject_ShouldWork()
        {
            // Arrange
            var json = "{\"StringField\":\"Hello\",\"IntField\":42,\"FloatField\":3.14,\"BoolField\":true}";

            // Act
            var data = _serializer.DeserializeFromString<SimpleData>(json);

            // Assert
            Assert.IsNotNull(data);
            Assert.AreEqual("Hello", data.StringField);
            Assert.AreEqual(42, data.IntField);
            Assert.AreEqual(3.14f, data.FloatField, 0.01f);
            Assert.IsTrue(data.BoolField);
        }

        /// <summary>
        /// 测试反序列化空字符串
        /// </summary>
        [Test]
        public void DeserializeFromString_EmptyString_ShouldReturnDefault()
        {
            // Act
            var data = _serializer.DeserializeFromString<SimpleData>("");

            // Assert
            Assert.IsNull(data);
        }

        /// <summary>
        /// 测试反序列化null字符串
        /// </summary>
        [Test]
        public void DeserializeFromString_NullString_ShouldReturnDefault()
        {
            // Act
            var data = _serializer.DeserializeFromString<SimpleData>("null");

            // Assert
            Assert.IsNull(data);
        }

        /// <summary>
        /// 测试反序列化数组数据
        /// </summary>
        [Test]
        public void DeserializeFromString_ArrayData_ShouldWork()
        {
            // Arrange
            var json = "{\"Numbers\":[1,2,3],\"Names\":[\"A\",\"B\"]}";

            // Act
            var data = _serializer.DeserializeFromString<ArrayData>(json);

            // Assert
            Assert.IsNotNull(data);
            Assert.AreEqual(3, data.Numbers.Length);
            Assert.AreEqual(2, data.Names.Length);
            Assert.AreEqual(1, data.Numbers[0]);
            Assert.AreEqual("A", data.Names[0]);
        }

        /// <summary>
        /// 测试反序列化嵌套数据
        /// </summary>
        [Test]
        public void DeserializeFromString_NestedData_ShouldWork()
        {
            // Arrange
            var json = "{\"Name\":\"Parent\",\"Inner\":{\"StringField\":\"Child\",\"IntField\":10}}";

            // Act
            var data = _serializer.DeserializeFromString<NestedData>(json);

            // Assert
            Assert.IsNotNull(data);
            Assert.AreEqual("Parent", data.Name);
            Assert.IsNotNull(data.Inner);
            Assert.AreEqual("Child", data.Inner.StringField);
            Assert.AreEqual(10, data.Inner.IntField);
        }

        #endregion

        #region 字节数组反序列化测试

        /// <summary>
        /// 测试从字节数组反序列化
        /// </summary>
        [Test]
        public void Deserialize_FromBytes_ShouldWork()
        {
            // Arrange
            var json = "{\"StringField\":\"ByteTest\",\"IntField\":99}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            // Act
            var data = _serializer.Deserialize<SimpleData>(bytes);

            // Assert
            Assert.IsNotNull(data);
            Assert.AreEqual("ByteTest", data.StringField);
            Assert.AreEqual(99, data.IntField);
        }

        /// <summary>
        /// 测试反序列化空字节数组
        /// </summary>
        [Test]
        public void Deserialize_EmptyBytes_ShouldReturnDefault()
        {
            // Act
            var data = _serializer.Deserialize<SimpleData>(new byte[0]);

            // Assert
            Assert.IsNull(data);
        }

        /// <summary>
        /// 测试反序列化null字节数组
        /// </summary>
        [Test]
        public void Deserialize_NullBytes_ShouldReturnDefault()
        {
            // Act
            var data = _serializer.Deserialize<SimpleData>(null);

            // Assert
            Assert.IsNull(data);
        }

        #endregion

        #region 类型反序列化测试

        /// <summary>
        /// 测试使用Type参数从字符串反序列化
        /// </summary>
        [Test]
        public void DeserializeFromString_WithType_ShouldWork()
        {
            // Arrange
            var json = "{\"StringField\":\"TypeTest\",\"IntField\":50}";

            // Act
            var data = _serializer.DeserializeFromString(typeof(SimpleData), json) as SimpleData;

            // Assert
            Assert.IsNotNull(data);
            Assert.AreEqual("TypeTest", data.StringField);
            Assert.AreEqual(50, data.IntField);
        }

        /// <summary>
        /// 测试使用Type参数从字节数组反序列化
        /// </summary>
        [Test]
        public void Deserialize_WithType_ShouldWork()
        {
            // Arrange
            var json = "{\"StringField\":\"TypeByteTest\",\"IntField\":75}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            // Act
            var data = _serializer.Deserialize(typeof(SimpleData), bytes) as SimpleData;

            // Assert
            Assert.IsNotNull(data);
            Assert.AreEqual("TypeByteTest", data.StringField);
            Assert.AreEqual(75, data.IntField);
        }

        /// <summary>
        /// 测试使用Type参数反序列化空数据
        /// </summary>
        [Test]
        public void DeserializeFromString_WithType_EmptyString_ShouldReturnNull()
        {
            // Act
            var data = _serializer.DeserializeFromString(typeof(SimpleData), "");

            // Assert
            Assert.IsNull(data);
        }

        #endregion

        #region 往返测试

        /// <summary>
        /// 测试序列化和反序列化的往返一致性
        /// </summary>
        [Test]
        public void RoundTrip_ShouldPreserveData()
        {
            // Arrange
            var original = new SimpleData
            {
                StringField = "RoundTrip",
                IntField = 123,
                FloatField = 4.56f,
                BoolField = true
            };

            // Act
            var json = _serializer.SerializeToString(original);
            var restored = _serializer.DeserializeFromString<SimpleData>(json);

            // Assert
            Assert.AreEqual(original.StringField, restored.StringField);
            Assert.AreEqual(original.IntField, restored.IntField);
            Assert.AreEqual(original.FloatField, restored.FloatField, 0.01f);
            Assert.AreEqual(original.BoolField, restored.BoolField);
        }

        /// <summary>
        /// 测试字节数组往返一致性
        /// </summary>
        [Test]
        public void RoundTrip_Bytes_ShouldPreserveData()
        {
            // Arrange
            var original = new SimpleData
            {
                StringField = "ByteRoundTrip",
                IntField = 456,
                FloatField = 7.89f,
                BoolField = false
            };

            // Act
            var bytes = _serializer.Serialize(original);
            var restored = _serializer.Deserialize<SimpleData>(bytes);

            // Assert
            Assert.AreEqual(original.StringField, restored.StringField);
            Assert.AreEqual(original.IntField, restored.IntField);
            Assert.AreEqual(original.FloatField, restored.FloatField, 0.01f);
            Assert.AreEqual(original.BoolField, restored.BoolField);
        }

        /// <summary>
        /// 测试嵌套对象往返一致性
        /// </summary>
        [Test]
        public void RoundTrip_NestedData_ShouldPreserveData()
        {
            // Arrange
            var original = new NestedData
            {
                Name = "ParentRoundTrip",
                Inner = new SimpleData
                {
                    StringField = "ChildRoundTrip",
                    IntField = 789,
                    FloatField = 1.23f,
                    BoolField = true
                }
            };

            // Act
            var json = _serializer.SerializeToString(original);
            var restored = _serializer.DeserializeFromString<NestedData>(json);

            // Assert
            Assert.AreEqual(original.Name, restored.Name);
            Assert.IsNotNull(restored.Inner);
            Assert.AreEqual(original.Inner.StringField, restored.Inner.StringField);
            Assert.AreEqual(original.Inner.IntField, restored.Inner.IntField);
            Assert.AreEqual(original.Inner.FloatField, restored.Inner.FloatField, 0.01f);
            Assert.AreEqual(original.Inner.BoolField, restored.Inner.BoolField);
        }

        #endregion
    }
}
