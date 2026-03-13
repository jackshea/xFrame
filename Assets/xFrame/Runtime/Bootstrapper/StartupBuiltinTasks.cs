using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using xFrame.Runtime.Logging;
using xFrame.Runtime.Networking;
using xFrame.Runtime.Platform;

namespace xFrame.Runtime.Startup
{
    /// <summary>
    ///     启动上下文内置键。
    /// </summary>
    public static class StartupContextKeys
    {
        public const string BootEnvironment = "startup.environment";
        public const string PersistentDataPath = "startup.platform.persistentDataPath";
        public const string StreamingAssetsPath = "startup.platform.streamingAssetsPath";
        public const string PlatformInfo = "startup.platform.info";
        public const string TerminalFlowHandled = "startup.flow.terminalHandled";
    }

    /// <summary>
    ///     启动网络端点提供器。
    /// </summary>
    public interface IStartupNetworkEndpointProvider
    {
        /// <summary>
        ///     根据启动环境返回当前应连接的端点。
        /// </summary>
        string GetStartupEndpoint(BootEnvironment environment, StartupTaskContext context);
    }

    /// <summary>
    ///     启动更新服务扩展点。
    /// </summary>
    public interface IStartupUpdateService
    {
        /// <summary>
        ///     执行启动阶段的版本检查。
        /// </summary>
        Task<StartupTaskResult> CheckForUpdatesAsync(StartupTaskContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    ///     启动 SDK 服务扩展点。
    /// </summary>
    public interface IStartupSdkService
    {
        /// <summary>
        ///     执行启动阶段的 SDK 初始化。
        /// </summary>
        Task<StartupTaskResult> InitializeSdkAsync(StartupTaskContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    ///     启动认证服务扩展点。
    /// </summary>
    public interface IStartupAuthenticationService
    {
        /// <summary>
        ///     执行启动阶段的登录流程。
        /// </summary>
        Task<StartupTaskResult> LoginAsync(
            bool useMockLogin,
            StartupTaskContext context,
            CancellationToken cancellationToken);
    }

    /// <summary>
    ///     启动流转服务扩展点。
    /// </summary>
    public interface IStartupFlowService
    {
        /// <summary>
        ///     进入 Lobby 主流程。
        /// </summary>
        Task<StartupTaskResult> EnterLobbyAsync(StartupTaskContext context, CancellationToken cancellationToken);

        /// <summary>
        ///     加载测试战斗场景。
        /// </summary>
        Task<StartupTaskResult> LoadTestBattleSceneAsync(StartupTaskContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    ///     启动内置任务工厂。
    /// </summary>
    public static class StartupBuiltinTaskFactory
    {
        /// <summary>
        ///     创建日志初始化任务。
        /// </summary>
        public static IStartupTask CreateInitLoggerTask(StartupTaskContext context, float weight)
        {
            return new DelegateStartupTask(
                StartupTaskKey.InitLogger.ToString(),
                weight,
                (_, cancellationToken) => InitializeLoggerAsync(context, cancellationToken));
        }

        /// <summary>
        ///     创建本地环境信息加载任务。
        /// </summary>
        public static IStartupTask CreateLoadLocalConfigTask(StartupTaskContext context, float weight)
        {
            return new DelegateStartupTask(
                StartupTaskKey.LoadLocalConfig.ToString(),
                weight,
                (_, cancellationToken) => LoadLocalConfigAsync(context, cancellationToken));
        }

        /// <summary>
        ///     创建版本检查任务。
        /// </summary>
        public static IStartupTask CreateCheckUpdateTask(
            StartupTaskContext context,
            float weight,
            StartupTaskFailurePolicy failurePolicy = StartupTaskFailurePolicy.StopPipeline)
        {
            return new DelegateStartupTask(
                StartupTaskKey.CheckUpdate.ToString(),
                weight,
                (_, cancellationToken) => ExecuteOptionalAsync<IStartupUpdateService>(
                    context,
                    failurePolicy,
                    "未注册更新服务，跳过版本检查。",
                    service => service.CheckForUpdatesAsync(context, cancellationToken)),
                failurePolicy);
        }

        /// <summary>
        ///     创建 SDK 初始化任务。
        /// </summary>
        public static IStartupTask CreateSdkInitTask(
            StartupTaskContext context,
            float weight,
            StartupTaskFailurePolicy failurePolicy = StartupTaskFailurePolicy.StopPipeline)
        {
            return new DelegateStartupTask(
                StartupTaskKey.SdkInit.ToString(),
                weight,
                (_, cancellationToken) => ExecuteOptionalAsync<IStartupSdkService>(
                    context,
                    failurePolicy,
                    "未注册 SDK 服务，跳过 SDK 初始化。",
                    service => service.InitializeSdkAsync(context, cancellationToken)),
                failurePolicy);
        }

        /// <summary>
        ///     创建网络连接任务。
        /// </summary>
        public static IStartupTask CreateNetworkConnectTask(
            StartupTaskContext context,
            float weight,
            StartupTaskFailurePolicy failurePolicy = StartupTaskFailurePolicy.StopPipeline)
        {
            return new DelegateStartupTask(
                StartupTaskKey.NetworkConnect.ToString(),
                weight,
                (_, cancellationToken) => ConnectNetworkAsync(context, failurePolicy, cancellationToken),
                failurePolicy);
        }

        /// <summary>
        ///     创建模拟登录任务。
        /// </summary>
        public static IStartupTask CreateMockLoginTask(
            StartupTaskContext context,
            float weight,
            StartupTaskFailurePolicy failurePolicy = StartupTaskFailurePolicy.StopPipeline)
        {
            return new DelegateStartupTask(
                StartupTaskKey.MockLogin.ToString(),
                weight,
                (_, cancellationToken) => ExecuteOptionalAsync<IStartupAuthenticationService>(
                    context,
                    failurePolicy,
                    "未注册认证服务，跳过模拟登录。",
                    service => service.LoginAsync(true, context, cancellationToken)),
                failurePolicy);
        }

        /// <summary>
        ///     创建测试战斗场景加载任务。
        /// </summary>
        public static IStartupTask CreateLoadTestBattleSceneTask(
            StartupTaskContext context,
            float weight,
            StartupTaskFailurePolicy failurePolicy = StartupTaskFailurePolicy.StopPipeline)
        {
            return new DelegateStartupTask(
                StartupTaskKey.LoadTestBattleScene.ToString(),
                weight,
                (_, cancellationToken) => ExecuteFlowAsync(
                    context,
                    failurePolicy,
                    "未注册启动流转服务，跳过测试战斗场景加载。",
                    service => service.LoadTestBattleSceneAsync(context, cancellationToken)),
                failurePolicy);
        }

        /// <summary>
        ///     创建进入 Lobby 任务。
        /// </summary>
        public static IStartupTask CreateEnterLobbyTask(
            StartupTaskContext context,
            float weight,
            StartupTaskFailurePolicy failurePolicy = StartupTaskFailurePolicy.StopPipeline)
        {
            return new DelegateStartupTask(
                StartupTaskKey.EnterLobby.ToString(),
                weight,
                (_, cancellationToken) => ExecuteFlowAsync(
                    context,
                    failurePolicy,
                    "未注册启动流转服务，跳过 Lobby 入口。",
                    service => service.EnterLobbyAsync(context, cancellationToken)),
                failurePolicy);
        }

        private static Task<StartupTaskResult> InitializeLoggerAsync(
            StartupTaskContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (context?.Services == null)
                return Task.FromResult(StartupTaskResult.Failed("MissingServices", "缺少启动服务解析器", true));

            if (!context.Services.TryResolve(out IXLogManager logManager))
                return Task.FromResult(StartupTaskResult.Failed("MissingLogger", "无法解析 IXLogManager", true));

            if (!context.Services.TryResolve(out XLoggingModule loggingModule))
                return Task.FromResult(StartupTaskResult.Failed("MissingLoggingModule", "无法解析 XLoggingModule", true));

            loggingModule.OnInit();
            logManager.GetLogger("Startup").Info("启动日志系统已初始化");
            return Task.FromResult(StartupTaskResult.Success());
        }

        private static Task<StartupTaskResult> LoadLocalConfigAsync(
            StartupTaskContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (context?.Services == null)
                return Task.FromResult(StartupTaskResult.Failed("MissingServices", "缺少启动服务解析器", true));

            if (!context.Services.TryResolve(out IPlatformService platformService))
                return Task.FromResult(StartupTaskResult.Failed("MissingPlatformService", "无法解析 IPlatformService", true));

            context.SetValue(StartupContextKeys.PersistentDataPath, platformService.PersistentDataPath);
            context.SetValue(StartupContextKeys.StreamingAssetsPath, platformService.StreamingAssetsPath);
            context.SetValue(StartupContextKeys.PlatformInfo, platformService.GetPlatformInfo());

            if (context.Services.TryResolve(out IXLogManager logManager))
            {
                var logger = logManager.GetLogger("Startup");
                logger.Info($"PersistentDataPath: {platformService.PersistentDataPath}");
                logger.Info($"StreamingAssetsPath: {platformService.StreamingAssetsPath}");
            }

            return Task.FromResult(StartupTaskResult.Success());
        }

        private static async Task<StartupTaskResult> ConnectNetworkAsync(
            StartupTaskContext context,
            StartupTaskFailurePolicy failurePolicy,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (context?.Services == null)
                return StartupTaskResult.Failed("MissingServices", "缺少启动服务解析器", true);

            if (!context.Services.TryResolve(out INetworkClient networkClient))
                return StartupTaskResult.Failed("MissingNetworkClient", "无法解析 INetworkClient", true);

            if (!context.Services.TryResolve(out IStartupNetworkEndpointProvider endpointProvider))
                return CreateOptionalSkipResult(context, failurePolicy, "未注册网络端点提供器，跳过网络连接。");

            var environment = BootEnvironment.DevFull;
            if (context.TryGetValue(StartupContextKeys.BootEnvironment, out BootEnvironment resolvedEnvironment))
                environment = resolvedEnvironment;

            var endpoint = endpointProvider.GetStartupEndpoint(environment, context);
            if (string.IsNullOrWhiteSpace(endpoint))
                return CreateOptionalSkipResult(context, failurePolicy, "启动网络端点为空，跳过网络连接。");

            await networkClient.ConnectAsync(endpoint, cancellationToken);

            if (context.Services.TryResolve(out IXLogManager logManager))
                logManager.GetLogger("Startup").Info($"已连接启动网络端点: {endpoint}");

            return StartupTaskResult.Success();
        }

        private static async Task<StartupTaskResult> ExecuteOptionalAsync<TService>(
            StartupTaskContext context,
            StartupTaskFailurePolicy failurePolicy,
            string skipMessage,
            Func<TService, Task<StartupTaskResult>> execute)
            where TService : class
        {
            if (context?.Services == null)
                return StartupTaskResult.Failed("MissingServices", "缺少启动服务解析器", true);

            if (!context.Services.TryResolve(out TService service))
                return CreateOptionalSkipResult(context, failurePolicy, skipMessage);

            return await execute.Invoke(service);
        }

        private static async Task<StartupTaskResult> ExecuteFlowAsync(
            StartupTaskContext context,
            StartupTaskFailurePolicy failurePolicy,
            string skipMessage,
            Func<IStartupFlowService, Task<StartupTaskResult>> execute)
        {
            var result = await ExecuteOptionalAsync(
                context,
                failurePolicy,
                skipMessage,
                execute);

            if (result.IsSuccess)
                context?.SetValue(StartupContextKeys.TerminalFlowHandled, true);

            return result;
        }

        private static StartupTaskResult CreateOptionalSkipResult(
            StartupTaskContext context,
            StartupTaskFailurePolicy failurePolicy,
            string message)
        {
            if (context?.Services != null && context.Services.TryResolve(out IXLogManager logManager))
                logManager.GetLogger("Startup").Warning(message);

            return failurePolicy == StartupTaskFailurePolicy.ContinuePipeline
                ? StartupTaskResult.Failed("OptionalTaskSkipped", message, false)
                : StartupTaskResult.Success();
        }
    }
}
