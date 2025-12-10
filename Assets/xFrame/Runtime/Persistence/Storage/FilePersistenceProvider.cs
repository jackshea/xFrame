using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using xFrame.Runtime.Logging;
using xFrame.Runtime.Serialization;

namespace xFrame.Runtime.Persistence.Storage
{
    /// <summary>
    /// 文件持久化提供者
    /// 将数据存储到本地文件系统
    /// </summary>
    public class FilePersistenceProvider : PersistenceProviderBase
    {
        private readonly string _basePath;
        private readonly string _fileExtension;
        private readonly IXLogger _logger;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        /// <summary>
        /// 提供者名称
        /// </summary>
        public override string Name => "File";

        /// <summary>
        /// 基础存储路径
        /// </summary>
        public string BasePath => _basePath;

        /// <summary>
        /// 创建文件持久化提供者
        /// </summary>
        /// <param name="serializer">序列化器</param>
        /// <param name="basePath">基础存储路径</param>
        /// <param name="fileExtension">文件扩展名（默认为.dat）</param>
        public FilePersistenceProvider(ISerializer serializer, string basePath, string fileExtension = ".dat")
            : base(serializer)
        {
            if (string.IsNullOrEmpty(basePath))
            {
                throw new ArgumentNullException(nameof(basePath));
            }

            _basePath = basePath;
            _fileExtension = fileExtension.StartsWith(".") ? fileExtension : "." + fileExtension;
            _logger = XLog.GetLogger<FilePersistenceProvider>();

            // 确保目录存在
            EnsureDirectoryExists();
        }

        /// <summary>
        /// 获取文件完整路径
        /// </summary>
        /// <param name="key">存储键</param>
        /// <returns>文件路径</returns>
        protected virtual string GetFilePath(string key)
        {
            // 对key进行安全处理，移除非法字符
            var safeKey = GetSafeFileName(key);
            return Path.Combine(_basePath, safeKey + _fileExtension);
        }

        /// <summary>
        /// 获取安全的文件名
        /// </summary>
        /// <param name="key">原始键</param>
        /// <returns>安全的文件名</returns>
        protected virtual string GetSafeFileName(string key)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeKey = key;
            foreach (var c in invalidChars)
            {
                safeKey = safeKey.Replace(c, '_');
            }

            return safeKey;
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        protected void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
                _logger.Debug($"创建存储目录: {_basePath}");
            }
        }

        /// <summary>
        /// 检查数据是否存在
        /// </summary>
        public override bool Exists(string key)
        {
            var filePath = GetFilePath(key);
            return File.Exists(filePath);
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        public override bool Delete(string key)
        {
            var filePath = GetFilePath(key);
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                File.Delete(filePath);
                _logger.Debug($"删除文件: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"删除文件失败: {filePath}", ex);
                return false;
            }
        }

        /// <summary>
        /// 保存原始字节数据
        /// </summary>
        public override void SaveRaw(string key, byte[] data)
        {
            var filePath = GetFilePath(key);
            EnsureDirectoryExists();

            try
            {
                _semaphore.Wait();
                try
                {
                    File.WriteAllBytes(filePath, data ?? Array.Empty<byte>());
                    _logger.Debug($"保存文件: {filePath}, 大小: {data?.Length ?? 0} bytes");
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"保存文件失败: {filePath}", ex);
                throw;
            }
        }

        /// <summary>
        /// 异步保存原始字节数据
        /// </summary>
        public override async UniTask SaveRawAsync(string key, byte[] data)
        {
            var filePath = GetFilePath(key);
            EnsureDirectoryExists();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    await File.WriteAllBytesAsync(filePath, data ?? Array.Empty<byte>());
                    _logger.Debug($"异步保存文件: {filePath}, 大小: {data?.Length ?? 0} bytes");
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"异步保存文件失败: {filePath}", ex);
                throw;
            }
        }

        /// <summary>
        /// 加载原始字节数据
        /// </summary>
        public override byte[] LoadRaw(string key)
        {
            var filePath = GetFilePath(key);
            if (!File.Exists(filePath))
            {
                _logger.Debug($"文件不存在: {filePath}");
                return null;
            }

            try
            {
                var data = File.ReadAllBytes(filePath);
                _logger.Debug($"加载文件: {filePath}, 大小: {data.Length} bytes");
                return data;
            }
            catch (Exception ex)
            {
                _logger.Error($"加载文件失败: {filePath}", ex);
                throw;
            }
        }

        /// <summary>
        /// 异步加载原始字节数据
        /// </summary>
        public override async UniTask<byte[]> LoadRawAsync(string key)
        {
            var filePath = GetFilePath(key);
            if (!File.Exists(filePath))
            {
                _logger.Debug($"文件不存在: {filePath}");
                return null;
            }

            try
            {
                var data = await File.ReadAllBytesAsync(filePath);
                _logger.Debug($"异步加载文件: {filePath}, 大小: {data.Length} bytes");
                return data;
            }
            catch (Exception ex)
            {
                _logger.Error($"异步加载文件失败: {filePath}", ex);
                throw;
            }
        }

        /// <summary>
        /// 获取所有存储的键
        /// </summary>
        /// <returns>键列表</returns>
        public string[] GetAllKeys()
        {
            if (!Directory.Exists(_basePath))
            {
                return Array.Empty<string>();
            }

            var files = Directory.GetFiles(_basePath, "*" + _fileExtension);
            var keys = new string[files.Length];
            for (var i = 0; i < files.Length; i++)
            {
                keys[i] = Path.GetFileNameWithoutExtension(files[i]);
            }

            return keys;
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void Clear()
        {
            if (!Directory.Exists(_basePath))
            {
                return;
            }

            var files = Directory.GetFiles(_basePath, "*" + _fileExtension);
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    _logger.Error($"删除文件失败: {file}", ex);
                }
            }

            _logger.Debug($"清除所有文件: {_basePath}");
        }
    }
}
