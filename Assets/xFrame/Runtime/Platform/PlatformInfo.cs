namespace xFrame.Runtime.Platform
{
    /// <summary>
    /// 平台信息。
    /// </summary>
    public sealed class PlatformInfo
    {
        /// <summary>
        /// 运行平台名称。
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// 操作系统信息。
        /// </summary>
        public string OperatingSystem { get; set; }

        /// <summary>
        /// Unity 版本。
        /// </summary>
        public string UnityVersion { get; set; }

        /// <summary>
        /// 设备型号。
        /// </summary>
        public string DeviceModel { get; set; }
    }
}
