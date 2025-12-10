using System;
using System.IO;
using NUnit.Framework;
using xFrame.Runtime.Logging;
using xFrame.Runtime.Persistence;
using xFrame.Runtime.Persistence.Migration;
using xFrame.Runtime.Persistence.Security;
using xFrame.Runtime.Persistence.Storage;
using xFrame.Runtime.Serialization;

namespace xFrame.Tests.PersistenceTests
{
    /// <summary>
    /// 持久化管理器单元测试
    /// 测试PersistenceManager的核心功能
    /// </summary>
    [TestFixture]
    public class PersistenceManagerTests
    {
        /// <summary>
        /// 测试用的简单数据类
        /// </summary>
        [Serializable]
        private class PlayerData
        {
            public string playerName;
            public int level;
            public float experience;
        }

        /// <summary>
        /// 版本化的游戏设置数据
        /// </summary>
        [Serializable]
        private class GameSettingsV1 : IVersionedData
        {
            public int CurrentVersion => 1;
            public float volume;
            public bool fullscreen;
        }

        /// <summary>
        /// 版本2的游戏设置数据
        /// </summary>
        [Serializable]
        private class GameSettingsV2 : IVersionedData
        {
            public int CurrentVersion => 2;
            public float masterVolume;
            public float musicVolume;
            public float sfxVolume;
            public bool fullscreen;
        }

        /// <summary>
        /// 设置迁移器
        /// </summary>
        private class SettingsMigratorV1ToV2 : DataMigratorBase<GameSettingsV1, GameSettingsV2>
        {
            public override int FromVersion => 1;
            public override int ToVersion => 2;

            public override GameSettingsV2 MigrateTyped(GameSettingsV1 oldData)
            {
                return new GameSettingsV2
                {
                    masterVolume = oldData.volume,
                    musicVolume = oldData.volume * 0.8f,
                    sfxVolume = oldData.volume,
                    fullscreen = oldData.fullscreen
                };
            }
        }

        private IPersistenceManager _persistenceManager;
        private MemoryPersistenceProvider _memoryProvider;
        private ISerializer _serializer;
        private PersistenceConfig _config;
        private string _testBasePath;

        [SetUp]
        public void SetUp()
        {
            // 初始化日志系统
            try
            {
                var logManager = new XLogManager();
                XLog.Initialize(logManager);
            }
            catch
            {
                // 可能已经初始化
            }

            _serializer = new JsonSerializer();
            _testBasePath = Path.Combine(Path.GetTempPath(), "xFrame_PersistenceManagerTests_" + Guid.NewGuid().ToString("N"));
            _config = PersistenceConfig.CreateDefault(_testBasePath);
            _config.EnableValidation = false; // 简化测试

            _memoryProvider = new MemoryPersistenceProvider(_serializer);
            _persistenceManager = new PersistenceManager(
                _memoryProvider,
                _serializer,
                _config);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testBasePath))
            {
                Directory.Delete(_testBasePath, true);
            }
        }

        #region 基础保存和加载测试

        /// <summary>
        /// 测试保存和加载数据（使用默认键）
        /// </summary>
        [Test]
        public void SaveAndLoad_WithDefaultKey_ShouldWork()
        {
            var data = new PlayerData { playerName = "Hero", level = 10, experience = 1500.5f };

            _persistenceManager.Save(data);
            var loaded = _persistenceManager.Load<PlayerData>();

            Assert.IsNotNull(loaded);
            Assert.AreEqual("Hero", loaded.playerName);
            Assert.AreEqual(10, loaded.level);
            Assert.AreEqual(1500.5f, loaded.experience, 0.01f);
        }

        /// <summary>
        /// 测试保存和加载数据（使用自定义键）
        /// </summary>
        [Test]
        public void SaveAndLoad_WithCustomKey_ShouldWork()
        {
            var data = new PlayerData { playerName = "Player1", level = 5 };

            _persistenceManager.Save("custom_player_key", data);
            var loaded = _persistenceManager.Load<PlayerData>("custom_player_key");

            Assert.IsNotNull(loaded);
            Assert.AreEqual("Player1", loaded.playerName);
            Assert.AreEqual(5, loaded.level);
        }

        /// <summary>
        /// 测试加载不存在的数据
        /// </summary>
        [Test]
        public void Load_NonExistent_ShouldReturnDefault()
        {
            var loaded = _persistenceManager.Load<PlayerData>("non_existent_key");
            Assert.IsNull(loaded);
        }

        /// <summary>
        /// 测试LoadOrDefault
        /// </summary>
        [Test]
        public void LoadOrDefault_NonExistent_ShouldReturnDefault()
        {
            var defaultData = new PlayerData { playerName = "Default", level = 1 };

            var loaded = _persistenceManager.LoadOrDefault("non_existent", defaultData);

            Assert.AreEqual("Default", loaded.playerName);
            Assert.AreEqual(1, loaded.level);
        }

        /// <summary>
        /// 测试LoadOrDefault（数据存在时）
        /// </summary>
        [Test]
        public void LoadOrDefault_Existing_ShouldReturnSavedData()
        {
            var savedData = new PlayerData { playerName = "Saved", level = 20 };
            var defaultData = new PlayerData { playerName = "Default", level = 1 };

            _persistenceManager.Save("player", savedData);
            var loaded = _persistenceManager.LoadOrDefault("player", defaultData);

            Assert.AreEqual("Saved", loaded.playerName);
            Assert.AreEqual(20, loaded.level);
        }

        #endregion

        #region 存在性检查和删除测试

        /// <summary>
        /// 测试Exists方法
        /// </summary>
        [Test]
        public void Exists_ShouldReturnCorrectly()
        {
            Assert.IsFalse(_persistenceManager.Exists<PlayerData>());

            _persistenceManager.Save(new PlayerData { playerName = "Test" });

            Assert.IsTrue(_persistenceManager.Exists<PlayerData>());
        }

        /// <summary>
        /// 测试Delete方法
        /// </summary>
        [Test]
        public void Delete_ShouldWork()
        {
            _persistenceManager.Save(new PlayerData { playerName = "ToDelete" });
            Assert.IsTrue(_persistenceManager.Exists<PlayerData>());

            var result = _persistenceManager.Delete<PlayerData>();

            Assert.IsTrue(result);
            Assert.IsFalse(_persistenceManager.Exists<PlayerData>());
        }

        /// <summary>
        /// 测试删除不存在的数据
        /// </summary>
        [Test]
        public void Delete_NonExistent_ShouldReturnFalse()
        {
            var result = _persistenceManager.Delete("non_existent_key");
            Assert.IsFalse(result);
        }

        #endregion

        #region 默认键生成测试

        /// <summary>
        /// 测试GetDefaultKey
        /// </summary>
        [Test]
        public void GetDefaultKey_ShouldReturnTypeName()
        {
            var key = _persistenceManager.GetDefaultKey<PlayerData>();
            Assert.IsTrue(key.Contains("PlayerData"));
        }

        /// <summary>
        /// 测试不同类型生成不同键
        /// </summary>
        [Test]
        public void GetDefaultKey_DifferentTypes_ShouldBeDifferent()
        {
            var key1 = _persistenceManager.GetDefaultKey<PlayerData>();
            var key2 = _persistenceManager.GetDefaultKey<GameSettingsV1>();

            Assert.AreNotEqual(key1, key2);
        }

        #endregion

        #region 数据校验测试

        /// <summary>
        /// 测试启用数据校验
        /// </summary>
        [Test]
        public void SaveAndLoad_WithValidation_ShouldWork()
        {
            var configWithValidation = PersistenceConfig.CreateDefault(_testBasePath);
            configWithValidation.EnableValidation = true;
            configWithValidation.ValidatorType = ValidatorType.Sha256;

            var provider = new MemoryPersistenceProvider(_serializer);
            var manager = new PersistenceManager(provider, _serializer, configWithValidation);

            var data = new PlayerData { playerName = "Validated", level = 50 };

            manager.Save(data);
            var loaded = manager.Load<PlayerData>();

            Assert.IsNotNull(loaded);
            Assert.AreEqual("Validated", loaded.playerName);
        }

        #endregion

        #region 加密测试

        /// <summary>
        /// 测试启用加密
        /// </summary>
        [Test]
        public void SaveAndLoad_WithEncryption_ShouldWork()
        {
            var encryptor = new AesEncryptor("test_encryption_key");
            var provider = new MemoryPersistenceProvider(_serializer);
            var manager = new PersistenceManager(provider, _serializer, _config, encryptor);

            var data = new PlayerData { playerName = "Encrypted", level = 100 };

            manager.Save(data);
            var loaded = manager.Load<PlayerData>();

            Assert.IsNotNull(loaded);
            Assert.AreEqual("Encrypted", loaded.playerName);
            Assert.AreEqual(100, loaded.level);
        }

        /// <summary>
        /// 测试加密后原始数据不可读
        /// </summary>
        [Test]
        public void Save_WithEncryption_RawDataShouldNotBeReadable()
        {
            var encryptor = new AesEncryptor("secret_key");
            var provider = new MemoryPersistenceProvider(_serializer);
            var manager = new PersistenceManager(provider, _serializer, _config, encryptor);

            var data = new PlayerData { playerName = "SecretPlayer", level = 99 };
            manager.Save("encrypted_data", data);

            // 直接读取原始数据
            var rawData = provider.LoadRaw("encrypted_data");
            var rawString = System.Text.Encoding.UTF8.GetString(rawData);

            // 原始数据不应该包含明文
            Assert.IsFalse(rawString.Contains("SecretPlayer"));
        }

        #endregion

        #region 迁移器注册测试

        /// <summary>
        /// 测试注册迁移器
        /// </summary>
        [Test]
        public void RegisterMigrator_ShouldWork()
        {
            var migrator = new SettingsMigratorV1ToV2();

            _persistenceManager.RegisterMigrator<GameSettingsV2>(migrator);

            var migrators = _persistenceManager.MigrationManager.GetMigrators<GameSettingsV2>();
            Assert.AreEqual(1, migrators.Count);
        }

        #endregion

        #region 配置测试

        /// <summary>
        /// 测试Config属性
        /// </summary>
        [Test]
        public void Config_ShouldReturnConfiguration()
        {
            Assert.IsNotNull(_persistenceManager.Config);
            Assert.AreEqual(_testBasePath, _persistenceManager.Config.BasePath);
        }

        /// <summary>
        /// 测试MigrationManager属性
        /// </summary>
        [Test]
        public void MigrationManager_ShouldNotBeNull()
        {
            Assert.IsNotNull(_persistenceManager.MigrationManager);
        }

        #endregion

        #region 异常处理测试

        /// <summary>
        /// 测试空provider抛出异常
        /// </summary>
        [Test]
        public void Constructor_NullProvider_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new PersistenceManager(null, _serializer, _config);
            });
        }

        /// <summary>
        /// 测试空serializer抛出异常
        /// </summary>
        [Test]
        public void Constructor_NullSerializer_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new PersistenceManager(_memoryProvider, null, _config);
            });
        }

        /// <summary>
        /// 测试空config抛出异常
        /// </summary>
        [Test]
        public void Constructor_NullConfig_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new PersistenceManager(_memoryProvider, _serializer, null);
            });
        }

        #endregion

        #region 多数据类型测试

        /// <summary>
        /// 测试保存多种类型的数据
        /// </summary>
        [Test]
        public void SaveAndLoad_MultipleTypes_ShouldWork()
        {
            var playerData = new PlayerData { playerName = "Player", level = 10 };
            var settingsData = new GameSettingsV1 { volume = 0.8f, fullscreen = true };

            _persistenceManager.Save(playerData);
            _persistenceManager.Save(settingsData);

            var loadedPlayer = _persistenceManager.Load<PlayerData>();
            var loadedSettings = _persistenceManager.Load<GameSettingsV1>();

            Assert.AreEqual("Player", loadedPlayer.playerName);
            Assert.AreEqual(0.8f, loadedSettings.volume, 0.01f);
            Assert.IsTrue(loadedSettings.fullscreen);
        }

        /// <summary>
        /// 测试覆盖保存
        /// </summary>
        [Test]
        public void Save_Overwrite_ShouldWork()
        {
            _persistenceManager.Save(new PlayerData { playerName = "First", level = 1 });
            _persistenceManager.Save(new PlayerData { playerName = "Second", level = 2 });

            var loaded = _persistenceManager.Load<PlayerData>();

            Assert.AreEqual("Second", loaded.playerName);
            Assert.AreEqual(2, loaded.level);
        }

        #endregion
    }
}
