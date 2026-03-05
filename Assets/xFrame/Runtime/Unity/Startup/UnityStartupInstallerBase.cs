using UnityEngine;
using xFrame.Runtime.Startup;

namespace xFrame.Runtime.Unity.Startup
{
    /// <summary>
    /// Unity 侧启动任务安装器基类。
    /// </summary>
    public abstract class UnityStartupInstallerBase : MonoBehaviour, IStartupTaskRegistryInstaller
    {
        public abstract void Install(StartupTaskRegistry registry);
    }
}
