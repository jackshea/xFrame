using UnityEngine;
using xFrame.Runtime.Startup;

namespace xFrame.Runtime.Unity.Startup
{
    /// <summary>
    ///     启动任务安装器示例。
    /// </summary>
    public class UnityStartupInstallerExample : UnityStartupInstallerBase
    {
        [Header("Optional Steps")] [SerializeField]
        private bool _includeUpdateAndSdk = true;

        [SerializeField] private bool _includeNetwork = true;

        public override void Install(StartupTaskRegistry registry)
        {
            registry.Register(StartupTaskKey.InitLogger, context => StartupBuiltinTaskFactory.CreateInitLoggerTask(context, 1f));
            registry.Register(StartupTaskKey.LoadLocalConfig,
                context => StartupBuiltinTaskFactory.CreateLoadLocalConfigTask(context, 1f));
            registry.Register(StartupTaskKey.CheckUpdate,
                context => StartupBuiltinTaskFactory.CreateCheckUpdateTask(context, 1f));
            registry.Register(StartupTaskKey.SdkInit,
                context => StartupBuiltinTaskFactory.CreateSdkInitTask(context, 1f));
            registry.Register(StartupTaskKey.NetworkConnect,
                context => StartupBuiltinTaskFactory.CreateNetworkConnectTask(context, 1f));
            registry.Register(StartupTaskKey.MockLogin,
                context => StartupBuiltinTaskFactory.CreateMockLoginTask(context, 0.5f));
            registry.Register(StartupTaskKey.LoadTestBattleScene,
                context => StartupBuiltinTaskFactory.CreateLoadTestBattleSceneTask(context, 0.5f));
            registry.Register(StartupTaskKey.EnterLobby,
                context => StartupBuiltinTaskFactory.CreateEnterLobbyTask(context, 1f));

            if (!_includeUpdateAndSdk)
            {
                registry.Register(StartupTaskKey.CheckUpdate,
                    context => StartupBuiltinTaskFactory.CreateCheckUpdateTask(
                        context,
                        0f,
                        StartupTaskFailurePolicy.ContinuePipeline));
                registry.Register(StartupTaskKey.SdkInit,
                    context => StartupBuiltinTaskFactory.CreateSdkInitTask(
                        context,
                        0f,
                        StartupTaskFailurePolicy.ContinuePipeline));
            }

            if (!_includeNetwork)
                registry.Register(StartupTaskKey.NetworkConnect,
                    context => StartupBuiltinTaskFactory.CreateNetworkConnectTask(
                        context,
                        0f,
                        StartupTaskFailurePolicy.ContinuePipeline));
        }
    }
}
