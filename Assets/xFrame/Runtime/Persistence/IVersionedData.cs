namespace xFrame.Runtime.Persistence
{
    /// <summary>
    /// 版本化数据接口
    /// 实现此接口的数据类型支持版本迁移
    /// </summary>
    public interface IVersionedData
    {
        /// <summary>
        /// 当前数据版本号
        /// 每次数据结构变更时应递增此值
        /// </summary>
        int CurrentVersion { get; }
    }
}
