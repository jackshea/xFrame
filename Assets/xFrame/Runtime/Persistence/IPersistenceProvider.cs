using Cysharp.Threading.Tasks;

namespace xFrame.Runtime.Persistence
{
    /// <summary>
    /// 持久化提供者接口
    /// 定义统一的数据持久化操作，屏蔽存储介质细节
    /// </summary>
    public interface IPersistenceProvider
    {
        /// <summary>
        /// 提供者名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 同步保存数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">存储键</param>
        /// <param name="data">要保存的数据</param>
        void Save<T>(string key, T data);

        /// <summary>
        /// 异步保存数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">存储键</param>
        /// <param name="data">要保存的数据</param>
        UniTask SaveAsync<T>(string key, T data);

        /// <summary>
        /// 同步加载数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">存储键</param>
        /// <returns>加载的数据，如果不存在则返回默认值</returns>
        T Load<T>(string key);

        /// <summary>
        /// 异步加载数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">存储键</param>
        /// <returns>加载的数据，如果不存在则返回默认值</returns>
        UniTask<T> LoadAsync<T>(string key);

        /// <summary>
        /// 检查指定键的数据是否存在
        /// </summary>
        /// <param name="key">存储键</param>
        /// <returns>是否存在</returns>
        bool Exists(string key);

        /// <summary>
        /// 异步检查指定键的数据是否存在
        /// </summary>
        /// <param name="key">存储键</param>
        /// <returns>是否存在</returns>
        UniTask<bool> ExistsAsync(string key);

        /// <summary>
        /// 删除指定键的数据
        /// </summary>
        /// <param name="key">存储键</param>
        /// <returns>是否成功删除</returns>
        bool Delete(string key);

        /// <summary>
        /// 异步删除指定键的数据
        /// </summary>
        /// <param name="key">存储键</param>
        /// <returns>是否成功删除</returns>
        UniTask<bool> DeleteAsync(string key);

        /// <summary>
        /// 保存原始字节数据
        /// </summary>
        /// <param name="key">存储键</param>
        /// <param name="data">字节数据</param>
        void SaveRaw(string key, byte[] data);

        /// <summary>
        /// 异步保存原始字节数据
        /// </summary>
        /// <param name="key">存储键</param>
        /// <param name="data">字节数据</param>
        UniTask SaveRawAsync(string key, byte[] data);

        /// <summary>
        /// 加载原始字节数据
        /// </summary>
        /// <param name="key">存储键</param>
        /// <returns>字节数据，如果不存在则返回null</returns>
        byte[] LoadRaw(string key);

        /// <summary>
        /// 异步加载原始字节数据
        /// </summary>
        /// <param name="key">存储键</param>
        /// <returns>字节数据，如果不存在则返回null</returns>
        UniTask<byte[]> LoadRawAsync(string key);
    }
}