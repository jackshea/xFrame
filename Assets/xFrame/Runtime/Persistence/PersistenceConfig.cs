using System;
using xFrame.Runtime.Persistence.Security;
using xFrame.Runtime.Serialization;

namespace xFrame.Runtime.Persistence
{
    /// <summary>
    /// 持久化配置类
    /// 定义持久化模块的各种配置选项
    /// </summary>
    [Serializable]
    public class PersistenceConfig
    {
        /// <summary>
        /// 默认存储路径
        /// </summary>
        public string BasePath { get; set; }

        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string FileExtension { get; set; } = ".dat";

        /// <summary>
        /// 是否启用加密
        /// </summary>
        public bool EnableEncryption { get; set; }

        /// <summary>
        /// 加密密钥（仅当EnableEncryption为true时有效）
        /// </summary>
        public string EncryptionKey { get; set; }

        /// <summary>
        /// 加密盐值（可选）
        /// </summary>
        public string EncryptionSalt { get; set; }

        /// <summary>
        /// 是否启用数据校验
        /// </summary>
        public bool EnableValidation { get; set; } = true;

        /// <summary>
        /// 校验器类型
        /// </summary>
        public ValidatorType ValidatorType { get; set; } = ValidatorType.Sha256;

        /// <summary>
        /// 是否启用数据版本管理
        /// </summary>
        public bool EnableVersioning { get; set; } = true;

        /// <summary>
        /// 是否启用备份
        /// </summary>
        public bool EnableBackup { get; set; }

        /// <summary>
        /// 备份文件数量上限
        /// </summary>
        public int MaxBackupCount { get; set; } = 3;

        /// <summary>
        /// 是否使用异步操作
        /// </summary>
        public bool UseAsync { get; set; } = true;

        /// <summary>
        /// 是否启用内存缓存
        /// </summary>
        public bool EnableCache { get; set; }

        /// <summary>
        /// 缓存过期时间（秒）
        /// </summary>
        public int CacheExpirationSeconds { get; set; } = 300;

        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <param name="basePath">基础存储路径</param>
        /// <returns>默认配置实例</returns>
        public static PersistenceConfig CreateDefault(string basePath)
        {
            return new PersistenceConfig
            {
                BasePath = basePath,
                FileExtension = ".dat",
                EnableEncryption = false,
                EnableValidation = true,
                ValidatorType = ValidatorType.Sha256,
                EnableVersioning = true,
                EnableBackup = false,
                UseAsync = true,
                EnableCache = false
            };
        }

        /// <summary>
        /// 创建安全配置（启用加密和校验）
        /// </summary>
        /// <param name="basePath">基础存储路径</param>
        /// <param name="encryptionKey">加密密钥</param>
        /// <returns>安全配置实例</returns>
        public static PersistenceConfig CreateSecure(string basePath, string encryptionKey)
        {
            return new PersistenceConfig
            {
                BasePath = basePath,
                FileExtension = ".enc",
                EnableEncryption = true,
                EncryptionKey = encryptionKey,
                EnableValidation = true,
                ValidatorType = ValidatorType.Sha256,
                EnableVersioning = true,
                EnableBackup = true,
                MaxBackupCount = 3,
                UseAsync = true,
                EnableCache = false
            };
        }
    }

    /// <summary>
    /// 校验器类型枚举
    /// </summary>
    public enum ValidatorType
    {
        /// <summary>
        /// 不使用校验
        /// </summary>
        None,

        /// <summary>
        /// CRC32校验（快速，安全性较低）
        /// </summary>
        Crc32,

        /// <summary>
        /// SHA256校验（安全性高）
        /// </summary>
        Sha256
    }
}
