using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using xFrame.Runtime.Startup;

namespace xFrame.Runtime.Unity.Startup
{
    /// <summary>
    /// 启动任务安装器示例。
    /// </summary>
    public class UnityStartupInstallerExample : UnityStartupInstallerBase
    {
        [Header("Optional Steps")]
        [SerializeField] private bool _includeUpdateAndSdk = true;
        [SerializeField] private bool _includeNetwork = true;

        public override void Install(StartupTaskRegistry registry)
        {
            registry.Register(StartupTaskKey.InitLogger, () => CreateTask(StartupTaskKey.InitLogger, 1f));
            registry.Register(StartupTaskKey.LoadLocalConfig, () => CreateTask(StartupTaskKey.LoadLocalConfig, 1f));
            registry.Register(StartupTaskKey.CheckUpdate, () => CreateTask(StartupTaskKey.CheckUpdate, 1f));
            registry.Register(StartupTaskKey.SdkInit, () => CreateTask(StartupTaskKey.SdkInit, 1f));
            registry.Register(StartupTaskKey.NetworkConnect, () => CreateTask(StartupTaskKey.NetworkConnect, 1f));
            registry.Register(StartupTaskKey.MockLogin, () => CreateTask(StartupTaskKey.MockLogin, 0.5f));
            registry.Register(StartupTaskKey.LoadTestBattleScene, () => CreateTask(StartupTaskKey.LoadTestBattleScene, 0.5f));
            registry.Register(StartupTaskKey.EnterLobby, () => CreateTask(StartupTaskKey.EnterLobby, 1f));

            if (!_includeUpdateAndSdk)
            {
                registry.Register(StartupTaskKey.CheckUpdate, () => CreateTask(StartupTaskKey.CheckUpdate, 0f, StartupTaskFailurePolicy.ContinuePipeline));
                registry.Register(StartupTaskKey.SdkInit, () => CreateTask(StartupTaskKey.SdkInit, 0f, StartupTaskFailurePolicy.ContinuePipeline));
            }

            if (!_includeNetwork)
            {
                registry.Register(StartupTaskKey.NetworkConnect, () => CreateTask(StartupTaskKey.NetworkConnect, 0f, StartupTaskFailurePolicy.ContinuePipeline));
            }
        }

        private static IStartupTask CreateTask(
            StartupTaskKey taskKey,
            float weight,
            StartupTaskFailurePolicy failurePolicy = StartupTaskFailurePolicy.StopPipeline)
        {
            return new DelegateStartupTask(
                taskKey.ToString(),
                weight,
                (_, _) => Task.FromResult(StartupTaskResult.Success()),
                failurePolicy,
                new StartupTaskExecutionOptions(0, Timeout.InfiniteTimeSpan));
        }
    }
}
