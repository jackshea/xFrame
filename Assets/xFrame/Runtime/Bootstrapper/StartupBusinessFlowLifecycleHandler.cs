using System.Threading;
using System.Threading.Tasks;

namespace xFrame.Runtime.Startup
{
    /// <summary>
    ///     默认启动业务流转处理器。
    ///     在启动成功后补齐终态流转，在启动失败后转交给恢复处理器。
    /// </summary>
    public sealed class StartupBusinessFlowLifecycleHandler : IStartupLifecycleHandler
    {
        /// <summary>
        ///     处理启动成功或失败后的统一业务流转。
        /// </summary>
        public async Task HandleAsync(
            StartupLifecycleSnapshot snapshot,
            StartupTaskContext context,
            CancellationToken cancellationToken)
        {
            switch (snapshot.Stage)
            {
                case StartupLifecycleStage.Succeeded:
                    await HandleSucceededAsync(snapshot, context, cancellationToken);
                    break;
                case StartupLifecycleStage.Failed:
                    await HandleFailedAsync(snapshot, context, cancellationToken);
                    break;
            }
        }

        private static async Task HandleSucceededAsync(
            StartupLifecycleSnapshot snapshot,
            StartupTaskContext context,
            CancellationToken cancellationToken)
        {
            if (context == null || context.Services == null)
                return;

            if (context.TryGetValue(StartupContextKeys.TerminalFlowHandled, out bool terminalHandled) && terminalHandled)
                return;

            if (!context.Services.TryResolve(out IStartupFlowService flowService))
                return;

            StartupTaskResult result;
            switch (snapshot.Environment)
            {
                case BootEnvironment.DevSkipToBattle:
                    result = await flowService.LoadTestBattleSceneAsync(context, cancellationToken);
                    break;
                case BootEnvironment.Release:
                case BootEnvironment.DevFull:
                default:
                    result = await flowService.EnterLobbyAsync(context, cancellationToken);
                    break;
            }

            if (result.IsSuccess)
            {
                context.SetValue(StartupContextKeys.TerminalFlowHandled, true);
                return;
            }

            await HandleFailedAsync(
                new StartupLifecycleSnapshot(StartupLifecycleStage.Failed, snapshot.Environment,
                    StartupPipelineResult.Failed("PostStartupFlow", result, false)),
                context,
                cancellationToken);
        }

        private static async Task HandleFailedAsync(
            StartupLifecycleSnapshot snapshot,
            StartupTaskContext context,
            CancellationToken cancellationToken)
        {
            if (context?.Services == null)
                return;

            if (!context.Services.TryResolve(out IStartupFailureHandler failureHandler))
                return;

            await failureHandler.HandleFailureAsync(snapshot, context, cancellationToken);
        }
    }
}
