using System;
using System.Collections.Generic;
using System.Linq;
using xFrame.Runtime.Logging;

namespace xFrame.Runtime.Persistence.Migration
{
    /// <summary>
    /// 数据迁移管理器
    /// 管理数据版本迁移器，支持链式迁移
    /// </summary>
    public class MigrationManager
    {
        private readonly Dictionary<Type, List<IDataMigrator>> _migrators = new();
        private readonly IXLogger _logger;

        /// <summary>
        /// 创建迁移管理器
        /// </summary>
        public MigrationManager()
        {
            _logger = XLog.GetLogger<MigrationManager>();
        }

        /// <summary>
        /// 注册数据迁移器
        /// </summary>
        /// <typeparam name="T">目标数据类型</typeparam>
        /// <param name="migrator">迁移器实例</param>
        public void RegisterMigrator<T>(IDataMigrator migrator)
        {
            RegisterMigrator(typeof(T), migrator);
        }

        /// <summary>
        /// 注册数据迁移器
        /// </summary>
        /// <param name="targetType">目标数据类型</param>
        /// <param name="migrator">迁移器实例</param>
        public void RegisterMigrator(Type targetType, IDataMigrator migrator)
        {
            if (migrator == null)
            {
                throw new ArgumentNullException(nameof(migrator));
            }

            if (!_migrators.TryGetValue(targetType, out var list))
            {
                list = new List<IDataMigrator>();
                _migrators[targetType] = list;
            }

            // 检查是否已存在相同版本范围的迁移器
            var existing = list.FirstOrDefault(m => m.FromVersion == migrator.FromVersion);
            if (existing != null)
            {
                _logger.Warning($"类型 {targetType.Name} 已存在从版本 {migrator.FromVersion} 的迁移器，将被覆盖");
                list.Remove(existing);
            }

            list.Add(migrator);
            list.Sort((a, b) => a.FromVersion.CompareTo(b.FromVersion));

            _logger.Debug($"注册迁移器: {targetType.Name} v{migrator.FromVersion} -> v{migrator.ToVersion}");
        }

        /// <summary>
        /// 注销数据迁移器
        /// </summary>
        /// <typeparam name="T">目标数据类型</typeparam>
        /// <param name="fromVersion">源版本号</param>
        /// <returns>是否成功注销</returns>
        public bool UnregisterMigrator<T>(int fromVersion)
        {
            return UnregisterMigrator(typeof(T), fromVersion);
        }

        /// <summary>
        /// 注销数据迁移器
        /// </summary>
        /// <param name="targetType">目标数据类型</param>
        /// <param name="fromVersion">源版本号</param>
        /// <returns>是否成功注销</returns>
        public bool UnregisterMigrator(Type targetType, int fromVersion)
        {
            if (!_migrators.TryGetValue(targetType, out var list))
            {
                return false;
            }

            var migrator = list.FirstOrDefault(m => m.FromVersion == fromVersion);
            if (migrator == null)
            {
                return false;
            }

            list.Remove(migrator);
            _logger.Debug($"注销迁移器: {targetType.Name} v{fromVersion}");
            return true;
        }

        /// <summary>
        /// 执行数据迁移
        /// </summary>
        /// <typeparam name="T">目标数据类型</typeparam>
        /// <param name="jsonData">原始JSON数据</param>
        /// <param name="fromVersion">源版本号</param>
        /// <param name="toVersion">目标版本号</param>
        /// <returns>迁移后的JSON数据</returns>
        public string Migrate<T>(string jsonData, int fromVersion, int toVersion)
        {
            return Migrate(typeof(T), jsonData, fromVersion, toVersion);
        }

        /// <summary>
        /// 执行数据迁移
        /// </summary>
        /// <param name="targetType">目标数据类型</param>
        /// <param name="jsonData">原始JSON数据</param>
        /// <param name="fromVersion">源版本号</param>
        /// <param name="toVersion">目标版本号</param>
        /// <returns>迁移后的JSON数据</returns>
        public string Migrate(Type targetType, string jsonData, int fromVersion, int toVersion)
        {
            if (fromVersion >= toVersion)
            {
                _logger.Debug($"无需迁移: {targetType.Name} v{fromVersion} >= v{toVersion}");
                return jsonData;
            }

            if (!_migrators.TryGetValue(targetType, out var list) || list.Count == 0)
            {
                _logger.Error($"类型 {targetType.Name} 没有注册迁移器，无法从 v{fromVersion} 迁移到 v{toVersion}");
                throw new InvalidOperationException(
                    $"类型 {targetType.Name} 没有注册任何迁移器，无法从 v{fromVersion} 迁移到 v{toVersion}");
            }

            var currentData = jsonData;
            var currentVersion = fromVersion;

            _logger.Info($"开始迁移 {targetType.Name}: v{fromVersion} -> v{toVersion}");

            while (currentVersion < toVersion)
            {
                var migrator = list.FirstOrDefault(m => m.FromVersion == currentVersion);
                if (migrator == null)
                {
                    _logger.Error($"找不到从版本 {currentVersion} 的迁移器，迁移中断");
                    throw new InvalidOperationException(
                        $"类型 {targetType.Name} 缺少从版本 {currentVersion} 到 {currentVersion + 1} 的迁移器");
                }

                try
                {
                    _logger.Debug($"执行迁移: v{migrator.FromVersion} -> v{migrator.ToVersion}");
                    currentData = migrator.Migrate(currentData);
                    currentVersion = migrator.ToVersion;
                }
                catch (Exception ex)
                {
                    _logger.Error($"迁移失败: v{migrator.FromVersion} -> v{migrator.ToVersion}", ex);
                    throw new InvalidOperationException(
                        $"类型 {targetType.Name} 从版本 {migrator.FromVersion} 迁移到 {migrator.ToVersion} 失败", ex);
                }
            }

            _logger.Info($"迁移完成 {targetType.Name}: v{fromVersion} -> v{toVersion}");
            return currentData;
        }

        /// <summary>
        /// 检查是否可以从指定版本迁移到目标版本
        /// </summary>
        /// <typeparam name="T">目标数据类型</typeparam>
        /// <param name="fromVersion">源版本号</param>
        /// <param name="toVersion">目标版本号</param>
        /// <returns>是否可以迁移</returns>
        public bool CanMigrate<T>(int fromVersion, int toVersion)
        {
            return CanMigrate(typeof(T), fromVersion, toVersion);
        }

        /// <summary>
        /// 检查是否可以从指定版本迁移到目标版本
        /// </summary>
        /// <param name="targetType">目标数据类型</param>
        /// <param name="fromVersion">源版本号</param>
        /// <param name="toVersion">目标版本号</param>
        /// <returns>是否可以迁移</returns>
        public bool CanMigrate(Type targetType, int fromVersion, int toVersion)
        {
            if (fromVersion >= toVersion)
            {
                return true;
            }

            if (!_migrators.TryGetValue(targetType, out var list) || list.Count == 0)
            {
                return false;
            }

            var currentVersion = fromVersion;
            while (currentVersion < toVersion)
            {
                var migrator = list.FirstOrDefault(m => m.FromVersion == currentVersion);
                if (migrator == null)
                {
                    return false;
                }

                currentVersion = migrator.ToVersion;
            }

            return true;
        }

        /// <summary>
        /// 获取指定类型的所有迁移器
        /// </summary>
        /// <typeparam name="T">目标数据类型</typeparam>
        /// <returns>迁移器列表</returns>
        public IReadOnlyList<IDataMigrator> GetMigrators<T>()
        {
            return GetMigrators(typeof(T));
        }

        /// <summary>
        /// 获取指定类型的所有迁移器
        /// </summary>
        /// <param name="targetType">目标数据类型</param>
        /// <returns>迁移器列表</returns>
        public IReadOnlyList<IDataMigrator> GetMigrators(Type targetType)
        {
            if (_migrators.TryGetValue(targetType, out var list))
            {
                return list.AsReadOnly();
            }

            return Array.Empty<IDataMigrator>();
        }

        /// <summary>
        /// 清除所有迁移器
        /// </summary>
        public void Clear()
        {
            _migrators.Clear();
            _logger.Debug("已清除所有迁移器");
        }
    }
}
