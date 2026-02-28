using System;
using NUnit.Framework;
using xFrame.Runtime.ObjectPool;

namespace xFrame.Tests
{
    /// <summary>
    /// 对象池管理器单元测试
    /// 测试对象池管理器的功能
    /// </summary>
    [TestFixture]
    public class ObjectPoolManagerTests
    {
        /// <summary>
        /// 测试前的初始化
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _manager = new ObjectPoolManager();
        }

        /// <summary>
        /// 测试后的清理
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _manager?.Dispose();
        }

        /// <summary>
        /// 测试用的简单类
        /// </summary>
        private class TestObjectA
        {
            public int Value { get; set; }
        }

        /// <summary>
        /// 测试用的另一个类
        /// </summary>
        private class TestObjectB
        {
            public string Name { get; set; }
        }

        private ObjectPoolManager _manager;

        /// <summary>
        /// 测试注册和获取对象池
        /// </summary>
        [Test]
        public void TestRegisterAndGetPool()
        {
            // 创建并注册对象池
            var pool = ObjectPoolFactory.Create(() => new TestObjectA());
            _manager.RegisterPool(pool);

            // 获取对象池
            var retrievedPool = _manager.GetPool<TestObjectA>();
            Assert.AreSame(pool, retrievedPool);

            // 获取不存在的对象池
            var nonExistentPool = _manager.GetPool<TestObjectB>();
            Assert.IsNull(nonExistentPool);
        }

        /// <summary>
        /// 测试获取或创建对象池
        /// </summary>
        [Test]
        public void TestGetOrCreatePool()
        {
            // 第一次调用应该创建新的对象池
            var pool1 = _manager.GetOrCreatePool(() => new TestObjectA());
            Assert.IsNotNull(pool1);

            // 第二次调用应该返回相同的对象池
            var pool2 = _manager.GetOrCreatePool(() => new TestObjectA());
            Assert.AreSame(pool1, pool2);
        }

        /// <summary>
        /// 测试获取或创建默认对象池
        /// </summary>
        [Test]
        public void TestGetOrCreateDefaultPool()
        {
            // 第一次调用应该创建新的对象池
            var pool1 = _manager.GetOrCreateDefaultPool<TestObjectA>();
            Assert.IsNotNull(pool1);

            // 第二次调用应该返回相同的对象池
            var pool2 = _manager.GetOrCreateDefaultPool<TestObjectA>();
            Assert.AreSame(pool1, pool2);
        }

        /// <summary>
        /// 测试通过管理器获取和释放对象
        /// </summary>
        [Test]
        public void TestGetAndReleaseObjects()
        {
            // 注册对象池
            var pool = ObjectPoolFactory.Create(() => new TestObjectA());
            _manager.RegisterPool(pool);

            // 通过管理器获取对象
            var obj1 = _manager.Get<TestObjectA>();
            Assert.IsNotNull(obj1);
            Assert.AreEqual(1, pool.CountAll);
            Assert.AreEqual(0, pool.CountInPool);

            // 通过管理器释放对象
            _manager.Release(obj1);
            Assert.AreEqual(1, pool.CountAll);
            Assert.AreEqual(1, pool.CountInPool);

            // 获取不存在池的对象
            var obj2 = _manager.Get<TestObjectB>();
            Assert.IsNull(obj2);
        }

        /// <summary>
        /// 测试预热功能
        /// </summary>
        [Test]
        public void TestWarmUp()
        {
            // 注册对象池
            var pool = ObjectPoolFactory.Create(() => new TestObjectA());
            _manager.RegisterPool(pool);

            // 通过管理器预热
            _manager.WarmUp<TestObjectA>(3);
            Assert.AreEqual(3, pool.CountAll);
            Assert.AreEqual(3, pool.CountInPool);

            // 预热不存在的池不应该抛出异常
            Assert.DoesNotThrow(() => _manager.WarmUp<TestObjectB>(3));
        }

        /// <summary>
        /// 测试清空功能
        /// </summary>
        [Test]
        public void TestClear()
        {
            // 注册对象池
            var pool = ObjectPoolFactory.Create(() => new TestObjectA());
            _manager.RegisterPool(pool);

            // 预热对象池
            pool.WarmUp(3);
            Assert.AreEqual(3, pool.CountInPool);

            // 通过管理器清空
            _manager.Clear<TestObjectA>();
            Assert.AreEqual(0, pool.CountInPool);
            Assert.AreEqual(0, pool.CountAll);

            // 清空不存在的池不应该抛出异常
            Assert.DoesNotThrow(() => _manager.Clear<TestObjectB>());
        }

        /// <summary>
        /// 测试清空所有对象池
        /// </summary>
        [Test]
        public void TestClearAll()
        {
            // 注册多个对象池
            var poolA = ObjectPoolFactory.Create(() => new TestObjectA());
            var poolB = ObjectPoolFactory.Create(() => new TestObjectB());
            _manager.RegisterPool(poolA);
            _manager.RegisterPool(poolB);

            // 预热对象池
            poolA.WarmUp(2);
            poolB.WarmUp(3);

            Assert.AreEqual(2, poolA.CountInPool);
            Assert.AreEqual(3, poolB.CountInPool);

            // 清空所有对象池
            _manager.ClearAll();

            // 对象池应该被清空且无法再访问
            var retrievedPoolA = _manager.GetPool<TestObjectA>();
            var retrievedPoolB = _manager.GetPool<TestObjectB>();
            Assert.IsNull(retrievedPoolA);
            Assert.IsNull(retrievedPoolB);
        }

        /// <summary>
        /// 测试多类型对象池管理
        /// </summary>
        [Test]
        public void TestMultipleObjectTypes()
        {
            // 创建不同类型的对象池
            var poolA = ObjectPoolFactory.Create(() => new TestObjectA { Value = 1 });
            var poolB = ObjectPoolFactory.Create(() => new TestObjectB { Name = "Test" });

            _manager.RegisterPool(poolA);
            _manager.RegisterPool(poolB);

            // 获取不同类型的对象
            var objA = _manager.Get<TestObjectA>();
            var objB = _manager.Get<TestObjectB>();

            Assert.IsNotNull(objA);
            Assert.IsNotNull(objB);
            Assert.AreEqual(1, objA.Value);
            Assert.AreEqual("Test", objB.Name);

            // 释放对象
            _manager.Release(objA);
            _manager.Release(objB);

            Assert.AreEqual(1, poolA.CountInPool);
            Assert.AreEqual(1, poolB.CountInPool);
        }

        /// <summary>
        /// 测试注册空对象池
        /// </summary>
        [Test]
        public void TestRegisterNullPool()
        {
            Assert.Throws<ArgumentNullException>(() => _manager.RegisterPool<TestObjectA>(null));
        }

        /// <summary>
        /// 测试释放空对象
        /// </summary>
        [Test]
        public void TestReleaseNullObject()
        {
            // 释放null对象不应该抛出异常
            Assert.DoesNotThrow(() => _manager.Release<TestObjectA>(null));
        }

        /// <summary>
        /// 测试管理器销毁
        /// </summary>
        [Test]
        public void TestManagerDispose()
        {
            // 注册对象池
            var pool = ObjectPoolFactory.Create(() => new TestObjectA());
            _manager.RegisterPool(pool);
            pool.WarmUp(3);

            Assert.AreEqual(3, pool.CountInPool);

            // 销毁管理器
            _manager.Dispose();

            // 对象池应该被清空
            var retrievedPool = _manager.GetPool<TestObjectA>();
            Assert.IsNull(retrievedPool);

            Assert.IsNull(_manager.GetPool<TestObjectA>());
            Assert.IsNull(_manager.Get<TestObjectA>());
            // 销毁后的操作应该抛出异常
            Assert.Throws<ObjectDisposedException>(() => _manager.RegisterPool(pool));
        }

        /// <summary>
        /// 测试线程安全的管理器
        /// </summary>
        [Test]
        public void TestThreadSafeManager()
        {
            using (var threadSafeManager = new ObjectPoolManager(true))
            {
                var pool = ObjectPoolFactory.Create(() => new TestObjectA(), threadSafe: true);
                threadSafeManager.RegisterPool(pool);

                var obj = threadSafeManager.Get<TestObjectA>();
                Assert.IsNotNull(obj);

                threadSafeManager.Release(obj);
                Assert.AreEqual(1, pool.CountInPool);
            }
        }
    }
}