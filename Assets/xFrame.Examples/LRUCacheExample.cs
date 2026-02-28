using System;
using System.Collections.Generic;
using UnityEngine;
using xFrame.Runtime.DataStructures;
using Random = UnityEngine.Random;

namespace xFrame.Examples
{
    /// <summary>
    /// LRU缓存系统使用示例
    /// 演示如何使用LRU缓存来提高数据访问性能
    /// </summary>
    public class LRUCacheExample : MonoBehaviour
    {
        private ILRUCache<string, string> _configCache;

        // 缓存实例
        private ILRUCache<int, PlayerData> _playerCache;
        private TextureCache _textureCache;
        private ThreadSafeLRUCache<string, object> _threadSafeCache;

        /// <summary>
        /// 初始化各种缓存示例
        /// </summary>
        private void Start()
        {
            Debug.Log("=== LRU缓存系统示例开始 ===");

            // 示例1: 创建玩家数据缓存
            CreatePlayerDataCache();

            // 示例2: 创建配置缓存
            CreateConfigCache();

            // 示例3: 创建线程安全缓存
            CreateThreadSafeCache();

            // 示例4: 创建纹理缓存
            CreateTextureCache();

            // 示例5: 演示各种功能
            DemonstrateCacheFeatures();
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void OnDestroy()
        {
            Debug.Log("\n=== 清理LRU缓存资源 ===");

            _playerCache?.Clear();
            _configCache?.Clear();
            _threadSafeCache?.Dispose();

            Debug.Log("LRU缓存系统示例结束");
        }

        /// <summary>
        /// 在Inspector中显示缓存状态
        /// </summary>
        private void OnGUI()
        {
            if (_playerCache == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            GUILayout.Label("=== LRU缓存状态 ===");

            GUILayout.Label($"玩家缓存: {_playerCache.Count}/{_playerCache.Capacity}");
            GUILayout.Label($"配置缓存: {_configCache.Count}/{_configCache.Capacity}");
            GUILayout.Label($"线程安全缓存: {_threadSafeCache.Count}/{_threadSafeCache.Capacity}");
            GUILayout.Label(_textureCache.GetCacheStatus());

            GUILayout.Space(10);

            if (GUILayout.Button("获取随机玩家"))
            {
                var randomId = Random.Range(1, 11);
                if (_playerCache.TryGet(randomId, out var player))
                    Debug.Log($"获取玩家 {randomId}: {player}");
                else
                    Debug.Log($"玩家 {randomId} 不在缓存中");
            }

            if (GUILayout.Button("添加新玩家"))
            {
                var newId = Random.Range(100, 200);
                var newPlayer = new PlayerData(
                    newId,
                    $"NewPlayer_{newId}",
                    Random.Range(1, 100),
                    Random.Range(0f, 5000f),
                    Vector3.zero
                );
                _playerCache.Put(newId, newPlayer);
                Debug.Log($"添加新玩家: {newPlayer}");
            }

            if (GUILayout.Button("清空玩家缓存"))
            {
                _playerCache.Clear();
                Debug.Log("玩家缓存已清空");
            }

            if (GUILayout.Button("加载随机纹理"))
            {
                string[] paths = { "tex1.png", "tex2.png", "tex3.png", "tex4.png", "tex5.png" };
                var randomPath = paths[Random.Range(0, paths.Length)];
                _textureCache.GetTexture(randomPath);
            }

            if (GUILayout.Button("清空纹理缓存")) _textureCache.ClearCache();

            GUILayout.EndArea();
        }

        /// <summary>
        /// 创建玩家数据缓存
        /// </summary>
        private void CreatePlayerDataCache()
        {
            Debug.Log("\n--- 示例1: 玩家数据缓存 ---");

            // 创建容量为100的玩家数据缓存
            _playerCache = LRUCacheFactory.CreateIntCache<PlayerData>(100);

            // 模拟添加玩家数据
            for (var i = 1; i <= 10; i++)
            {
                var player = new PlayerData(
                    i,
                    $"Player_{i}",
                    Random.Range(1, 50),
                    Random.Range(0f, 1000f),
                    new Vector3(Random.Range(-100f, 100f), 0, Random.Range(-100f, 100f))
                );
                _playerCache.Put(i, player);
            }

            Debug.Log($"玩家缓存初始化完成: {_playerCache.Count} 个玩家");
        }

        /// <summary>
        /// 创建配置缓存
        /// </summary>
        private void CreateConfigCache()
        {
            Debug.Log("\n--- 示例2: 配置缓存 ---");

            // 创建字符串到字符串的配置缓存
            _configCache = LRUCacheFactory.CreateStringToStringCache(50);

            // 添加一些配置项
            _configCache.Put("game.version", "1.0.0");
            _configCache.Put("player.max_health", "100");
            _configCache.Put("world.gravity", "-9.81");
            _configCache.Put("graphics.quality", "High");
            _configCache.Put("audio.master_volume", "0.8");

            Debug.Log($"配置缓存初始化完成: {_configCache.Count} 个配置项");
        }

        /// <summary>
        /// 创建线程安全缓存
        /// </summary>
        private void CreateThreadSafeCache()
        {
            Debug.Log("\n--- 示例3: 线程安全缓存 ---");

            // 创建线程安全的通用对象缓存
            _threadSafeCache = new ThreadSafeLRUCache<string, object>(200);

            // 添加不同类型的数据
            _threadSafeCache.Put("int_value", 42);
            _threadSafeCache.Put("float_value", 3.14f);
            _threadSafeCache.Put("string_value", "Hello World");
            _threadSafeCache.Put("vector_value", Vector3.one);
            _threadSafeCache.Put("bool_value", true);

            Debug.Log($"线程安全缓存初始化完成: {_threadSafeCache.Count} 个对象");
        }

        /// <summary>
        /// 创建纹理缓存
        /// </summary>
        private void CreateTextureCache()
        {
            Debug.Log("\n--- 示例4: 纹理缓存 ---");

            // 创建纹理缓存管理器
            _textureCache = new TextureCache(20);

            Debug.Log("纹理缓存管理器创建完成");
        }

        /// <summary>
        /// 演示缓存的各种功能
        /// </summary>
        private void DemonstrateCacheFeatures()
        {
            Debug.Log("\n--- 示例5: 缓存功能演示 ---");

            // 演示玩家数据缓存
            DemonstratePlayerCache();

            // 演示配置缓存
            DemonstrateConfigCache();

            // 演示LRU淘汰机制
            DemonstrateLRUEviction();

            // 演示纹理缓存
            DemonstrateTextureCache();

            // 演示缓存统计
            DemonstrateCacheStatistics();
        }

        /// <summary>
        /// 演示玩家数据缓存功能
        /// </summary>
        private void DemonstratePlayerCache()
        {
            Debug.Log("\n-- 玩家数据缓存演示 --");

            // 获取玩家数据
            if (_playerCache.TryGet(1, out var player1)) Debug.Log($"获取玩家1: {player1}");

            if (_playerCache.TryGet(5, out var player5)) Debug.Log($"获取玩家5: {player5}");

            // 更新玩家数据
            var updatedPlayer = new PlayerData(1, "UpdatedPlayer_1", 25, 500f, Vector3.zero);
            _playerCache.Put(1, updatedPlayer);
            Debug.Log($"更新玩家1数据: {_playerCache.Get(1)}");

            // 检查玩家是否存在
            Debug.Log($"玩家3是否存在: {_playerCache.ContainsKey(3)}");
            Debug.Log($"玩家99是否存在: {_playerCache.ContainsKey(99)}");
        }

        /// <summary>
        /// 演示配置缓存功能
        /// </summary>
        private void DemonstrateConfigCache()
        {
            Debug.Log("\n-- 配置缓存演示 --");

            // 获取配置值
            var version = _configCache.Get("game.version");
            var maxHealth = _configCache.Get("player.max_health");
            Debug.Log($"游戏版本: {version}");
            Debug.Log($"玩家最大血量: {maxHealth}");

            // 更新配置
            _configCache.Put("graphics.quality", "Ultra");
            Debug.Log($"图形质量已更新为: {_configCache.Get("graphics.quality")}");

            // 添加新配置
            _configCache.Put("network.timeout", "30");
            Debug.Log($"新增网络超时配置: {_configCache.Get("network.timeout")}");
        }

        /// <summary>
        /// 演示LRU淘汰机制
        /// </summary>
        private void DemonstrateLRUEviction()
        {
            Debug.Log("\n-- LRU淘汰机制演示 --");

            // 创建小容量缓存来演示淘汰
            var smallCache = LRUCacheFactory.Create<int, string>(3);

            // 添加数据
            smallCache.Put(1, "first");
            smallCache.Put(2, "second");
            smallCache.Put(3, "third");
            Debug.Log($"添加3个元素后: Count={smallCache.Count}");

            // 访问第一个元素，使其成为最近使用的
            var first = smallCache.Get(1);
            Debug.Log($"访问第一个元素: {first}");

            // 添加第四个元素，应该淘汰最久未使用的（元素2）
            smallCache.Put(4, "fourth");
            Debug.Log($"添加第四个元素后: Count={smallCache.Count}");

            // 检查哪些元素还在缓存中
            Debug.Log($"元素1是否存在: {smallCache.ContainsKey(1)}");
            Debug.Log($"元素2是否存在: {smallCache.ContainsKey(2)}"); // 应该被淘汰
            Debug.Log($"元素3是否存在: {smallCache.ContainsKey(3)}");
            Debug.Log($"元素4是否存在: {smallCache.ContainsKey(4)}");

            // 显示当前缓存中的所有键（按最近使用顺序）
            var keys = new List<int>(smallCache.Keys);
            Debug.Log($"当前缓存中的键（按最近使用顺序）: [{string.Join(", ", keys)}]");
        }

        /// <summary>
        /// 演示纹理缓存功能
        /// </summary>
        private void DemonstrateTextureCache()
        {
            Debug.Log("\n-- 纹理缓存演示 --");

            // 模拟加载纹理
            string[] texturePaths =
            {
                "textures/player.png",
                "textures/enemy.png",
                "textures/background.png",
                "textures/ui_button.png",
                "textures/particle.png"
            };

            // 第一次加载（从磁盘）
            foreach (var path in texturePaths)
            {
                var texture = _textureCache.GetTexture(path);
                Debug.Log($"加载纹理: {path}");
            }

            Debug.Log(_textureCache.GetCacheStatus());

            // 第二次加载（从缓存）
            Debug.Log("\n重新加载相同纹理:");
            for (var i = 0; i < Math.Min(3, texturePaths.Length); i++)
            {
                var texture = _textureCache.GetTexture(texturePaths[i]);
            }
        }

        /// <summary>
        /// 演示缓存统计信息
        /// </summary>
        private void DemonstrateCacheStatistics()
        {
            Debug.Log("\n-- 缓存统计信息 --");

            Debug.Log($"玩家缓存: {_playerCache.Count}/{_playerCache.Capacity}");
            Debug.Log($"配置缓存: {_configCache.Count}/{_configCache.Capacity}");
            Debug.Log($"线程安全缓存: {_threadSafeCache.Count}/{_threadSafeCache.Capacity}");
            Debug.Log(_textureCache.GetCacheStatus());

            // 显示玩家缓存中的所有键
            var playerKeys = new List<int>(_playerCache.Keys);
            Debug.Log($"玩家缓存中的键: [{string.Join(", ", playerKeys)}]");

            // 显示配置缓存中的所有键
            var configKeys = new List<string>(_configCache.Keys);
            Debug.Log($"配置缓存中的键: [{string.Join(", ", configKeys)}]");
        }

        /// <summary>
        /// 示例数据类
        /// </summary>
        [Serializable]
        public class PlayerData
        {
            public int playerId;
            public string playerName;
            public int level;
            public float experience;
            public Vector3 position;

            public PlayerData(int id, string name, int lvl, float exp, Vector3 pos)
            {
                playerId = id;
                playerName = name;
                level = lvl;
                experience = exp;
                position = pos;
            }

            public override string ToString()
            {
                return $"Player[{playerId}]: {playerName} (Lv.{level}, Exp:{experience:F1})";
            }
        }

        /// <summary>
        /// 示例纹理缓存类
        /// </summary>
        public class TextureCache
        {
            private readonly ILRUCache<string, Texture2D> _cache;

            public TextureCache(int capacity)
            {
                _cache = LRUCacheFactory.CreateStringCache<Texture2D>(capacity);
            }

            public Texture2D GetTexture(string path)
            {
                if (_cache.TryGet(path, out var texture))
                {
                    Debug.Log($"缓存命中: {path}");
                    return texture;
                }

                // 模拟从磁盘加载纹理
                texture = LoadTextureFromDisk(path);
                if (texture != null)
                {
                    _cache.Put(path, texture);
                    Debug.Log($"纹理已缓存: {path}");
                }

                return texture;
            }

            private Texture2D LoadTextureFromDisk(string path)
            {
                // 这里应该是实际的纹理加载逻辑
                Debug.Log($"从磁盘加载纹理: {path}");
                return new Texture2D(256, 256);
            }

            public void ClearCache()
            {
                _cache.Clear();
                Debug.Log("纹理缓存已清空");
            }

            public string GetCacheStatus()
            {
                return $"纹理缓存: {_cache.Count}/{_cache.Capacity}";
            }
        }
    }
}