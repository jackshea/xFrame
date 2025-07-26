using System;
using UnityEngine;
using xFrame.Core.ObjectPool;

namespace xFrame.Examples
{
    /// <summary>
    /// 对象池系统使用示例
    /// 演示如何使用对象池系统来管理对象的创建和回收
    /// </summary>
    public class ObjectPoolExample : MonoBehaviour
    {
        /// <summary>
        /// 示例子弹类
        /// 实现IPoolable接口以支持对象池生命周期管理
        /// </summary>
        public class Bullet : IPoolable
        {
            public Vector3 Position { get; set; }
            public Vector3 Velocity { get; set; }
            public float Damage { get; set; }
            public bool IsActive { get; private set; }

            /// <summary>
            /// 当从对象池获取时调用
            /// </summary>
            public void OnGet()
            {
                IsActive = true;
                Debug.Log($"子弹被激活: {GetHashCode()}");
            }

            /// <summary>
            /// 当释放回对象池时调用
            /// </summary>
            public void OnRelease()
            {
                IsActive = false;
                Position = Vector3.zero;
                Velocity = Vector3.zero;
                Damage = 0f;
                Debug.Log($"子弹被回收: {GetHashCode()}");
            }

            /// <summary>
            /// 当对象被销毁时调用
            /// </summary>
            public void OnDestroy()
            {
                Debug.Log($"子弹被销毁: {GetHashCode()}");
            }

            /// <summary>
            /// 更新子弹位置
            /// </summary>
            public void Update(float deltaTime)
            {
                if (IsActive)
                {
                    Position += Velocity * deltaTime;
                }
            }
        }

        /// <summary>
        /// 示例敌人类
        /// 普通类，不实现IPoolable接口
        /// </summary>
        public class Enemy
        {
            public Vector3 Position { get; set; }
            public float Health { get; set; }
            public bool IsAlive => Health > 0;

            public Enemy()
            {
                Health = 100f;
            }

            public void TakeDamage(float damage)
            {
                Health -= damage;
                if (Health <= 0)
                {
                    Health = 0;
                }
            }

            public void Reset()
            {
                Health = 100f;
                Position = Vector3.zero;
            }
        }

        // 对象池实例
        private IObjectPool<Bullet> _bulletPool;
        private IObjectPool<Enemy> _enemyPool;
        private ObjectPoolManager _poolManager;

        /// <summary>
        /// 初始化对象池
        /// </summary>
        void Start()
        {
            Debug.Log("=== 对象池系统示例开始 ===");

            // 示例1: 创建支持IPoolable接口的对象池
            CreatePoolableObjectPool();

            // 示例2: 创建普通对象池
            CreateNormalObjectPool();

            // 示例3: 使用对象池管理器
            CreatePoolManager();

            // 示例4: 演示各种功能
            DemonstratePoolFeatures();
        }

        /// <summary>
        /// 创建支持IPoolable接口的对象池
        /// </summary>
        private void CreatePoolableObjectPool()
        {
            Debug.Log("\n--- 示例1: 创建支持IPoolable接口的对象池 ---");

            // 创建子弹对象池，最大容量为10，支持线程安全
            _bulletPool = ObjectPoolFactory.CreateForPoolable<Bullet>(
                createFunc: () => new Bullet(),
                maxSize: 10,
                threadSafe: false
            );

            // 预热对象池，预先创建5个子弹对象
            _bulletPool.WarmUp(5);
            Debug.Log($"预热后池中对象数量: {_bulletPool.CountInPool}, 总对象数量: {_bulletPool.CountAll}");
        }

        /// <summary>
        /// 创建普通对象池
        /// </summary>
        private void CreateNormalObjectPool()
        {
            Debug.Log("\n--- 示例2: 创建普通对象池 ---");

            // 创建敌人对象池，带有自定义回调
            _enemyPool = ObjectPoolFactory.Create<Enemy>(
                createFunc: () => new Enemy(),
                onGet: enemy => Debug.Log($"获取敌人: {enemy.GetHashCode()}"),
                onRelease: enemy => {
                    enemy.Reset(); // 重置敌人状态
                    Debug.Log($"释放敌人: {enemy.GetHashCode()}");
                },
                onDestroy: enemy => Debug.Log($"销毁敌人: {enemy.GetHashCode()}"),
                maxSize: 5
            );

            // 预热敌人对象池
            _enemyPool.WarmUp(3);
            Debug.Log($"敌人池预热后: 池中{_enemyPool.CountInPool}个, 总共{_enemyPool.CountAll}个");
        }

        /// <summary>
        /// 创建对象池管理器
        /// </summary>
        private void CreatePoolManager()
        {
            Debug.Log("\n--- 示例3: 使用对象池管理器 ---");

            // 创建对象池管理器
            _poolManager = new ObjectPoolManager();

            // 注册对象池到管理器
            _poolManager.RegisterPool(_bulletPool);
            _poolManager.RegisterPool(_enemyPool);

            // 或者直接通过管理器创建对象池
            var stringPool = _poolManager.GetOrCreatePool(() => "DefaultString");
            Debug.Log("通过管理器创建了字符串对象池");
        }

        /// <summary>
        /// 演示对象池的各种功能
        /// </summary>
        private void DemonstratePoolFeatures()
        {
            Debug.Log("\n--- 示例4: 演示对象池功能 ---");

            // 演示子弹对象池
            DemonstrateBulletPool();

            // 演示敌人对象池
            DemonstrateEnemyPool();

            // 演示通过管理器使用对象池
            DemonstratePoolManager();

            // 演示容量限制
            DemonstrateCapacityLimit();
        }

        /// <summary>
        /// 演示子弹对象池功能
        /// </summary>
        private void DemonstrateBulletPool()
        {
            Debug.Log("\n-- 子弹对象池演示 --");

            // 获取3个子弹对象
            var bullet1 = _bulletPool.Get();
            var bullet2 = _bulletPool.Get();
            var bullet3 = _bulletPool.Get();

            // 设置子弹属性
            bullet1.Position = new Vector3(0, 0, 0);
            bullet1.Velocity = new Vector3(1, 0, 0);
            bullet1.Damage = 10f;

            bullet2.Position = new Vector3(0, 1, 0);
            bullet2.Velocity = new Vector3(0, 1, 0);
            bullet2.Damage = 15f;

            Debug.Log($"获取3个子弹后: 池中{_bulletPool.CountInPool}个, 总共{_bulletPool.CountAll}个");

            // 释放子弹对象
            _bulletPool.Release(bullet1);
            _bulletPool.Release(bullet2);
            Debug.Log($"释放2个子弹后: 池中{_bulletPool.CountInPool}个, 总共{_bulletPool.CountAll}个");

            // 再次获取子弹，应该重用之前的对象
            var reusedBullet = _bulletPool.Get();
            Debug.Log($"重用子弹: {reusedBullet.GetHashCode()}, 位置已重置: {reusedBullet.Position}");

            _bulletPool.Release(reusedBullet);
            _bulletPool.Release(bullet3);
        }

        /// <summary>
        /// 演示敌人对象池功能
        /// </summary>
        private void DemonstrateEnemyPool()
        {
            Debug.Log("\n-- 敌人对象池演示 --");

            // 获取敌人对象
            var enemy1 = _enemyPool.Get();
            var enemy2 = _enemyPool.Get();

            // 模拟战斗
            enemy1.Position = new Vector3(5, 0, 0);
            enemy1.TakeDamage(30f);
            Debug.Log($"敌人1受到伤害后血量: {enemy1.Health}");

            enemy2.Position = new Vector3(-5, 0, 0);
            enemy2.TakeDamage(120f); // 致命伤害
            Debug.Log($"敌人2是否存活: {enemy2.IsAlive}");

            // 释放敌人对象
            _enemyPool.Release(enemy1);
            _enemyPool.Release(enemy2);

            // 重新获取敌人，状态应该被重置
            var reusedEnemy = _enemyPool.Get();
            Debug.Log($"重用敌人血量: {reusedEnemy.Health}, 位置: {reusedEnemy.Position}");
            _enemyPool.Release(reusedEnemy);
        }

        /// <summary>
        /// 演示通过管理器使用对象池
        /// </summary>
        private void DemonstratePoolManager()
        {
            Debug.Log("\n-- 对象池管理器演示 --");

            // 通过管理器获取对象
            var bullet = _poolManager.Get<Bullet>();
            var enemy = _poolManager.Get<Enemy>();

            Debug.Log($"通过管理器获取子弹: {bullet?.GetHashCode()}");
            Debug.Log($"通过管理器获取敌人: {enemy?.GetHashCode()}");

            // 通过管理器释放对象
            _poolManager.Release(bullet);
            _poolManager.Release(enemy);

            // 通过管理器预热对象池
            _poolManager.WarmUp<Bullet>(2);
            Debug.Log("通过管理器预热了子弹池");
        }

        /// <summary>
        /// 演示容量限制功能
        /// </summary>
        private void DemonstrateCapacityLimit()
        {
            Debug.Log("\n-- 容量限制演示 --");

            // 创建一个容量限制为2的对象池
            var limitedPool = ObjectPoolFactory.Create(
                () => new Enemy(),
                onDestroy: enemy => Debug.Log($"敌人因容量限制被销毁: {enemy.GetHashCode()}"),
                maxSize: 2
            );

            // 获取3个对象
            var obj1 = limitedPool.Get();
            var obj2 = limitedPool.Get();
            var obj3 = limitedPool.Get();

            Debug.Log($"获取3个对象后总数: {limitedPool.CountAll}");

            // 释放3个对象，但只有2个能放入池中
            limitedPool.Release(obj1);
            limitedPool.Release(obj2);
            limitedPool.Release(obj3); // 这个会被销毁

            Debug.Log($"释放后池中对象数: {limitedPool.CountInPool}, 总对象数: {limitedPool.CountAll}");

            limitedPool.Dispose();
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        void OnDestroy()
        {
            Debug.Log("\n=== 清理对象池资源 ===");

            _bulletPool?.Dispose();
            _enemyPool?.Dispose();
            _poolManager?.Dispose();

            Debug.Log("对象池系统示例结束");
        }

        /// <summary>
        /// 在Inspector中显示对象池状态信息
        /// </summary>
        void OnGUI()
        {
            if (_bulletPool == null || _enemyPool == null)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("=== 对象池状态 ===");
            GUILayout.Label($"子弹池: {_bulletPool.CountInPool}/{_bulletPool.CountAll}");
            GUILayout.Label($"敌人池: {_enemyPool.CountInPool}/{_enemyPool.CountAll}");

            if (GUILayout.Button("获取子弹"))
            {
                var bullet = _bulletPool.Get();
                Debug.Log($"手动获取子弹: {bullet.GetHashCode()}");
            }

            if (GUILayout.Button("清空子弹池"))
            {
                _bulletPool.Clear();
                Debug.Log("手动清空子弹池");
            }

            GUILayout.EndArea();
        }
    }
}
