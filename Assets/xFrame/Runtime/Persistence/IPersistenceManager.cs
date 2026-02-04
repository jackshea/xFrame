using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using xFrame.Runtime.Persistence.Migration;

namespace xFrame.Runtime.Persistence
{
    /// <summary>
    /// 持久化管理器接口
    /// 提供高层的数据持久化API
    /// </summary>
    public interface IPersistenceManager
    {
        /// <summary>
        /// 持久化配置
        /// </summary>
        PersistenceConfig Config { get; }

        /// <summary>
        /// 迁移管理器
        /// </summary>
        MigrationManager MigrationManager { get; }

        /// <summary>
        /// 保存数据（使用类型名作为键）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">要保存的数据</param>
        void Save<T>(T data);

        /// <summary>
        /// 保存数据（指定键）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">存储键</param>
        /// <param name="data">要保存的数据</param>
        void Save<T>(string key, T data);

        /// <summary>
        /// 异步保存数据（使用类型名作为键）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">要保存的数据</param>
        UniTask SaveAsync<T>(T data);

        /// <summary>
        /// 异步保存数据（指定键）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">存储键</param>
        /// <param name="data">要保存的数据</param>
        UniTask SaveAsync<T>(string key, T data);

        /// <summary>
        /// 加载数据（使用类型名作为键）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <returns>加载的数据，如果不存在则返回默认值</returns>
        T Load<T>();

        /// <summary>
        /// 加载数据（指定键）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">存储键</param>
        /// <returns>加载的数据，如果不存在则返回默认值</returns>
        T Load<T>(string key);

        /// <summary>
        /// 异步加载数据（使用类型名作为键）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <returns>加载的数据，如果不存在则返回默认值</returns>
        UniTask<T> LoadAsync<T>();

        /// <summary>
        /// 异步加载数据（指定键）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">存储键</param>
        /// <returns>加载的数据，如果不存在则返回默认值</returns>
        UniTask<T> LoadAsync<T>(string key);

        /// <summary>
        /// 加载数据，如果不存在则使用默认值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="defaultValue">默认值</param>
        /// <returns>加载的数据或默认值</returns>
        T LoadOrDefault<T>(T defaultValue);

        /// <summary>
        /// 加载数据，如果不存在则使用默认值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">存储键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>加载的数据或默认值</returns>
        T LoadOrDefault<T>(string key, T defaultValue);

        /// <summary>
        /// 异步加载数据，如果不存在则使用默认值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="defaultValue">默认值</param>
        /// <returns>加载的数据或默认值</returns>
        UniTask<T> LoadOrDefaultAsync<T>(T defaultValue);

        /// <summary>
        /// 异步加载数据，如果不存在则使用默认值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">存储键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>加载的数据或默认值</returns>
        UniTask<T> LoadOrDefaultAsync<T>(string key, T defaultValue);

        /// <summary>
        /// 检查数据是否存在（使用类型名作为键）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <returns>是否存在</returns>
        bool Exists<T>();

        /// <summary>
        /// 检查数据是否存在（指定键）
        /// </summary>
        /// <param name="key">存储键</param>
        /// <returns>是否存在</returns>
        bool Exists(string key);

        /// <summary>
        /// 删除数据（使用类型名作为键）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <returns>是否成功删除</returns>
        bool Delete<T>();

        /// <summary>
        /// 删除数据（指定键）
        /// </summary>
        /// <param name="key">存储键</param>
        /// <returns>是否成功删除</returns>
        bool Delete(string key);

        /// <summary>
        /// 注册数据迁移器
        /// </summary>
        /// <typeparam name="T">目标数据类型</typeparam>
        /// <param name="migrator">迁移器实例</param>
        void RegisterMigrator<T>(IDataMigrator migrator);

        /// <summary>
        /// 获取类型的默认存储键
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <returns>存储键</returns>
        string GetDefaultKey<T>();

        /// <summary>
        /// 获取类型的默认存储键
        /// </summary>
        /// <param name="type">数据类型</param>
        /// <returns>存储键</returns>
        string GetDefaultKey(Type type);
        
        /// <summary>
        /// 异步检查数据是否存在（使用类型名作为键）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <returns>是否存在</returns>
        UniTask<bool> ExistsAsync<T>();
        
        /// <summary>
        /// 异步检查数据是否存在（指定键）
        /// </summary>
        /// <param name="key">存储键</param>
        /// <returns>是否存在</returns>
        UniTask<bool> ExistsAsync(string key);
        
        /// <summary>
        /// 异步删除数据（使用类型名作为键）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <returns>是否成功删除</returns>
        UniTask<bool> DeleteAsync<T>();
        
        /// <summary>
        /// 异步删除数据（指定键）
        /// </summary>
        /// <param name="key">存储键</param>
        /// <returns>是否成功删除</returns>
        UniTask<bool> DeleteAsync(string key);
        
        /// <summary>
        /// 清除所有缓存
        /// </summary>
        void ClearCache();
        
        /// <summary>
        /// 批量保存数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="items">键值对列表</param>
        void SaveBatch<T>(IEnumerable<KeyValuePair<string, T>> items);
        
        /// <summary>
        /// 异步批量保存数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="items">键值对列表</param>
        UniTask SaveBatchAsync<T>(IEnumerable<KeyValuePair<string, T>> items);
        
        /// <summary>
        /// 批量加载数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="keys">键列表</param>
        /// <returns>键值对字典</returns>
        Dictionary<string, T> LoadBatch<T>(IEnumerable<string> keys);
        
        /// <summary>
        /// 异步批量加载数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="keys">键列表</param>
        /// <returns>键值对字典</returns>
        UniTask<Dictionary<string, T>> LoadBatchAsync<T>(IEnumerable<string> keys);
        
        /// <summary>
        /// 从备份恢复数据
        /// </summary>
        /// <param name="key">存储键</param>
        /// <param name="backupIndex">备份索引（0为最新备份）</param>
        /// <returns>是否成功恢复</returns>
        bool RestoreFromBackup(string key, int backupIndex = 0);
        
        /// <summary>
        /// 异步从备份恢复数据
        /// </summary>
        /// <param name="key">存储键</param>
        /// <param name="backupIndex">备份索引（0为最新备份）</param>
        /// <returns>是否成功恢复</returns>
        UniTask<bool> RestoreFromBackupAsync(string key, int backupIndex = 0);
        
        /// <summary>
        /// 获取可用的备份数量
        /// </summary>
        /// <param name="key">存储键</param>
        /// <returns>备份数量</returns>
        int GetBackupCount(string key);
        
        /// <summary>
        /// 删除所有备份
        /// </summary>
        /// <param name="key">存储键</param>
        void DeleteAllBackups(string key);
    }
}
