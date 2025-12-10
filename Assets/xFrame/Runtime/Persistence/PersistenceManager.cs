using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using xFrame.Runtime.Logging;
using xFrame.Runtime.Persistence.Migration;
using xFrame.Runtime.Persistence.Security;
using xFrame.Runtime.Persistence.Storage;
using xFrame.Runtime.Serialization;

namespace xFrame.Runtime.Persistence
{
    /// <summary>
    /// 持久化管理器实现
    /// 提供高层的数据持久化API，自动处理版本、加密、校验等
    /// </summary>
    public class PersistenceManager : IPersistenceManager
    {
        private readonly IPersistenceProvider _provider;
        private readonly ISerializer _serializer;
        private readonly IEncryptor _encryptor;
        private readonly IValidator _validator;
        private readonly MigrationManager _migrationManager;
        private readonly PersistenceConfig _config;
        private readonly IXLogger _logger;

        /// <summary>
        /// 持久化配置
        /// </summary>
        public PersistenceConfig Config => _config;

        /// <summary>
        /// 迁移管理器
        /// </summary>
        public MigrationManager MigrationManager => _migrationManager;

        /// <summary>
        /// 创建持久化管理器
        /// </summary>
        /// <param name="provider">持久化提供者</param>
        /// <param name="serializer">序列化器</param>
        /// <param name="config">配置</param>
        /// <param name="encryptor">加密器（可选）</param>
        /// <param name="validator">校验器（可选）</param>
        public PersistenceManager(
            IPersistenceProvider provider,
            ISerializer serializer,
            PersistenceConfig config,
            IEncryptor encryptor = null,
            IValidator validator = null)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _encryptor = encryptor ?? new NoEncryptor();
            _validator = validator ?? CreateValidator(config.ValidatorType);
            _migrationManager = new MigrationManager();
            _logger = XLog.GetLogger<PersistenceManager>();

            _logger.Info($"持久化管理器初始化完成 - Provider: {_provider.Name}, Encryptor: {_encryptor.Name}, Validator: {_validator.Name}");
        }

        /// <summary>
        /// 根据配置创建校验器
        /// </summary>
        private static IValidator CreateValidator(ValidatorType type)
        {
            return type switch
            {
                ValidatorType.None => new NoValidator(),
                ValidatorType.Crc32 => new Crc32Validator(),
                ValidatorType.Sha256 => new Sha256Validator(),
                _ => new Sha256Validator()
            };
        }

        /// <summary>
        /// 获取类型的默认存储键
        /// </summary>
        public string GetDefaultKey<T>()
        {
            return GetDefaultKey(typeof(T));
        }

        /// <summary>
        /// 获取类型的默认存储键
        /// </summary>
        public string GetDefaultKey(Type type)
        {
            return type.FullName ?? type.Name;
        }

        /// <summary>
        /// 保存数据（使用类型名作为键）
        /// </summary>
        public void Save<T>(T data)
        {
            Save(GetDefaultKey<T>(), data);
        }

        /// <summary>
        /// 保存数据（指定键）
        /// </summary>
        public void Save<T>(string key, T data)
        {
            try
            {
                var wrapper = CreateWrapper<T>(data);
                var wrapperJson = JsonUtility.ToJson(wrapper);
                var wrapperBytes = Encoding.UTF8.GetBytes(wrapperJson);

                // 加密
                var encryptedBytes = _encryptor.Encrypt(wrapperBytes);

                _provider.SaveRaw(key, encryptedBytes);
                _logger.Debug($"保存数据成功: {key}");
            }
            catch (Exception ex)
            {
                _logger.Error($"保存数据失败: {key}", ex);
                throw;
            }
        }

        /// <summary>
        /// 异步保存数据（使用类型名作为键）
        /// </summary>
        public UniTask SaveAsync<T>(T data)
        {
            return SaveAsync(GetDefaultKey<T>(), data);
        }

        /// <summary>
        /// 异步保存数据（指定键）
        /// </summary>
        public async UniTask SaveAsync<T>(string key, T data)
        {
            try
            {
                var wrapper = CreateWrapper<T>(data);
                var wrapperJson = JsonUtility.ToJson(wrapper);
                var wrapperBytes = Encoding.UTF8.GetBytes(wrapperJson);

                // 加密
                var encryptedBytes = _encryptor.Encrypt(wrapperBytes);

                await _provider.SaveRawAsync(key, encryptedBytes);
                _logger.Debug($"异步保存数据成功: {key}");
            }
            catch (Exception ex)
            {
                _logger.Error($"异步保存数据失败: {key}", ex);
                throw;
            }
        }

        /// <summary>
        /// 加载数据（使用类型名作为键）
        /// </summary>
        public T Load<T>()
        {
            return Load<T>(GetDefaultKey<T>());
        }

        /// <summary>
        /// 加载数据（指定键）
        /// </summary>
        public T Load<T>(string key)
        {
            try
            {
                var encryptedBytes = _provider.LoadRaw(key);
                if (encryptedBytes == null || encryptedBytes.Length == 0)
                {
                    _logger.Debug($"数据不存在: {key}");
                    return default;
                }

                return ProcessLoadedData<T>(key, encryptedBytes);
            }
            catch (Exception ex)
            {
                _logger.Error($"加载数据失败: {key}", ex);
                return default;
            }
        }

        /// <summary>
        /// 异步加载数据（使用类型名作为键）
        /// </summary>
        public UniTask<T> LoadAsync<T>()
        {
            return LoadAsync<T>(GetDefaultKey<T>());
        }

        /// <summary>
        /// 异步加载数据（指定键）
        /// </summary>
        public async UniTask<T> LoadAsync<T>(string key)
        {
            try
            {
                var encryptedBytes = await _provider.LoadRawAsync(key);
                if (encryptedBytes == null || encryptedBytes.Length == 0)
                {
                    _logger.Debug($"数据不存在: {key}");
                    return default;
                }

                return ProcessLoadedData<T>(key, encryptedBytes);
            }
            catch (Exception ex)
            {
                _logger.Error($"异步加载数据失败: {key}", ex);
                return default;
            }
        }

        /// <summary>
        /// 处理加载的数据（解密、校验、迁移）
        /// </summary>
        private T ProcessLoadedData<T>(string key, byte[] encryptedBytes)
        {
            // 解密
            var wrapperBytes = _encryptor.Decrypt(encryptedBytes);
            var wrapperJson = Encoding.UTF8.GetString(wrapperBytes);
            var wrapper = JsonUtility.FromJson<DataWrapper>(wrapperJson);

            // 获取原始数据
            var payload = wrapper.GetPayload();
            if (payload == null)
            {
                _logger.Warning($"数据载荷为空: {key}");
                return default;
            }

            // 校验数据
            if (_config.EnableValidation)
            {
                var storedHash = wrapper.GetHash();
                if (storedHash != null && storedHash.Length > 0)
                {
                    if (!_validator.VerifyHash(payload, storedHash))
                    {
                        _logger.Error($"数据校验失败: {key}");
                        throw new InvalidOperationException($"数据校验失败: {key}");
                    }
                }
            }

            // 获取数据JSON
            var dataJson = Encoding.UTF8.GetString(payload);

            // 版本迁移
            if (_config.EnableVersioning)
            {
                var currentVersion = GetCurrentVersion<T>();
                if (wrapper.dataVersion < currentVersion)
                {
                    _logger.Info($"执行数据迁移: {key} v{wrapper.dataVersion} -> v{currentVersion}");
                    dataJson = _migrationManager.Migrate<T>(dataJson, wrapper.dataVersion, currentVersion);

                    // 迁移后重新保存
                    var migratedData = JsonUtility.FromJson<T>(dataJson);
                    Save(key, migratedData);
                    return migratedData;
                }
            }

            return JsonUtility.FromJson<T>(dataJson);
        }

        /// <summary>
        /// 加载数据，如果不存在则使用默认值
        /// </summary>
        public T LoadOrDefault<T>(T defaultValue)
        {
            return LoadOrDefault(GetDefaultKey<T>(), defaultValue);
        }

        /// <summary>
        /// 加载数据，如果不存在则使用默认值
        /// </summary>
        public T LoadOrDefault<T>(string key, T defaultValue)
        {
            if (!Exists(key))
            {
                return defaultValue;
            }

            var result = Load<T>(key);
            return result == null ? defaultValue : result;
        }

        /// <summary>
        /// 异步加载数据，如果不存在则使用默认值
        /// </summary>
        public UniTask<T> LoadOrDefaultAsync<T>(T defaultValue)
        {
            return LoadOrDefaultAsync(GetDefaultKey<T>(), defaultValue);
        }

        /// <summary>
        /// 异步加载数据，如果不存在则使用默认值
        /// </summary>
        public async UniTask<T> LoadOrDefaultAsync<T>(string key, T defaultValue)
        {
            if (!Exists(key))
            {
                return defaultValue;
            }

            var result = await LoadAsync<T>(key);
            return result == null ? defaultValue : result;
        }

        /// <summary>
        /// 检查数据是否存在（使用类型名作为键）
        /// </summary>
        public bool Exists<T>()
        {
            return Exists(GetDefaultKey<T>());
        }

        /// <summary>
        /// 检查数据是否存在（指定键）
        /// </summary>
        public bool Exists(string key)
        {
            return _provider.Exists(key);
        }

        /// <summary>
        /// 删除数据（使用类型名作为键）
        /// </summary>
        public bool Delete<T>()
        {
            return Delete(GetDefaultKey<T>());
        }

        /// <summary>
        /// 删除数据（指定键）
        /// </summary>
        public bool Delete(string key)
        {
            var result = _provider.Delete(key);
            if (result)
            {
                _logger.Debug($"删除数据成功: {key}");
            }

            return result;
        }

        /// <summary>
        /// 注册数据迁移器
        /// </summary>
        public void RegisterMigrator<T>(IDataMigrator migrator)
        {
            _migrationManager.RegisterMigrator<T>(migrator);
        }

        /// <summary>
        /// 创建数据包装器
        /// </summary>
        private DataWrapper CreateWrapper<T>(T data)
        {
            var dataJson = JsonUtility.ToJson(data);
            var payload = Encoding.UTF8.GetBytes(dataJson);

            // 计算哈希
            byte[] hash = null;
            if (_config.EnableValidation)
            {
                hash = _validator.ComputeHash(payload);
            }

            var version = GetCurrentVersion<T>();
            return new DataWrapper(version, typeof(T).FullName, payload, hash);
        }

        /// <summary>
        /// 获取类型的当前版本号
        /// </summary>
        private int GetCurrentVersion<T>()
        {
            if (typeof(IVersionedData).IsAssignableFrom(typeof(T)))
            {
                // 创建临时实例获取版本号
                try
                {
                    var instance = Activator.CreateInstance<T>();
                    if (instance is IVersionedData versionedData)
                    {
                        return versionedData.CurrentVersion;
                    }
                }
                catch
                {
                    // 如果无法创建实例，返回默认版本
                }
            }

            return 1;
        }
    }
}
