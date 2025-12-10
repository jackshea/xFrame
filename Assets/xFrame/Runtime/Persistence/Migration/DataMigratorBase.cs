using UnityEngine;

namespace xFrame.Runtime.Persistence.Migration
{
    /// <summary>
    /// 数据迁移器基类
    /// 提供JSON字符串级别的迁移支持
    /// </summary>
    public abstract class DataMigratorBase : IDataMigrator
    {
        /// <summary>
        /// 源版本号
        /// </summary>
        public abstract int FromVersion { get; }

        /// <summary>
        /// 目标版本号
        /// </summary>
        public abstract int ToVersion { get; }

        /// <summary>
        /// 执行数据迁移
        /// </summary>
        /// <param name="oldData">旧版本数据（JSON字符串）</param>
        /// <returns>迁移后的数据（JSON字符串）</returns>
        public abstract string Migrate(string oldData);
    }

    /// <summary>
    /// 泛型数据迁移器基类
    /// 提供类型安全的迁移支持
    /// </summary>
    /// <typeparam name="TFrom">源数据类型</typeparam>
    /// <typeparam name="TTo">目标数据类型</typeparam>
    public abstract class DataMigratorBase<TFrom, TTo> : IDataMigrator<TFrom, TTo>
    {
        /// <summary>
        /// 源版本号
        /// </summary>
        public abstract int FromVersion { get; }

        /// <summary>
        /// 目标版本号
        /// </summary>
        public abstract int ToVersion { get; }

        /// <summary>
        /// 执行数据迁移（JSON字符串级别）
        /// </summary>
        /// <param name="oldData">旧版本数据（JSON字符串）</param>
        /// <returns>迁移后的数据（JSON字符串）</returns>
        public string Migrate(string oldData)
        {
            var fromObj = JsonUtility.FromJson<TFrom>(oldData);
            var toObj = MigrateTyped(fromObj);
            return JsonUtility.ToJson(toObj);
        }

        /// <summary>
        /// 执行类型安全的数据迁移
        /// </summary>
        /// <param name="oldData">旧版本数据</param>
        /// <returns>迁移后的数据</returns>
        public abstract TTo MigrateTyped(TFrom oldData);
    }
}
