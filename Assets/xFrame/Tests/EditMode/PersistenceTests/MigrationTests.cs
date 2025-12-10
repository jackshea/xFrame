using System;
using NUnit.Framework;
using UnityEngine;
using xFrame.Runtime.Logging;
using xFrame.Runtime.Persistence.Migration;

namespace xFrame.Tests.PersistenceTests
{
    /// <summary>
    /// 数据迁移单元测试
    /// 测试迁移管理器和迁移器的功能
    /// </summary>
    [TestFixture]
    public class MigrationTests
    {
        /// <summary>
        /// 版本1的用户数据
        /// </summary>
        [Serializable]
        private class UserDataV1
        {
            public string name;
            public int age;
        }

        /// <summary>
        /// 版本2的用户数据（添加了email字段）
        /// </summary>
        [Serializable]
        private class UserDataV2
        {
            public string name;
            public int age;
            public string email;
        }

        /// <summary>
        /// 版本3的用户数据（拆分name为firstName和lastName）
        /// </summary>
        [Serializable]
        private class UserDataV3
        {
            public string firstName;
            public string lastName;
            public int age;
            public string email;
        }

        /// <summary>
        /// V1到V2的迁移器
        /// </summary>
        private class UserDataMigratorV1ToV2 : DataMigratorBase<UserDataV1, UserDataV2>
        {
            public override int FromVersion => 1;
            public override int ToVersion => 2;

            public override UserDataV2 MigrateTyped(UserDataV1 oldData)
            {
                return new UserDataV2
                {
                    name = oldData.name,
                    age = oldData.age,
                    email = "unknown@example.com" // 默认值
                };
            }
        }

        /// <summary>
        /// V2到V3的迁移器
        /// </summary>
        private class UserDataMigratorV2ToV3 : DataMigratorBase<UserDataV2, UserDataV3>
        {
            public override int FromVersion => 2;
            public override int ToVersion => 3;

            public override UserDataV3 MigrateTyped(UserDataV2 oldData)
            {
                var nameParts = oldData.name?.Split(' ') ?? new[] { "", "" };
                return new UserDataV3
                {
                    firstName = nameParts.Length > 0 ? nameParts[0] : "",
                    lastName = nameParts.Length > 1 ? nameParts[1] : "",
                    age = oldData.age,
                    email = oldData.email
                };
            }
        }

        /// <summary>
        /// 简单的JSON字符串迁移器
        /// </summary>
        private class SimpleJsonMigrator : DataMigratorBase
        {
            public override int FromVersion => 1;
            public override int ToVersion => 2;

            public override string Migrate(string oldData)
            {
                // 简单地在JSON中添加一个字段
                return oldData.Replace("}", ",\"newField\":\"added\"}");
            }
        }

        private MigrationManager _migrationManager;

        [SetUp]
        public void SetUp()
        {
            // 初始化日志系统（测试环境）
            try
            {
                var logManager = new XLogManager();
                XLog.Initialize(logManager);
            }
            catch
            {
                // 可能已经初始化
            }

            _migrationManager = new MigrationManager();
        }

        #region 迁移器注册测试

        /// <summary>
        /// 测试注册迁移器
        /// </summary>
        [Test]
        public void RegisterMigrator_ShouldWork()
        {
            var migrator = new UserDataMigratorV1ToV2();

            _migrationManager.RegisterMigrator<UserDataV3>(migrator);

            var migrators = _migrationManager.GetMigrators<UserDataV3>();
            Assert.AreEqual(1, migrators.Count);
            Assert.AreEqual(1, migrators[0].FromVersion);
            Assert.AreEqual(2, migrators[0].ToVersion);
        }

        /// <summary>
        /// 测试注册多个迁移器
        /// </summary>
        [Test]
        public void RegisterMigrator_Multiple_ShouldBeSorted()
        {
            _migrationManager.RegisterMigrator<UserDataV3>(new UserDataMigratorV2ToV3());
            _migrationManager.RegisterMigrator<UserDataV3>(new UserDataMigratorV1ToV2());

            var migrators = _migrationManager.GetMigrators<UserDataV3>();
            Assert.AreEqual(2, migrators.Count);
            Assert.AreEqual(1, migrators[0].FromVersion); // 应该按版本排序
            Assert.AreEqual(2, migrators[1].FromVersion);
        }

        /// <summary>
        /// 测试注销迁移器
        /// </summary>
        [Test]
        public void UnregisterMigrator_ShouldWork()
        {
            _migrationManager.RegisterMigrator<UserDataV3>(new UserDataMigratorV1ToV2());

            var result = _migrationManager.UnregisterMigrator<UserDataV3>(1);

            Assert.IsTrue(result);
            Assert.AreEqual(0, _migrationManager.GetMigrators<UserDataV3>().Count);
        }

        /// <summary>
        /// 测试注销不存在的迁移器
        /// </summary>
        [Test]
        public void UnregisterMigrator_NonExistent_ShouldReturnFalse()
        {
            var result = _migrationManager.UnregisterMigrator<UserDataV3>(99);
            Assert.IsFalse(result);
        }

        /// <summary>
        /// 测试清除所有迁移器
        /// </summary>
        [Test]
        public void Clear_ShouldRemoveAllMigrators()
        {
            _migrationManager.RegisterMigrator<UserDataV3>(new UserDataMigratorV1ToV2());
            _migrationManager.RegisterMigrator<UserDataV3>(new UserDataMigratorV2ToV3());

            _migrationManager.Clear();

            Assert.AreEqual(0, _migrationManager.GetMigrators<UserDataV3>().Count);
        }

        #endregion

        #region 迁移能力检查测试

        /// <summary>
        /// 测试检查可以迁移
        /// </summary>
        [Test]
        public void CanMigrate_WithMigrators_ShouldReturnTrue()
        {
            _migrationManager.RegisterMigrator<UserDataV3>(new UserDataMigratorV1ToV2());
            _migrationManager.RegisterMigrator<UserDataV3>(new UserDataMigratorV2ToV3());

            var canMigrate = _migrationManager.CanMigrate<UserDataV3>(1, 3);

            Assert.IsTrue(canMigrate);
        }

        /// <summary>
        /// 测试检查无法迁移（缺少迁移器）
        /// </summary>
        [Test]
        public void CanMigrate_MissingMigrator_ShouldReturnFalse()
        {
            _migrationManager.RegisterMigrator<UserDataV3>(new UserDataMigratorV1ToV2());
            // 缺少V2到V3的迁移器

            var canMigrate = _migrationManager.CanMigrate<UserDataV3>(1, 3);

            Assert.IsFalse(canMigrate);
        }

        /// <summary>
        /// 测试相同版本无需迁移
        /// </summary>
        [Test]
        public void CanMigrate_SameVersion_ShouldReturnTrue()
        {
            var canMigrate = _migrationManager.CanMigrate<UserDataV3>(2, 2);
            Assert.IsTrue(canMigrate);
        }

        /// <summary>
        /// 测试降级版本无需迁移
        /// </summary>
        [Test]
        public void CanMigrate_DowngradeVersion_ShouldReturnTrue()
        {
            var canMigrate = _migrationManager.CanMigrate<UserDataV3>(3, 1);
            Assert.IsTrue(canMigrate);
        }

        #endregion

        #region 数据迁移测试

        /// <summary>
        /// 测试单步迁移
        /// </summary>
        [Test]
        public void Migrate_SingleStep_ShouldWork()
        {
            _migrationManager.RegisterMigrator<UserDataV2>(new UserDataMigratorV1ToV2());

            var v1Data = new UserDataV1 { name = "John", age = 30 };
            var v1Json = JsonUtility.ToJson(v1Data);

            var v2Json = _migrationManager.Migrate<UserDataV2>(v1Json, 1, 2);
            var v2Data = JsonUtility.FromJson<UserDataV2>(v2Json);

            Assert.AreEqual("John", v2Data.name);
            Assert.AreEqual(30, v2Data.age);
            Assert.AreEqual("unknown@example.com", v2Data.email);
        }

        /// <summary>
        /// 测试链式迁移
        /// </summary>
        [Test]
        public void Migrate_ChainedSteps_ShouldWork()
        {
            _migrationManager.RegisterMigrator<UserDataV3>(new UserDataMigratorV1ToV2());
            _migrationManager.RegisterMigrator<UserDataV3>(new UserDataMigratorV2ToV3());

            var v1Data = new UserDataV1 { name = "John Doe", age = 25 };
            var v1Json = JsonUtility.ToJson(v1Data);

            var v3Json = _migrationManager.Migrate<UserDataV3>(v1Json, 1, 3);
            var v3Data = JsonUtility.FromJson<UserDataV3>(v3Json);

            Assert.AreEqual("John", v3Data.firstName);
            Assert.AreEqual("Doe", v3Data.lastName);
            Assert.AreEqual(25, v3Data.age);
            Assert.AreEqual("unknown@example.com", v3Data.email);
        }

        /// <summary>
        /// 测试相同版本不迁移
        /// </summary>
        [Test]
        public void Migrate_SameVersion_ShouldReturnOriginalData()
        {
            var originalJson = "{\"name\":\"Test\",\"age\":20}";

            var result = _migrationManager.Migrate<UserDataV1>(originalJson, 1, 1);

            Assert.AreEqual(originalJson, result);
        }

        /// <summary>
        /// 测试缺少迁移器抛出异常
        /// </summary>
        [Test]
        public void Migrate_MissingMigrator_ShouldThrow()
        {
            var v1Json = "{\"name\":\"Test\",\"age\":20}";

            Assert.Throws<InvalidOperationException>(() =>
            {
                _migrationManager.Migrate<UserDataV3>(v1Json, 1, 3);
            });
        }

        /// <summary>
        /// 测试简单JSON迁移器
        /// </summary>
        [Test]
        public void Migrate_SimpleJsonMigrator_ShouldWork()
        {
            _migrationManager.RegisterMigrator<object>(new SimpleJsonMigrator());

            var oldJson = "{\"existingField\":\"value\"}";
            var newJson = _migrationManager.Migrate<object>(oldJson, 1, 2);

            Assert.IsTrue(newJson.Contains("newField"));
            Assert.IsTrue(newJson.Contains("added"));
        }

        #endregion

        #region 泛型迁移器测试

        /// <summary>
        /// 测试泛型迁移器的MigrateTyped方法
        /// </summary>
        [Test]
        public void TypedMigrator_MigrateTyped_ShouldWork()
        {
            var migrator = new UserDataMigratorV1ToV2();
            var v1Data = new UserDataV1 { name = "Alice", age = 28 };

            var v2Data = migrator.MigrateTyped(v1Data);

            Assert.AreEqual("Alice", v2Data.name);
            Assert.AreEqual(28, v2Data.age);
            Assert.AreEqual("unknown@example.com", v2Data.email);
        }

        /// <summary>
        /// 测试泛型迁移器的Migrate方法（JSON字符串）
        /// </summary>
        [Test]
        public void TypedMigrator_Migrate_ShouldWork()
        {
            var migrator = new UserDataMigratorV1ToV2();
            var v1Json = JsonUtility.ToJson(new UserDataV1 { name = "Bob", age = 35 });

            var v2Json = migrator.Migrate(v1Json);
            var v2Data = JsonUtility.FromJson<UserDataV2>(v2Json);

            Assert.AreEqual("Bob", v2Data.name);
            Assert.AreEqual(35, v2Data.age);
        }

        #endregion
    }
}
