using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using xFrame.Runtime.DataStructures;

namespace xFrame.Tests
{
    /// <summary>
    /// LRU缓存系统单元测试
    /// 测试LRU缓存的核心功能和边界情况
    /// </summary>
    [TestFixture]
    public class LRUCacheTests
    {
        /// <summary>
        /// 测试基本的Put和Get操作
        /// </summary>
        [Test]
        public void TestBasicPutAndGet()
        {
            var cache = new LRUCache<int, string>(3);

            // 测试Put操作
            cache.Put(1, "one");
            cache.Put(2, "two");
            cache.Put(3, "three");

            Assert.AreEqual(3, cache.Count);
            Assert.AreEqual(3, cache.Capacity);

            // 测试Get操作
            Assert.AreEqual("one", cache.Get(1));
            Assert.AreEqual("two", cache.Get(2));
            Assert.AreEqual("three", cache.Get(3));
        }

        /// <summary>
        /// 测试TryGet方法
        /// </summary>
        [Test]
        public void TestTryGet()
        {
            var cache = new LRUCache<string, int>(2);

            cache.Put("key1", 100);
            cache.Put("key2", 200);

            // 测试存在的键
            Assert.IsTrue(cache.TryGet("key1", out var value1));
            Assert.AreEqual(100, value1);

            // 测试不存在的键
            Assert.IsFalse(cache.TryGet("key3", out var value2));
            Assert.AreEqual(0, value2); // 默认值
        }

        /// <summary>
        /// 测试LRU淘汰机制
        /// </summary>
        [Test]
        public void TestLRUEviction()
        {
            var cache = new LRUCache<int, string>(2);

            // 添加两个元素
            cache.Put(1, "one");
            cache.Put(2, "two");

            // 访问第一个元素，使其成为最近使用的
            cache.Get(1);

            // 添加第三个元素，应该淘汰最久未使用的元素（2）
            cache.Put(3, "three");

            Assert.AreEqual(2, cache.Count);
            Assert.IsTrue(cache.ContainsKey(1));
            Assert.IsFalse(cache.ContainsKey(2)); // 应该被淘汰
            Assert.IsTrue(cache.ContainsKey(3));
        }

        /// <summary>
        /// 测试更新现有键的值
        /// </summary>
        [Test]
        public void TestUpdateExistingKey()
        {
            var cache = new LRUCache<string, int>(3);

            cache.Put("key1", 100);
            cache.Put("key2", 200);
            cache.Put("key3", 300);

            // 更新现有键的值
            cache.Put("key2", 250);

            Assert.AreEqual(3, cache.Count);
            Assert.AreEqual(250, cache.Get("key2"));

            // key2应该成为最近使用的
            cache.Put("key4", 400); // 应该淘汰key1
            Assert.IsFalse(cache.ContainsKey("key1"));
            Assert.IsTrue(cache.ContainsKey("key2"));
        }

        /// <summary>
        /// 测试Remove方法
        /// </summary>
        [Test]
        public void TestRemove()
        {
            var cache = new LRUCache<int, string>(3);

            cache.Put(1, "one");
            cache.Put(2, "two");
            cache.Put(3, "three");

            // 移除存在的键
            Assert.IsTrue(cache.Remove(2));
            Assert.AreEqual(2, cache.Count);
            Assert.IsFalse(cache.ContainsKey(2));

            // 移除不存在的键
            Assert.IsFalse(cache.Remove(4));
            Assert.AreEqual(2, cache.Count);
        }

        /// <summary>
        /// 测试Clear方法
        /// </summary>
        [Test]
        public void TestClear()
        {
            var cache = new LRUCache<string, int>(3);

            cache.Put("a", 1);
            cache.Put("b", 2);
            cache.Put("c", 3);

            Assert.AreEqual(3, cache.Count);

            cache.Clear();

            Assert.AreEqual(0, cache.Count);
            Assert.IsFalse(cache.ContainsKey("a"));
            Assert.IsFalse(cache.ContainsKey("b"));
            Assert.IsFalse(cache.ContainsKey("c"));
        }

        /// <summary>
        /// 测试ContainsKey方法
        /// </summary>
        [Test]
        public void TestContainsKey()
        {
            var cache = new LRUCache<int, string>(2);

            cache.Put(1, "one");
            cache.Put(2, "two");

            Assert.IsTrue(cache.ContainsKey(1));
            Assert.IsTrue(cache.ContainsKey(2));
            Assert.IsFalse(cache.ContainsKey(3));
        }

        /// <summary>
        /// 测试Keys和Values属性
        /// </summary>
        [Test]
        public void TestKeysAndValues()
        {
            var cache = new LRUCache<int, string>(3);

            cache.Put(1, "one");
            cache.Put(2, "two");
            cache.Put(3, "three");

            // 访问key2，使其成为最近使用的
            cache.Get(2);

            var keys = cache.Keys.ToList();
            var values = cache.Values.ToList();

            Assert.AreEqual(3, keys.Count);
            Assert.AreEqual(3, values.Count);

            // 最近使用的应该在前面
            Assert.AreEqual(2, keys[0]);
            Assert.AreEqual("two", values[0]);
        }

        /// <summary>
        /// 测试访问顺序
        /// </summary>
        [Test]
        public void TestAccessOrder()
        {
            var cache = new LRUCache<int, string>(3);

            cache.Put(1, "one");
            cache.Put(2, "two");
            cache.Put(3, "three");

            // 访问顺序：3, 1, 2
            cache.Get(3);
            cache.Get(1);
            cache.Get(2);

            var keys = cache.Keys.ToList();

            // 最近访问的应该在前面
            Assert.AreEqual(2, keys[0]);
            Assert.AreEqual(1, keys[1]);
            Assert.AreEqual(3, keys[2]);
        }

        /// <summary>
        /// 测试异常情况
        /// </summary>
        [Test]
        public void TestExceptions()
        {
            // 测试无效容量
            Assert.Throws<ArgumentException>(() => new LRUCache<int, string>(0));
            Assert.Throws<ArgumentException>(() => new LRUCache<int, string>(-1));

            var cache = new LRUCache<int, string>(2);

            // 测试获取不存在的键
            Assert.Throws<KeyNotFoundException>(() => cache.Get(999));
        }

        /// <summary>
        /// 测试单个元素的缓存
        /// </summary>
        [Test]
        public void TestSingleElementCache()
        {
            var cache = new LRUCache<string, int>(1);

            cache.Put("key1", 100);
            Assert.AreEqual(100, cache.Get("key1"));

            // 添加新元素应该淘汰旧元素
            cache.Put("key2", 200);
            Assert.IsFalse(cache.ContainsKey("key1"));
            Assert.IsTrue(cache.ContainsKey("key2"));
            Assert.AreEqual(1, cache.Count);
        }

        /// <summary>
        /// 测试大量数据的性能
        /// </summary>
        [Test]
        public void TestLargeDataPerformance()
        {
            var cache = new LRUCache<int, string>(1000);

            // 添加大量数据
            for (var i = 0; i < 2000; i++) cache.Put(i, $"value_{i}");

            // 缓存应该只保留最后1000个元素
            Assert.AreEqual(1000, cache.Count);

            // 检查最新的元素是否存在
            for (var i = 1000; i < 2000; i++)
            {
                Assert.IsTrue(cache.ContainsKey(i));
                Assert.AreEqual($"value_{i}", cache.Get(i));
            }

            // 检查最旧的元素是否被淘汰
            for (var i = 0; i < 1000; i++) Assert.IsFalse(cache.ContainsKey(i));
        }

        /// <summary>
        /// 测试null值处理
        /// </summary>
        [Test]
        public void TestNullValues()
        {
            var cache = new LRUCache<string, string>(3);

            // 测试存储null值
            cache.Put("key1", null);
            cache.Put("key2", "value2");

            Assert.IsTrue(cache.ContainsKey("key1"));
            Assert.IsNull(cache.Get("key1"));
            Assert.AreEqual("value2", cache.Get("key2"));

            // 测试TryGet with null值
            Assert.IsTrue(cache.TryGet("key1", out var value));
            Assert.IsNull(value);
        }

        /// <summary>
        /// 测试ToString方法
        /// </summary>
        [Test]
        public void TestToString()
        {
            var cache = new LRUCache<int, string>(5);
            cache.Put(1, "one");
            cache.Put(2, "two");

            var str = cache.ToString();
            Assert.IsTrue(str.Contains("Count=2"));
            Assert.IsTrue(str.Contains("Capacity=5"));
        }
    }
}