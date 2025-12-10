using Cysharp.Threading.Tasks;
using xFrame.Runtime.Serialization;

namespace xFrame.Runtime.Persistence.Storage
{
    /// <summary>
    /// 持久化提供者基类
    /// 提供序列化器集成和通用实现
    /// </summary>
    public abstract class PersistenceProviderBase : IPersistenceProvider
    {
        /// <summary>
        /// 序列化器
        /// </summary>
        protected readonly ISerializer Serializer;

        /// <summary>
        /// 提供者名称
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// 创建持久化提供者
        /// </summary>
        /// <param name="serializer">序列化器</param>
        protected PersistenceProviderBase(ISerializer serializer)
        {
            Serializer = serializer;
        }

        /// <summary>
        /// 同步保存数据
        /// </summary>
        public virtual void Save<T>(string key, T data)
        {
            var bytes = Serializer.Serialize(data);
            SaveRaw(key, bytes);
        }

        /// <summary>
        /// 异步保存数据
        /// </summary>
        public virtual async UniTask SaveAsync<T>(string key, T data)
        {
            var bytes = Serializer.Serialize(data);
            await SaveRawAsync(key, bytes);
        }

        /// <summary>
        /// 同步加载数据
        /// </summary>
        public virtual T Load<T>(string key)
        {
            var bytes = LoadRaw(key);
            if (bytes == null || bytes.Length == 0)
            {
                return default;
            }

            return Serializer.Deserialize<T>(bytes);
        }

        /// <summary>
        /// 异步加载数据
        /// </summary>
        public virtual async UniTask<T> LoadAsync<T>(string key)
        {
            var bytes = await LoadRawAsync(key);
            if (bytes == null || bytes.Length == 0)
            {
                return default;
            }

            return Serializer.Deserialize<T>(bytes);
        }

        /// <summary>
        /// 检查数据是否存在
        /// </summary>
        public abstract bool Exists(string key);

        /// <summary>
        /// 异步检查数据是否存在
        /// </summary>
        public virtual UniTask<bool> ExistsAsync(string key)
        {
            return UniTask.FromResult(Exists(key));
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        public abstract bool Delete(string key);

        /// <summary>
        /// 异步删除数据
        /// </summary>
        public virtual UniTask<bool> DeleteAsync(string key)
        {
            return UniTask.FromResult(Delete(key));
        }

        /// <summary>
        /// 保存原始字节数据
        /// </summary>
        public abstract void SaveRaw(string key, byte[] data);

        /// <summary>
        /// 异步保存原始字节数据
        /// </summary>
        public abstract UniTask SaveRawAsync(string key, byte[] data);

        /// <summary>
        /// 加载原始字节数据
        /// </summary>
        public abstract byte[] LoadRaw(string key);

        /// <summary>
        /// 异步加载原始字节数据
        /// </summary>
        public abstract UniTask<byte[]> LoadRawAsync(string key);
    }
}
