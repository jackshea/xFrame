using UnityEngine;

namespace xFrame.Runtime.Platform
{
    /// <summary>
    /// Unity 平台服务实现。
    /// </summary>
    public sealed class UnityPlatformService : IPlatformService
    {
        public string PersistentDataPath => Application.persistentDataPath;

        public string StreamingAssetsPath => Application.streamingAssetsPath;

        public bool IsEditor => Application.isEditor;

        public PlatformInfo GetPlatformInfo()
        {
            return new PlatformInfo
            {
                Platform = Application.platform.ToString(),
                OperatingSystem = SystemInfo.operatingSystem,
                UnityVersion = Application.unityVersion,
                DeviceModel = SystemInfo.deviceModel,
            };
        }
    }
}
