using System;

namespace xFrame.Runtime.Persistence
{
    /// <summary>
    /// 数据包装结构
    /// 用于存储数据本体及其元信息（版本、哈希等）
    /// </summary>
    [Serializable]
    public class DataWrapper
    {
        /// <summary>
        /// 数据版本号
        /// </summary>
        public int dataVersion;

        /// <summary>
        /// 数据类型全名
        /// </summary>
        public string typeName;

        /// <summary>
        /// 原始数据（Base64编码的字节数组）
        /// </summary>
        public string rawPayload;

        /// <summary>
        /// 数据哈希值（Base64编码）
        /// </summary>
        public string hash;

        /// <summary>
        /// 创建时间戳（UTC）
        /// </summary>
        public long timestamp;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public DataWrapper()
        {
        }

        /// <summary>
        /// 创建数据包装器
        /// </summary>
        /// <param name="version">数据版本</param>
        /// <param name="typeName">数据类型名称</param>
        /// <param name="payload">原始数据</param>
        /// <param name="hash">数据哈希</param>
        public DataWrapper(int version, string typeName, byte[] payload, byte[] hash = null)
        {
            dataVersion = version;
            this.typeName = typeName;
            rawPayload = payload != null ? Convert.ToBase64String(payload) : null;
            this.hash = hash != null ? Convert.ToBase64String(hash) : null;
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 获取原始数据字节数组
        /// </summary>
        /// <returns>原始数据</returns>
        public byte[] GetPayload()
        {
            return string.IsNullOrEmpty(rawPayload) ? null : Convert.FromBase64String(rawPayload);
        }

        /// <summary>
        /// 设置原始数据
        /// </summary>
        /// <param name="payload">原始数据</param>
        public void SetPayload(byte[] payload)
        {
            rawPayload = payload != null ? Convert.ToBase64String(payload) : null;
        }

        /// <summary>
        /// 获取哈希值字节数组
        /// </summary>
        /// <returns>哈希值</returns>
        public byte[] GetHash()
        {
            return string.IsNullOrEmpty(hash) ? null : Convert.FromBase64String(hash);
        }

        /// <summary>
        /// 设置哈希值
        /// </summary>
        /// <param name="hashBytes">哈希值</param>
        public void SetHash(byte[] hashBytes)
        {
            hash = hashBytes != null ? Convert.ToBase64String(hashBytes) : null;
        }
    }
}
