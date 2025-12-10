namespace xFrame.Runtime.Persistence.Migration
{
    /// <summary>
    /// 数据迁移器接口
    /// 定义从一个版本到另一个版本的数据迁移操作
    /// </summary>
    public interface IDataMigrator
    {
        /// <summary>
        /// 源版本号
        /// </summary>
        int FromVersion { get; }

        /// <summary>
        /// 目标版本号
        /// </summary>
        int ToVersion { get; }

        /// <summary>
        /// 执行数据迁移
        /// </summary>
        /// <param name="oldData">旧版本数据（JSON字符串）</param>
        /// <returns>迁移后的数据（JSON字符串）</returns>
        string Migrate(string oldData);
    }

    /// <summary>
    /// 泛型数据迁移器接口
    /// 支持类型安全的数据迁移
    /// </summary>
    /// <typeparam name="TFrom">源数据类型</typeparam>
    /// <typeparam name="TTo">目标数据类型</typeparam>
    public interface IDataMigrator<TFrom, TTo> : IDataMigrator
    {
        /// <summary>
        /// 执行类型安全的数据迁移
        /// </summary>
        /// <param name="oldData">旧版本数据</param>
        /// <returns>迁移后的数据</returns>
        TTo MigrateTyped(TFrom oldData);
    }
}
