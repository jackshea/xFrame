using System;
using NUnit.Framework;
using xFrame.Runtime.ObjectPool;

namespace xFrame.Tests
{
    /// <summary>
    /// 对象池系统单元测试
    /// 测试对象池的核心功能和边界情况
    /// </summary>
    [TestFixture]
    public class ObjectPoolTests
    {
        /// <summary>
        /// 测试用的简单类
        /// </summary>
        private class TestObject
        {
            public int Value { get; set; }
            public bool IsReset { get; set; }

            public void Reset()
            {
                Value = 0;
                IsReset = true;
            }
        }

        /// <summary>
        /// 实现IPoolable接口的测试类
        /// </summary>
        private class PoolableTestObject : IPoolable
        {
            public int Value { get; set; }
            public bool OnGetCalled { get; private set; }
            public bool OnReleaseCalled { get; private set; }
            public bool OnDestroyCalled { get; private set; }

            public void OnGet()
            {
                OnGetCalled = true;
                OnReleaseCalled = false;
                OnDestroyCalled = false;
            }

            public void OnRelease()
            {
                OnReleaseCalled = true;
                Value = 0; // 重置状态
            }

            public void OnDestroy()
            {
                OnDestroyCalled = true;
            }
        }

        /// <summary>
        /// 测试基本的获取和释放功能
        /// </summary>
        [Test]
        public void TestBasicGetAndRelease()
        {
            // 创建对象池
            var pool = ObjectPoolFactory.Create(() => new TestObject());

            // 测试获取对象
            var obj1 = pool.Get();
            Assert.IsNotNull(obj1);
            Assert.AreEqual(1, pool.CountAll);
            Assert.AreEqual(0, pool.CountInPool);

            // 测试释放对象
            pool.Release(obj1);
            Assert.AreEqual(1, pool.CountAll);
            Assert.AreEqual(1, pool.CountInPool);

            // 测试重复使用对象
            var obj2 = pool.Get();
            Assert.AreSame(obj1, obj2); // 应该是同一个对象
            Assert.AreEqual(1, pool.CountAll);
            Assert.AreEqual(0, pool.CountInPool);
        }

        /// <summary>
        /// 测试对象池的容量限制
        /// </summary>
        [Test]
        public void TestMaxSizeLimit()
        {
            var destroyCount = 0;

            // 创建最大容量为2的对象池
            var pool = ObjectPoolFactory.Create(
                () => new TestObject(),
                null,
                null,
                obj => destroyCount++,
                2);

            // 创建3个对象
            var obj1 = pool.Get();
            var obj2 = pool.Get();
            var obj3 = pool.Get();

            Assert.AreEqual(3, pool.CountAll);
            Assert.AreEqual(0, pool.CountInPool);

            // 释放3个对象，但池最多只能容纳2个
            pool.Release(obj1);
            pool.Release(obj2);
            pool.Release(obj3);

            Assert.AreEqual(2, pool.CountAll); // 一个对象被销毁
            Assert.AreEqual(2, pool.CountInPool);
            Assert.AreEqual(1, destroyCount); // 一个对象被销毁
        }

        /// <summary>
        /// 测试预热功能
        /// </summary>
        [Test]
        public void TestWarmUp()
        {
            var pool = ObjectPoolFactory.Create(() => new TestObject());

            // 预热5个对象
            pool.WarmUp(5);
            Assert.AreEqual(5, pool.CountAll);
            Assert.AreEqual(5, pool.CountInPool);

            // 获取对象，应该从池中取出
            var obj = pool.Get();
            Assert.IsNotNull(obj);
            Assert.AreEqual(5, pool.CountAll);
            Assert.AreEqual(4, pool.CountInPool);
        }

        /// <summary>
        /// 测试带容量限制的预热
        /// </summary>
        [Test]
        public void TestWarmUpWithMaxSize()
        {
            var pool = ObjectPoolFactory.Create(() => new TestObject(), 3);

            // 尝试预热5个对象，但最大容量为3
            pool.WarmUp(5);
            Assert.AreEqual(3, pool.CountAll);
            Assert.AreEqual(3, pool.CountInPool);
        }

        /// <summary>
        /// 测试清空功能
        /// </summary>
        [Test]
        public void TestClear()
        {
            var destroyCount = 0;
            var pool = ObjectPoolFactory.Create(
                () => new TestObject(),
                null,
                null,
                obj => destroyCount++);

            // 预热3个对象
            pool.WarmUp(3);
            Assert.AreEqual(3, pool.CountAll);
            Assert.AreEqual(3, pool.CountInPool);

            // 清空池
            pool.Clear();
            Assert.AreEqual(0, pool.CountAll);
            Assert.AreEqual(0, pool.CountInPool);
            Assert.AreEqual(3, destroyCount);
        }

        /// <summary>
        /// 测试回调函数
        /// </summary>
        [Test]
        public void TestCallbacks()
        {
            var getCount = 0;
            var releaseCount = 0;
            var destroyCount = 0;

            var pool = ObjectPoolFactory.Create(
                () => new TestObject(),
                obj => getCount++,
                obj => releaseCount++,
                obj => destroyCount++,
                1);

            // 获取对象
            var obj1 = pool.Get();
            Assert.AreEqual(1, getCount);

            // 释放对象
            pool.Release(obj1);
            Assert.AreEqual(1, releaseCount);

            // 获取第二个对象并释放，然后再释放一个对象（应该被销毁）
            var obj2 = pool.Get();
            var obj3 = pool.Get();
            pool.Release(obj2);
            pool.Release(obj3); // 这个应该被销毁，因为池已满

            Assert.AreEqual(3, getCount);
            Assert.AreEqual(3, releaseCount);
            Assert.AreEqual(1, destroyCount);
        }

        /// <summary>
        /// 测试IPoolable接口支持
        /// </summary>
        [Test]
        public void TestPoolableInterface()
        {
            var pool = ObjectPoolFactory.CreateForPoolable(() => new PoolableTestObject());

            // 获取对象
            var obj = pool.Get();
            Assert.IsTrue(obj.OnGetCalled);
            Assert.IsFalse(obj.OnReleaseCalled);
            Assert.IsFalse(obj.OnDestroyCalled);

            obj.Value = 100;

            // 释放对象
            pool.Release(obj);
            Assert.IsTrue(obj.OnReleaseCalled);
            Assert.AreEqual(0, obj.Value); // 应该被重置

            // 清空池
            pool.Clear();
            Assert.IsTrue(obj.OnDestroyCalled);
        }

        /// <summary>
        /// 测试默认构造函数的对象池
        /// </summary>
        [Test]
        public void TestDefaultConstructorPool()
        {
            var pool = ObjectPoolFactory.CreateDefault<TestObject>();

            var obj = pool.Get();
            Assert.IsNotNull(obj);
            Assert.AreEqual(0, obj.Value);

            obj.Value = 42;
            pool.Release(obj);

            var obj2 = pool.Get();
            Assert.AreSame(obj, obj2);
            Assert.AreEqual(42, obj2.Value); // 值应该保持不变，因为没有重置回调
        }

        /// <summary>
        /// 测试防止重复释放
        /// </summary>
        [Test]
        public void TestPreventDuplicateRelease()
        {
            var pool = ObjectPoolFactory.Create(() => new TestObject());

            var obj = pool.Get();
            pool.Release(obj);
            Assert.AreEqual(1, pool.CountInPool);

            // 重复释放同一个对象
            pool.Release(obj);
            Assert.AreEqual(1, pool.CountInPool); // 数量不应该增加
        }

        /// <summary>
        /// 测试空对象释放
        /// </summary>
        [Test]
        public void TestReleaseNullObject()
        {
            var pool = ObjectPoolFactory.Create(() => new TestObject());

            // 释放null对象不应该抛出异常
            Assert.DoesNotThrow(() => pool.Release(null));
            Assert.AreEqual(0, pool.CountInPool);
        }

        /// <summary>
        /// 测试对象池销毁
        /// </summary>
        [Test]
        public void TestPoolDispose()
        {
            var pool = ObjectPoolFactory.Create(() => new TestObject()) as ObjectPool<TestObject>;

            pool.WarmUp(3);
            Assert.AreEqual(3, pool.CountInPool);

            pool.Dispose();
            Assert.AreEqual(0, pool.CountInPool);
            Assert.AreEqual(0, pool.CountAll);

            // 销毁后的操作应该抛出异常
            Assert.Throws<ObjectDisposedException>(() => pool.Get());
            Assert.Throws<ObjectDisposedException>(() => pool.Release(new TestObject()));
            Assert.Throws<ObjectDisposedException>(() => pool.WarmUp(1));
            Assert.Throws<ObjectDisposedException>(() => pool.Clear());
        }
    }
}