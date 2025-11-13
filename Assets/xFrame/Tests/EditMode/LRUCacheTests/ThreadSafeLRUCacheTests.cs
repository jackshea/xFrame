using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using xFrame.Runtime.DataStructures;

namespace xFrame.Tests
{
    /// <summary>
    /// 线程安全LRU缓存单元测试
    /// 测试线程安全LRU缓存的功能和并发安全性
    /// </summary>
    [TestFixture]
    public class ThreadSafeLRUCacheTests
    {
        /// <summary>
        /// 测试基本功能
        /// </summary>
        [Test]
        public void TestBasicFunctionality()
        {
            using (var cache = new ThreadSafeLRUCache<int, string>(3))
            {
                cache.Put(1, "one");
                cache.Put(2, "two");
                cache.Put(3, "three");

                Assert.AreEqual(3, cache.Count);
                Assert.AreEqual("one", cache.Get(1));
                Assert.AreEqual("two", cache.Get(2));
                Assert.AreEqual("three", cache.Get(3));
            }
        }

        /// <summary>
        /// 测试LRU淘汰机制
        /// </summary>
        [Test]
        public void TestLRUEviction()
        {
            using (var cache = new ThreadSafeLRUCache<int, string>(2))
            {
                cache.Put(1, "one");
                cache.Put(2, "two");
                cache.Get(1); // 使key1成为最近使用的
                cache.Put(3, "three"); // 应该淘汰key2

                Assert.IsTrue(cache.ContainsKey(1));
                Assert.IsFalse(cache.ContainsKey(2));
                Assert.IsTrue(cache.ContainsKey(3));
            }
        }

        /// <summary>
        /// 测试并发读操作
        /// </summary>
        [Test]
        public void TestConcurrentReads()
        {
            using (var cache = new ThreadSafeLRUCache<int, string>(100))
            {
                // 预填充缓存
                for (var i = 0; i < 50; i++) cache.Put(i, $"value_{i}");

                var tasks = new List<Task>();
                var results = new List<bool>[10];

                for (var t = 0; t < 10; t++)
                {
                    results[t] = new List<bool>();
                    var taskIndex = t;

                    tasks.Add(Task.Run(() =>
                    {
                        for (var i = 0; i < 50; i++)
                        {
                            var found = cache.TryGet(i % 25, out var value);
                            results[taskIndex].Add(found);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());

                // 验证所有读操作都成功
                foreach (var result in results) Assert.IsTrue(result.All(r => r));
            }
        }

        /// <summary>
        /// 测试并发写操作
        /// </summary>
        [Test]
        public void TestConcurrentWrites()
        {
            using (var cache = new ThreadSafeLRUCache<int, string>(1000))
            {
                var tasks = new List<Task>();

                for (var t = 0; t < 10; t++)
                {
                    var taskIndex = t;
                    tasks.Add(Task.Run(() =>
                    {
                        for (var i = 0; i < 100; i++)
                        {
                            var key = taskIndex * 100 + i;
                            cache.Put(key, $"value_{key}");
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());

                // 验证所有数据都被正确写入
                Assert.AreEqual(1000, cache.Count);

                for (var i = 0; i < 1000; i++)
                {
                    Assert.IsTrue(cache.ContainsKey(i));
                    Assert.AreEqual($"value_{i}", cache.Get(i));
                }
            }
        }

        /// <summary>
        /// 测试并发读写操作
        /// </summary>
        [Test]
        public void TestConcurrentReadWrites()
        {
            using (var cache = new ThreadSafeLRUCache<int, string>(500))
            {
                var tasks = new List<Task>();
                var readResults = new List<string>[5];

                // 预填充一些数据
                for (var i = 0; i < 100; i++) cache.Put(i, $"initial_{i}");

                // 启动读任务
                for (var t = 0; t < 5; t++)
                {
                    readResults[t] = new List<string>();
                    var taskIndex = t;

                    tasks.Add(Task.Run(() =>
                    {
                        for (var i = 0; i < 200; i++)
                        {
                            if (cache.TryGet(i % 100, out var value)) readResults[taskIndex].Add(value);
                            Thread.Sleep(1); // 小延迟以增加并发冲突概率
                        }
                    }));
                }

                // 启动写任务
                for (var t = 0; t < 5; t++)
                {
                    var taskIndex = t;
                    tasks.Add(Task.Run(() =>
                    {
                        for (var i = 0; i < 100; i++)
                        {
                            var key = 100 + taskIndex * 100 + i;
                            cache.Put(key, $"new_{key}");
                            Thread.Sleep(1); // 小延迟以增加并发冲突概率
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());

                // 验证缓存状态一致性
                Assert.LessOrEqual(cache.Count, 500);

                // 验证读取的数据格式正确
                foreach (var results in readResults)
                foreach (var value in results)
                    Assert.IsTrue(value.StartsWith("initial_") || value.StartsWith("new_"));
            }
        }

        /// <summary>
        /// 测试并发Remove操作
        /// </summary>
        [Test]
        public void TestConcurrentRemoves()
        {
            using (var cache = new ThreadSafeLRUCache<int, string>(1000))
            {
                // 预填充数据
                for (var i = 0; i < 1000; i++) cache.Put(i, $"value_{i}");

                var tasks = new List<Task>();
                var removeResults = new List<bool>[10];

                for (var t = 0; t < 10; t++)
                {
                    removeResults[t] = new List<bool>();
                    var taskIndex = t;

                    tasks.Add(Task.Run(() =>
                    {
                        for (var i = 0; i < 100; i++)
                        {
                            var key = taskIndex * 100 + i;
                            var removed = cache.Remove(key);
                            removeResults[taskIndex].Add(removed);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());

                // 验证所有remove操作都成功
                foreach (var results in removeResults) Assert.IsTrue(results.All(r => r));

                // 验证缓存为空
                Assert.AreEqual(0, cache.Count);
            }
        }

        /// <summary>
        /// 测试并发Clear操作
        /// </summary>
        [Test]
        public void TestConcurrentClear()
        {
            using (var cache = new ThreadSafeLRUCache<int, string>(100))
            {
                var tasks = new List<Task>();

                // 一个任务持续添加数据
                tasks.Add(Task.Run(() =>
                {
                    for (var i = 0; i < 1000; i++)
                    {
                        cache.Put(i, $"value_{i}");
                        Thread.Sleep(1);
                    }
                }));

                // 另一个任务定期清空缓存
                tasks.Add(Task.Run(() =>
                {
                    for (var i = 0; i < 10; i++)
                    {
                        Thread.Sleep(50);
                        cache.Clear();
                    }
                }));

                Task.WaitAll(tasks.ToArray());

                // 最终清空一次确保缓存为空
                cache.Clear();
                Assert.AreEqual(0, cache.Count);
            }
        }

        /// <summary>
        /// 测试Keys和Values属性的线程安全性
        /// </summary>
        [Test]
        public void TestKeysAndValuesThreadSafety()
        {
            using (var cache = new ThreadSafeLRUCache<int, string>(100))
            {
                // 预填充数据
                for (var i = 0; i < 50; i++) cache.Put(i, $"value_{i}");

                var tasks = new List<Task>();
                var exceptions = new List<Exception>();

                // 多个任务同时访问Keys和Values
                for (var t = 0; t < 5; t++)
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            for (var i = 0; i < 100; i++)
                            {
                                // 注意：在并发环境下，单独获取Keys和Values可能会不一致
                                // 这是正常现象，因为它们是分别获取读锁的
                                var keys = cache.Keys.ToList();
                                var values = cache.Values.ToList();

                                // 允许一定的差异，因为在高并发下可能会有时序问题
                                var countDiff = Math.Abs(keys.Count - values.Count);
                                Assert.IsTrue(countDiff <= 2,
                                    $"Keys和Values数量差异过大: Keys={keys.Count}, Values={values.Count}, 差异={countDiff}");

                                // 使用GetKeyValueSnapshot方法获取一致的键值对
                                var snapshot = cache.GetKeyValueSnapshot();
                                var snapshotKeys = snapshot.Key.ToList();
                                var snapshotValues = snapshot.Value.ToList();
                                Assert.AreEqual(snapshotKeys.Count, snapshotValues.Count, "快照中的Keys和Values数量不一致");

                                Thread.Sleep(1);
                            }
                        }
                        catch (Exception ex)
                        {
                            lock (exceptions)
                            {
                                exceptions.Add(ex);
                            }
                        }
                    }));

                // 同时进行写操作
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        for (var i = 50; i < 150; i++)
                        {
                            cache.Put(i, $"value_{i}");
                            Thread.Sleep(2);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));

                Task.WaitAll(tasks.ToArray());

                // 验证没有异常发生
                Assert.AreEqual(0, exceptions.Count,
                    $"发生了 {exceptions.Count} 个异常: {string.Join(", ", exceptions.Select(e => e.Message))}");
            }
        }

        /// <summary>
        /// 测试Dispose方法
        /// </summary>
        [Test]
        public void TestDispose()
        {
            var cache = new ThreadSafeLRUCache<int, string>(10);

            cache.Put(1, "one");
            cache.Put(2, "two");

            Assert.AreEqual(2, cache.Count);

            cache.Dispose();

            // 调用Dispose后的操作应该抛出异常
            Assert.Throws<ObjectDisposedException>(() => cache.Get(1));
            Assert.Throws<ObjectDisposedException>(() => cache.Put(3, "three"));
            Assert.Throws<ObjectDisposedException>(() => cache.Remove(1));
            Assert.Throws<ObjectDisposedException>(() => cache.Clear());
            Assert.Throws<ObjectDisposedException>(() => cache.ContainsKey(1));
            Assert.Throws<ObjectDisposedException>(() => cache.TryGet(1, out var value));
        }

        /// <summary>
        /// 测试ToString方法的线程安全性
        /// </summary>
        [Test]
        public void TestToStringThreadSafety()
        {
            using (var cache = new ThreadSafeLRUCache<int, string>(50))
            {
                var tasks = new List<Task>();

                // 一个任务持续修改缓存
                tasks.Add(Task.Run(() =>
                {
                    for (var i = 0; i < 100; i++)
                    {
                        cache.Put(i, $"value_{i}");
                        Thread.Sleep(1);
                    }
                }));

                // 多个任务调用ToString
                for (var t = 0; t < 5; t++)
                    tasks.Add(Task.Run(() =>
                    {
                        for (var i = 0; i < 50; i++)
                        {
                            var str = cache.ToString();
                            Assert.IsNotNull(str);
                            Assert.IsTrue(str.Contains("ThreadSafeLRUCache"));
                            Thread.Sleep(2);
                        }
                    }));

                Task.WaitAll(tasks.ToArray());
            }
        }

        /// <summary>
        /// 压力测试：大量并发操作
        /// </summary>
        [Test]
        public void StressTest()
        {
            using (var cache = new ThreadSafeLRUCache<int, string>(1000))
            {
                var tasks = new List<Task>();
                var random = new Random();

                // 启动多个并发任务
                for (var t = 0; t < 20; t++)
                {
                    var taskId = t;
                    tasks.Add(Task.Run(() =>
                    {
                        var localRandom = new Random(taskId);

                        for (var i = 0; i < 500; i++)
                        {
                            var operation = localRandom.Next(4);
                            var key = localRandom.Next(2000);

                            switch (operation)
                            {
                                case 0: // Put
                                    cache.Put(key, $"value_{key}_{taskId}");
                                    break;
                                case 1: // Get
                                    cache.TryGet(key, out var value);
                                    break;
                                case 2: // Remove
                                    cache.Remove(key);
                                    break;
                                case 3: // ContainsKey
                                    cache.ContainsKey(key);
                                    break;
                            }
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());

                // 验证缓存状态一致性
                Assert.LessOrEqual(cache.Count, 1000);

                // 验证缓存仍然可以正常工作
                cache.Put(9999, "test");
                Assert.IsTrue(cache.ContainsKey(9999));
                Assert.AreEqual("test", cache.Get(9999));
            }
        }
    }
}