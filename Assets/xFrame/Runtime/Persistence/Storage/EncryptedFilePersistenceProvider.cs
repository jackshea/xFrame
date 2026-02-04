using System;
using Cysharp.Threading.Tasks;
using xFrame.Runtime.Logging;
using xFrame.Runtime.Persistence.Security;
using xFrame.Runtime.Serialization;

namespace xFrame.Runtime.Persistence.Storage
{
    /// <summary>
    /// 加密文件持久化提供者
    /// 在文件存储的基础上增加加密功能
    /// </summary>
    public class EncryptedFilePersistenceProvider : FilePersistenceProvider
    {
        private readonly IEncryptor _encryptor;
        private readonly IXLogger _logger;

        /// <summary>
        /// 提供者名称
        /// </summary>
        public override string Name => "EncryptedFile";

        /// <summary>
        /// 创建加密文件持久化提供者
        /// </summary>
        /// <param name="serializer">序列化器</param>
        /// <param name="basePath">基础存储路径</param>
        /// <param name="encryptor">加密器</param>
        /// <param name="fileExtension">文件扩展名（默认为.enc）</param>
        public EncryptedFilePersistenceProvider(
            ISerializer serializer,
            string basePath,
            IEncryptor encryptor,
            string fileExtension = ".enc")
            : base(serializer, basePath, fileExtension)
        {
            _encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
            _logger = XLog.GetLogger<EncryptedFilePersistenceProvider>();
        }

        /// <summary>
        /// 保存原始字节数据（加密后存储）
        /// </summary>
        public override void SaveRaw(string key, byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                base.SaveRaw(key, data);
                return;
            }

            try
            {
                var encryptedData = _encryptor.Encrypt(data);
                _logger.Debug($"加密数据: {data.Length} bytes -> {encryptedData.Length} bytes");
                base.SaveRaw(key, encryptedData);
            }
            catch (Exception ex)
            {
                _logger.Error($"加密数据失败: {key}", ex);
                throw;
            }
        }

        /// <summary>
        /// 异步保存原始字节数据（加密后存储）
        /// </summary>
        public override async UniTask SaveRawAsync(string key, byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                await base.SaveRawAsync(key, data);
                return;
            }

            try
            {
                var encryptedData = _encryptor.Encrypt(data);
                _logger.Debug($"加密数据: {data.Length} bytes -> {encryptedData.Length} bytes");
                await base.SaveRawAsync(key, encryptedData);
            }
            catch (Exception ex)
            {
                _logger.Error($"加密数据失败: {key}", ex);
                throw;
            }
        }

        /// <summary>
        /// 加载原始字节数据（解密后返回）
        /// </summary>
        public override byte[] LoadRaw(string key)
        {
            var encryptedData = base.LoadRaw(key);
            if (encryptedData == null || encryptedData.Length == 0)
            {
                return encryptedData;
            }

            try
            {
                var decryptedData = _encryptor.Decrypt(encryptedData);
                _logger.Debug($"解密数据: {encryptedData.Length} bytes -> {decryptedData.Length} bytes");
                return decryptedData;
            }
            catch (Exception ex)
            {
                _logger.Error($"解密数据失败: {key}", ex);
                throw;
            }
        }

        /// <summary>
        /// 异步加载原始字节数据（解密后返回）
        /// </summary>
        public override async UniTask<byte[]> LoadRawAsync(string key)
        {
            var encryptedData = await base.LoadRawAsync(key);
            if (encryptedData == null || encryptedData.Length == 0)
            {
                return encryptedData;
            }

            try
            {
                var decryptedData = _encryptor.Decrypt(encryptedData);
                _logger.Debug($"解密数据: {encryptedData.Length} bytes -> {decryptedData.Length} bytes");
                return decryptedData;
            }
            catch (Exception ex)
            {
                _logger.Error($"解密数据失败: {key}", ex);
                throw;
            }
        }
    }
}
