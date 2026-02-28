namespace xFrame.Runtime.Platform
{
    /// <summary>
    /// 平台服务接口。
    /// 统一封装与运行平台相关的查询能力。
    /// </summary>
    public interface IPlatformService
    {
        /// <summary>
        /// 持久化数据目录。
        /// </summary>
        string PersistentDataPath { get; }

        /// <summary>
        /// StreamingAssets 目录。
        /// </summary>
        string StreamingAssetsPath { get; }

        /// <summary>
        /// 当前是否在编辑器环境。
        /// </summary>
        bool IsEditor { get; }

        /// <summary>
        /// 获取平台信息快照。
        /// </summary>
        PlatformInfo GetPlatformInfo();
    }
}
