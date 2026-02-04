using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using xFrame.Runtime.Serialization;

namespace xFrame.Runtime.Persistence.Storage
{
    /// <summary>
    /// 内存持久化提供者
    /// 将数据存储在内存中，适用于测试或缓存场景
    /// 注意：应用关闭后数据会丢失
    /// </summary>
    public class MemoryPersistenceProvider : PersistenceProviderBase
    {
        private readonly Dictionary<string, byte[]> _storage = new();
        private readonly object _lock = new();

        /// <summary>
        /// 提供者名称
        /// </summary>
        public override string Name => "Memory";

        /// <summary>
        /// 创建内存持久化提供者
        /// </summary>
        /// <param name="serializer">序列化器</param>
        public MemoryPersistenceProvider(ISerializer serializer) : base(serializer)
        {
        }

        /// <summary>
        /// 检查数据是否存在
        /// </summary>
        public override bool Exists(string key)
        {
            lock (_lock)
            {
                return _storage.ContainsKey(key);
            }
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        public override bool Delete(string key)
        {
            lock (_lock)
            {
                return _storage.Remove(key);
            }
        }

        /// <summary>
        /// 保存原始字节数据
        /// </summary>
        public override void SaveRaw(string key, byte[] data)
        {
            lock (_lock)
            {
                if (data == null)
                {
                    _storage.Remove(key);
                }
                else
                {
                    // 复制数据以避免外部修改
                    var copy = new byte[data.Length];
                    data.CopyTo(copy, 0);
                    _storage[key] = copy;
                }
            }
        }

        /// <summary>
        /// 异步保存原始字节数据
        /// </summary>
        public override UniTask SaveRawAsync(string key, byte[] data)
        {
            SaveRaw(key, data);
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 加载原始字节数据
        /// </summary>
        public override byte[] LoadRaw(string key)
        {
            lock (_lock)
            {
                if (_storage.TryGetValue(key, out var data))
                {
                    // 返回副本以避免外部修改
                    var copy = new byte[data.Length];
                    data.CopyTo(copy, 0);
                    return copy;
                }

                return null;
            }
        }

        /// <summary>
        /// 异步加载原始字节数据
        /// </summary>
        public override UniTask<byte[]> LoadRawAsync(string key)
        {
            return UniTask.FromResult(LoadRaw(key));
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _storage.Clear();
            }
        }

        /// <summary>
        /// 获取所有存储的键
        /// </summary>
        /// <returns>键列表</returns>
        public IReadOnlyCollection<string> GetAllKeys()
        {
            lock (_lock)
            {
                return new List<string>(_storage.Keys);
            }
        }

        /// <summary>
        /// 获取存储的数据数量
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _storage.Count;
                }
            }
        }
    }
}
