using System;
using System.Collections.Generic;
using NUnit.Framework;
using xFrame.Runtime.EventBus;
using xFrame.Runtime.Serialization;

namespace xFrame.Tests.SerializationTests
{
    /// <summary>
    /// 序列化模块集成测试
    /// 测试多个序列化器协同工作的场景
    /// </summary>
    [TestFixture]
    public class SerializationIntegrationTests
    {
        /// <summary>
        /// 游戏存档数据类
        /// </summary>
        [Serializable]
        private class GameSaveData
        {
            public string PlayerName;
            public int Level;
            public float PlayTime;
            public PlayerStats Stats;
            public InventoryData Inventory;
        }

        /// <summary>
        /// 玩家属性数据类
        /// </summary>
        [Serializable]
        private class PlayerStats
        {
            public int Health;
            public int MaxHealth;
            public int Attack;
            public int Defense;
            public float CritRate;
        }

        /// <summary>
        /// 背包数据类
        /// </summary>
        [Serializable]
        private class InventoryData
        {
            public int Gold;
            public ItemData[] Items;
        }

        /// <summary>
        /// 物品数据类
        /// </summary>
        [Serializable]
        private class ItemData
        {
            public string ItemId;
            public string ItemName;
            public int Count;
            public int Quality;
        }

        /// <summary>
        /// 配置数据类
        /// </summary>
        [Serializable]
        private class ConfigData
        {
            public float MusicVolume;
            public float SoundVolume;
            public int GraphicsQuality;
            public bool FullScreen;
            public string Language;
        }

        private ISerializerManager _serializerManager;

        [SetUp]
        public void SetUp()
        {
            _serializerManager = new SerializerManager();

            // 清理事件监听器
            xFrameEventBus.ClearListeners<SerializerRegisteredEvent>();
            xFrameEventBus.ClearListeners<SerializerUnregisteredEvent>();
            xFrameEventBus.ClearListeners<DefaultSerializerChangedEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            xFrameEventBus.ClearListeners<SerializerRegisteredEvent>();
            xFrameEventBus.ClearListeners<SerializerUnregisteredEvent>();
            xFrameEventBus.ClearListeners<DefaultSerializerChangedEvent>();
        }

        #region 复杂数据结构测试

        /// <summary>
        /// 测试游戏存档数据的序列化和反序列化
        /// </summary>
        [Test]
        public void GameSaveData_SerializeAndDeserialize_ShouldWork()
        {
            // Arrange
            var saveData = CreateTestGameSaveData();

            // Act
            var json = _serializerManager.SerializeToString(saveData);
            var restored = _serializerManager.DeserializeFromString<GameSaveData>(json);

            // Assert
            Assert.IsNotNull(restored);
            Assert.AreEqual(saveData.PlayerName, restored.PlayerName);
            Assert.AreEqual(saveData.Level, restored.Level);
            Assert.AreEqual(saveData.PlayTime, restored.PlayTime, 0.01f);

            // 验证嵌套的PlayerStats
            Assert.IsNotNull(restored.Stats);
            Assert.AreEqual(saveData.Stats.Health, restored.Stats.Health);
            Assert.AreEqual(saveData.Stats.MaxHealth, restored.Stats.MaxHealth);
            Assert.AreEqual(saveData.Stats.Attack, restored.Stats.Attack);
            Assert.AreEqual(saveData.Stats.Defense, restored.Stats.Defense);
            Assert.AreEqual(saveData.Stats.CritRate, restored.Stats.CritRate, 0.01f);

            // 验证嵌套的Inventory
            Assert.IsNotNull(restored.Inventory);
            Assert.AreEqual(saveData.Inventory.Gold, restored.Inventory.Gold);
            Assert.AreEqual(saveData.Inventory.Items.Length, restored.Inventory.Items.Length);

            // 验证物品数据
            for (int i = 0; i < saveData.Inventory.Items.Length; i++)
            {
                Assert.AreEqual(saveData.Inventory.Items[i].ItemId, restored.Inventory.Items[i].ItemId);
                Assert.AreEqual(saveData.Inventory.Items[i].ItemName, restored.Inventory.Items[i].ItemName);
                Assert.AreEqual(saveData.Inventory.Items[i].Count, restored.Inventory.Items[i].Count);
                Assert.AreEqual(saveData.Inventory.Items[i].Quality, restored.Inventory.Items[i].Quality);
            }
        }

        /// <summary>
        /// 测试配置数据的序列化和反序列化
        /// </summary>
        [Test]
        public void ConfigData_SerializeAndDeserialize_ShouldWork()
        {
            // Arrange
            var config = new ConfigData
            {
                MusicVolume = 0.8f,
                SoundVolume = 0.6f,
                GraphicsQuality = 2,
                FullScreen = true,
                Language = "zh-CN"
            };

            // Act
            var json = _serializerManager.SerializeToString(config);
            var restored = _serializerManager.DeserializeFromString<ConfigData>(json);

            // Assert
            Assert.IsNotNull(restored);
            Assert.AreEqual(config.MusicVolume, restored.MusicVolume, 0.01f);
            Assert.AreEqual(config.SoundVolume, restored.SoundVolume, 0.01f);
            Assert.AreEqual(config.GraphicsQuality, restored.GraphicsQuality);
            Assert.AreEqual(config.FullScreen, restored.FullScreen);
            Assert.AreEqual(config.Language, restored.Language);
        }

        /// <summary>
        /// 测试字节数组序列化游戏存档
        /// </summary>
        [Test]
        public void GameSaveData_SerializeToBytes_ShouldWork()
        {
            // Arrange
            var saveData = CreateTestGameSaveData();

            // Act
            var bytes = _serializerManager.Serialize(saveData);
            var restored = _serializerManager.Deserialize<GameSaveData>(bytes);

            // Assert
            Assert.IsNotNull(bytes);
            Assert.IsTrue(bytes.Length > 0);
            Assert.IsNotNull(restored);
            Assert.AreEqual(saveData.PlayerName, restored.PlayerName);
            Assert.AreEqual(saveData.Level, restored.Level);
        }

        #endregion

        #region 多序列化器切换测试

        /// <summary>
        /// 测试注册多个序列化器并切换
        /// </summary>
        [Test]
        public void MultipleSerializers_SwitchDefault_ShouldWork()
        {
            // Arrange
            var prettySerializer = new JsonSerializer(true);
            _serializerManager.RegisterSerializer("PrettyJson", prettySerializer);

            var data = new ConfigData { MusicVolume = 0.5f, Language = "en-US" };

            // Act - 使用默认序列化器
            var normalJson = _serializerManager.SerializeToString(data);

            // 切换到格式化序列化器
            _serializerManager.SetDefaultSerializer("PrettyJson");
            var prettyJson = _serializerManager.SerializeToString(data);

            // Assert
            Assert.IsNotNull(normalJson);
            Assert.IsNotNull(prettyJson);
            Assert.IsTrue(prettyJson.Length >= normalJson.Length, "格式化JSON应该更长");

            // 两种格式都应该能正确反序列化
            var fromNormal = _serializerManager.DeserializeFromString<ConfigData>(normalJson);
            var fromPretty = _serializerManager.DeserializeFromString<ConfigData>(prettyJson);

            Assert.AreEqual(data.MusicVolume, fromNormal.MusicVolume, 0.01f);
            Assert.AreEqual(data.MusicVolume, fromPretty.MusicVolume, 0.01f);
        }

        /// <summary>
        /// 测试使用指定序列化器而不改变默认
        /// </summary>
        [Test]
        public void UseSpecificSerializer_WithoutChangingDefault_ShouldWork()
        {
            // Arrange
            var prettySerializer = new JsonSerializer(true);
            _serializerManager.RegisterSerializer("PrettyJson", prettySerializer);

            var data = new ConfigData { MusicVolume = 0.7f, Language = "ja-JP" };

            // Act
            var defaultJson = _serializerManager.SerializeToString(data);
            var prettyJson = _serializerManager.SerializeToString("PrettyJson", data);

            // Assert - 默认序列化器应该没变
            Assert.AreEqual(JsonSerializer.SerializerName, _serializerManager.DefaultSerializer.Name);

            // 两种方式都应该能正常工作
            Assert.IsNotNull(defaultJson);
            Assert.IsNotNull(prettyJson);
        }

        #endregion

        #region 事件集成测试

        /// <summary>
        /// 测试序列化器生命周期事件
        /// </summary>
        [Test]
        public void SerializerLifecycle_ShouldRaiseCorrectEvents()
        {
            // Arrange
            var events = new List<string>();

            xFrameEventBus.SubscribeTo<SerializerRegisteredEvent>((ref SerializerRegisteredEvent e) =>
            {
                events.Add($"Registered:{e.SerializerName}");
            });

            xFrameEventBus.SubscribeTo<DefaultSerializerChangedEvent>((ref DefaultSerializerChangedEvent e) =>
            {
                events.Add($"DefaultChanged:{e.PreviousSerializerName}->{e.NewSerializerName}");
            });

            xFrameEventBus.SubscribeTo<SerializerUnregisteredEvent>((ref SerializerUnregisteredEvent e) =>
            {
                events.Add($"Unregistered:{e.SerializerName}");
            });

            // Act
            var customSerializer = new JsonSerializer(true);
            _serializerManager.RegisterSerializer("Custom", customSerializer);
            _serializerManager.SetDefaultSerializer("Custom");
            _serializerManager.UnregisterSerializer("Custom");

            // Assert
            Assert.AreEqual(3, events.Count);
            Assert.AreEqual("Registered:Custom", events[0]);
            Assert.AreEqual("DefaultChanged:Json->Custom", events[1]);
            Assert.AreEqual("Unregistered:Custom", events[2]);
        }

        #endregion

        #region 边界条件测试

        /// <summary>
        /// 测试空数组的序列化
        /// </summary>
        [Test]
        public void EmptyArray_SerializeAndDeserialize_ShouldWork()
        {
            // Arrange
            var inventory = new InventoryData
            {
                Gold = 0,
                Items = new ItemData[0]
            };

            // Act
            var json = _serializerManager.SerializeToString(inventory);
            var restored = _serializerManager.DeserializeFromString<InventoryData>(json);

            // Assert
            Assert.IsNotNull(restored);
            Assert.AreEqual(0, restored.Gold);
            Assert.IsNotNull(restored.Items);
            Assert.AreEqual(0, restored.Items.Length);
        }

        /// <summary>
        /// 测试null嵌套对象的序列化
        /// </summary>
        [Test]
        public void NullNestedObject_SerializeAndDeserialize_ShouldWork()
        {
            // Arrange
            var saveData = new GameSaveData
            {
                PlayerName = "TestPlayer",
                Level = 1,
                PlayTime = 0f,
                Stats = null,
                Inventory = null
            };

            // Act
            var json = _serializerManager.SerializeToString(saveData);
            var restored = _serializerManager.DeserializeFromString<GameSaveData>(json);

            // Assert
            Assert.IsNotNull(restored);
            Assert.AreEqual(saveData.PlayerName, restored.PlayerName);
            Assert.AreEqual(saveData.Level, restored.Level);
        }

        /// <summary>
        /// 测试特殊字符的序列化
        /// </summary>
        [Test]
        public void SpecialCharacters_SerializeAndDeserialize_ShouldWork()
        {
            // Arrange
            var config = new ConfigData
            {
                Language = "中文测试",
                MusicVolume = 1.0f
            };

            // Act
            var json = _serializerManager.SerializeToString(config);
            var restored = _serializerManager.DeserializeFromString<ConfigData>(json);

            // Assert
            Assert.IsNotNull(restored);
            Assert.AreEqual("中文测试", restored.Language);
        }

        /// <summary>
        /// 测试大数据量的序列化性能
        /// </summary>
        [Test]
        public void LargeData_SerializeAndDeserialize_ShouldWork()
        {
            // Arrange
            var items = new ItemData[100];
            for (int i = 0; i < 100; i++)
            {
                items[i] = new ItemData
                {
                    ItemId = $"item_{i:D4}",
                    ItemName = $"物品{i}",
                    Count = i * 10,
                    Quality = i % 5
                };
            }

            var inventory = new InventoryData
            {
                Gold = 999999,
                Items = items
            };

            // Act
            var json = _serializerManager.SerializeToString(inventory);
            var restored = _serializerManager.DeserializeFromString<InventoryData>(json);

            // Assert
            Assert.IsNotNull(restored);
            Assert.AreEqual(100, restored.Items.Length);
            Assert.AreEqual(inventory.Gold, restored.Gold);

            // 验证部分数据
            Assert.AreEqual("item_0000", restored.Items[0].ItemId);
            Assert.AreEqual("item_0099", restored.Items[99].ItemId);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建测试用的游戏存档数据
        /// </summary>
        private GameSaveData CreateTestGameSaveData()
        {
            return new GameSaveData
            {
                PlayerName = "TestHero",
                Level = 50,
                PlayTime = 3600.5f,
                Stats = new PlayerStats
                {
                    Health = 800,
                    MaxHealth = 1000,
                    Attack = 150,
                    Defense = 80,
                    CritRate = 0.25f
                },
                Inventory = new InventoryData
                {
                    Gold = 50000,
                    Items = new[]
                    {
                        new ItemData { ItemId = "sword_001", ItemName = "铁剑", Count = 1, Quality = 2 },
                        new ItemData { ItemId = "potion_hp", ItemName = "生命药水", Count = 99, Quality = 1 },
                        new ItemData { ItemId = "armor_001", ItemName = "皮甲", Count = 1, Quality = 2 }
                    }
                }
            };
        }

        #endregion
    }
}
