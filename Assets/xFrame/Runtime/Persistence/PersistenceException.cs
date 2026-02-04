using System;

namespace xFrame.Runtime.Persistence
{
    /// <summary>
    /// 持久化异常基类
    /// </summary>
    public class PersistenceException : Exception
    {
        /// <summary>
        /// 创建持久化异常
        /// </summary>
        /// <param name="message">异常消息</param>
        public PersistenceException(string message) : base(message)
        {
        }

        /// <summary>
        /// 创建持久化异常
        /// </summary>
        /// <param name="message">异常消息</param>
        /// <param name="innerException">内部异常</param>
        public PersistenceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// 数据校验失败异常
    /// </summary>
    public class DataValidationException : PersistenceException
    {
        /// <summary>
        /// 存储键
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// 创建数据校验失败异常
        /// </summary>
        /// <param name="key">存储键</param>
        public DataValidationException(string key)
            : base($"数据校验失败: {key}")
        {
            Key = key;
        }

        /// <summary>
        /// 创建数据校验失败异常
        /// </summary>
        /// <param name="key">存储键</param>
        /// <param name="innerException">内部异常</param>
        public DataValidationException(string key, Exception innerException)
            : base($"数据校验失败: {key}", innerException)
        {
            Key = key;
        }
    }

    /// <summary>
    /// 数据迁移失败异常
    /// </summary>
    public class DataMigrationException : PersistenceException
    {
        /// <summary>
        /// 数据类型
        /// </summary>
        public Type DataType { get; }

        /// <summary>
        /// 源版本
        /// </summary>
        public int FromVersion { get; }

        /// <summary>
        /// 目标版本
        /// </summary>
        public int ToVersion { get; }

        /// <summary>
        /// 创建数据迁移失败异常
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="fromVersion">源版本</param>
        /// <param name="toVersion">目标版本</param>
        public DataMigrationException(Type dataType, int fromVersion, int toVersion)
            : base($"数据迁移失败: {dataType.Name} v{fromVersion} -> v{toVersion}")
        {
            DataType = dataType;
            FromVersion = fromVersion;
            ToVersion = toVersion;
        }

        /// <summary>
        /// 创建数据迁移失败异常
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="fromVersion">源版本</param>
        /// <param name="toVersion">目标版本</param>
        /// <param name="innerException">内部异常</param>
        public DataMigrationException(Type dataType, int fromVersion, int toVersion, Exception innerException)
            : base($"数据迁移失败: {dataType.Name} v{fromVersion} -> v{toVersion}", innerException)
        {
            DataType = dataType;
            FromVersion = fromVersion;
            ToVersion = toVersion;
        }
    }

    /// <summary>
    /// 加密解密失败异常
    /// </summary>
    public class EncryptionException : PersistenceException
    {
        /// <summary>
        /// 是否为加密操作（否则为解密）
        /// </summary>
        public bool IsEncryption { get; }

        /// <summary>
        /// 创建加密解密失败异常
        /// </summary>
        /// <param name="isEncryption">是否为加密操作</param>
        /// <param name="message">异常消息</param>
        public EncryptionException(bool isEncryption, string message)
            : base(message)
        {
            IsEncryption = isEncryption;
        }

        /// <summary>
        /// 创建加密解密失败异常
        /// </summary>
        /// <param name="isEncryption">是否为加密操作</param>
        /// <param name="message">异常消息</param>
        /// <param name="innerException">内部异常</param>
        public EncryptionException(bool isEncryption, string message, Exception innerException)
            : base(message, innerException)
        {
            IsEncryption = isEncryption;
        }
    }
}
