using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        
        // 内存缓存
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        
        // 版本号缓存，避免重复创建实例
        private readonly ConcurrentDictionary<Type, int> _versionCache = new();
        
        /// <summary>
        /// 缓存条目
        /// </summary>
        private class CacheEntry
        {
            public object Data { get; set; }
            public DateTime ExpireTime { get; set; }
            public bool IsExpired => DateTime.UtcNow > ExpireTime;
        }

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
                // 备份原有数据
                if (_config.EnableBackup && _provider.Exists(key))
                {
                    CreateBackup(key);
                }
                
                var wrapper = CreateWrapper<T>(data);
                var wrapperJson = JsonUtility.ToJson(wrapper);
                var wrapperBytes = Encoding.UTF8.GetBytes(wrapperJson);

                // 加密
                var encryptedBytes = _encryptor.Encrypt(wrapperBytes);

                _provider.SaveRaw(key, encryptedBytes);
                
                // 更新缓存
                UpdateCache(key, data);
                
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
                // 备份原有数据
                if (_config.EnableBackup && _provider.Exists(key))
                {
                    await CreateBackupAsync(key);
                }
                
                var wrapper = CreateWrapper<T>(data);
                var wrapperJson = JsonUtility.ToJson(wrapper);
                var wrapperBytes = Encoding.UTF8.GetBytes(wrapperJson);

                // 加密
                var encryptedBytes = _encryptor.Encrypt(wrapperBytes);

                await _provider.SaveRawAsync(key, encryptedBytes);
                
                // 更新缓存
                UpdateCache(key, data);
                
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
                // 尝试从缓存获取
                if (TryGetFromCache<T>(key, out var cachedData))
                {
                    _logger.Debug($"从缓存加载数据: {key}");
                    return cachedData;
                }
                
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
                // 尝试从缓存获取
                if (TryGetFromCache<T>(key, out var cachedData))
                {
                    _logger.Debug($"从缓存加载数据: {key}");
                    return cachedData;
                }
                
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
                        throw new DataValidationException(key);
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
                    try
                    {
                        dataJson = _migrationManager.Migrate<T>(dataJson, wrapper.dataVersion, currentVersion);
                    }
                    catch (Exception ex)
                    {
                        throw new DataMigrationException(typeof(T), wrapper.dataVersion, currentVersion, ex);
                    }

                    // 迁移后重新保存
                    var migratedData = JsonUtility.FromJson<T>(dataJson);
                    Save(key, migratedData);
                    
                    // 更新缓存
                    UpdateCache(key, migratedData);
                    return migratedData;
                }
            }

            var result = JsonUtility.FromJson<T>(dataJson);
            
            // 更新缓存
            UpdateCache(key, result);
            
            return result;
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
            if (!await ExistsAsync(key))
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
        /// 获取类型的当前版本号（使用缓存避免重复创建实例）
        /// </summary>
        private int GetCurrentVersion<T>()
        {
            var type = typeof(T);
            
            // 尝试从缓存获取
            if (_versionCache.TryGetValue(type, out var cachedVersion))
            {
                return cachedVersion;
            }
            
            var version = 1;
            if (typeof(IVersionedData).IsAssignableFrom(type))
            {
                try
                {
                    var instance = Activator.CreateInstance<T>();
                    if (instance is IVersionedData versionedData)
                    {
                        version = versionedData.CurrentVersion;
                    }
                }
                catch
                {
                    // 如果无法创建实例，返回默认版本
                }
            }
            
            // 缓存版本号
            _versionCache.TryAdd(type, version);
            return version;
        }
        
        /// <summary>
        /// 更新缓存
        /// </summary>
        private void UpdateCache<T>(string key, T data)
        {
            if (!_config.EnableCache) return;
            
            var entry = new CacheEntry
            {
                Data = data,
                ExpireTime = DateTime.UtcNow.AddSeconds(_config.CacheExpirationSeconds)
            };
            _cache[key] = entry;
        }
        
        /// <summary>
        /// 尝试从缓存获取数据
        /// </summary>
        private bool TryGetFromCache<T>(string key, out T data)
        {
            data = default;
            if (!_config.EnableCache) return false;
            
            if (_cache.TryGetValue(key, out var entry))
            {
                if (!entry.IsExpired && entry.Data is T typedData)
                {
                    data = typedData;
                    return true;
                }
                
                // 移除过期缓存
                _cache.TryRemove(key, out _);
            }
            
            return false;
        }
        
        /// <summary>
        /// 使缓存失效
        /// </summary>
        private void InvalidateCache(string key)
        {
            if (_config.EnableCache)
            {
                _cache.TryRemove(key, out _);
            }
        }
        
        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            _logger.Debug("已清除所有缓存");
        }
        
        /// <summary>
        /// 批量保存数据
        /// </summary>
        /// <param name="items">键值对列表</param>
        public void SaveBatch<T>(IEnumerable<KeyValuePair<string, T>> items)
        {
            foreach (var item in items)
            {
                Save(item.Key, item.Value);
            }
        }
        
        /// <summary>
        /// 异步批量保存数据
        /// </summary>
        /// <param name="items">键值对列表</param>
        public async UniTask SaveBatchAsync<T>(IEnumerable<KeyValuePair<string, T>> items)
        {
            foreach (var item in items)
            {
                await SaveAsync(item.Key, item.Value);
            }
        }
        
        /// <summary>
        /// 批量加载数据
        /// </summary>
        /// <param name="keys">键列表</param>
        /// <returns>键值对字典</returns>
        public Dictionary<string, T> LoadBatch<T>(IEnumerable<string> keys)
        {
            var result = new Dictionary<string, T>();
            foreach (var key in keys)
            {
                var data = Load<T>(key);
                if (data != null)
                {
                    result[key] = data;
                }
            }
            return result;
        }
        
        /// <summary>
        /// 异步批量加载数据
        /// </summary>
        /// <param name="keys">键列表</param>
        /// <returns>键值对字典</returns>
        public async UniTask<Dictionary<string, T>> LoadBatchAsync<T>(IEnumerable<string> keys)
        {
            var result = new Dictionary<string, T>();
            foreach (var key in keys)
            {
                var data = await LoadAsync<T>(key);
                if (data != null)
                {
                    result[key] = data;
                }
            }
            return result;
        }
        
        /// <summary>
        /// 异步检查数据是否存在（使用类型名作为键）
        /// </summary>
        public UniTask<bool> ExistsAsync<T>()
        {
            return ExistsAsync(GetDefaultKey<T>());
        }
        
        /// <summary>
        /// 异步检查数据是否存在（指定键）
        /// </summary>
        public UniTask<bool> ExistsAsync(string key)
        {
            return _provider.ExistsAsync(key);
        }
        
        /// <summary>
        /// 异步删除数据（使用类型名作为键）
        /// </summary>
        public UniTask<bool> DeleteAsync<T>()
        {
            return DeleteAsync(GetDefaultKey<T>());
        }
        
        /// <summary>
        /// 异步删除数据（指定键）
        /// </summary>
        public async UniTask<bool> DeleteAsync(string key)
        {
            InvalidateCache(key);
            var result = await _provider.DeleteAsync(key);
            if (result)
            {
                _logger.Debug($"异步删除数据成功: {key}");
            }
            return result;
        }
        
        #region 备份功能
        
        /// <summary>
        /// 获取备份键名
        /// </summary>
        private string GetBackupKey(string key, int backupIndex)
        {
            return $"{key}.backup.{backupIndex}";
        }
        
        /// <summary>
        /// 创建数据备份
        /// </summary>
        private void CreateBackup(string key)
        {
            try
            {
                var currentData = _provider.LoadRaw(key);
                if (currentData == null || currentData.Length == 0) return;
                
                // 滚动备份：将旧备份向后移动
                for (var i = _config.MaxBackupCount - 1; i > 0; i--)
                {
                    var fromKey = GetBackupKey(key, i - 1);
                    var toKey = GetBackupKey(key, i);
                    
                    if (_provider.Exists(fromKey))
                    {
                        var backupData = _provider.LoadRaw(fromKey);
                        _provider.SaveRaw(toKey, backupData);
                    }
                }
                
                // 保存当前数据为第一个备份
                _provider.SaveRaw(GetBackupKey(key, 0), currentData);
                _logger.Debug($"创建备份成功: {key}");
            }
            catch (Exception ex)
            {
                _logger.Warning($"创建备份失败: {key}, 错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 异步创建数据备份
        /// </summary>
        private async UniTask CreateBackupAsync(string key)
        {
            try
            {
                var currentData = await _provider.LoadRawAsync(key);
                if (currentData == null || currentData.Length == 0) return;
                
                // 滚动备份：将旧备份向后移动
                for (var i = _config.MaxBackupCount - 1; i > 0; i--)
                {
                    var fromKey = GetBackupKey(key, i - 1);
                    var toKey = GetBackupKey(key, i);
                    
                    if (_provider.Exists(fromKey))
                    {
                        var backupData = await _provider.LoadRawAsync(fromKey);
                        await _provider.SaveRawAsync(toKey, backupData);
                    }
                }
                
                // 保存当前数据为第一个备份
                await _provider.SaveRawAsync(GetBackupKey(key, 0), currentData);
                _logger.Debug($"异步创建备份成功: {key}");
            }
            catch (Exception ex)
            {
                _logger.Warning($"异步创建备份失败: {key}, 错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 从备份恢复数据
        /// </summary>
        /// <param name="key">存储键</param>
        /// <param name="backupIndex">备份索引（0为最新备份）</param>
        /// <returns>是否成功恢复</returns>
        public bool RestoreFromBackup(string key, int backupIndex = 0)
        {
            try
            {
                var backupKey = GetBackupKey(key, backupIndex);
                if (!_provider.Exists(backupKey))
                {
                    _logger.Warning($"备份不存在: {backupKey}");
                    return false;
                }
                
                var backupData = _provider.LoadRaw(backupKey);
                _provider.SaveRaw(key, backupData);
                InvalidateCache(key);
                
                _logger.Info($"从备份恢复成功: {key} <- {backupKey}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"从备份恢复失败: {key}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 异步从备份恢复数据
        /// </summary>
        /// <param name="key">存储键</param>
        /// <param name="backupIndex">备份索引（0为最新备份）</param>
        /// <returns>是否成功恢复</returns>
        public async UniTask<bool> RestoreFromBackupAsync(string key, int backupIndex = 0)
        {
            try
            {
                var backupKey = GetBackupKey(key, backupIndex);
                if (!await _provider.ExistsAsync(backupKey))
                {
                    _logger.Warning($"备份不存在: {backupKey}");
                    return false;
                }
                
                var backupData = await _provider.LoadRawAsync(backupKey);
                await _provider.SaveRawAsync(key, backupData);
                InvalidateCache(key);
                
                _logger.Info($"异步从备份恢复成功: {key} <- {backupKey}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"异步从备份恢复失败: {key}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 获取可用的备份数量
        /// </summary>
        /// <param name="key">存储键</param>
        /// <returns>备份数量</returns>
        public int GetBackupCount(string key)
        {
            var count = 0;
            for (var i = 0; i < _config.MaxBackupCount; i++)
            {
                if (_provider.Exists(GetBackupKey(key, i)))
                {
                    count++;
                }
                else
                {
                    break;
                }
            }
            return count;
        }
        
        /// <summary>
        /// 删除所有备份
        /// </summary>
        /// <param name="key">存储键</param>
        public void DeleteAllBackups(string key)
        {
            for (var i = 0; i < _config.MaxBackupCount; i++)
            {
                var backupKey = GetBackupKey(key, i);
                if (_provider.Exists(backupKey))
                {
                    _provider.Delete(backupKey);
                }
            }
            _logger.Debug($"已删除所有备份: {key}");
        }
        
        #endregion
    }
}
