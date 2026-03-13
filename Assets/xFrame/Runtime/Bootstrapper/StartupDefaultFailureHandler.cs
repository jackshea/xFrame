using System.Threading;
using System.Threading.Tasks;
using xFrame.Runtime.Logging;

namespace xFrame.Runtime.Startup
{
    /// <summary>
    ///     默认启动失败处理器。
    ///     负责统一记录启动失败日志，并将错误信息转交给宿主层展示服务。
    /// </summary>
    public sealed class StartupDefaultFailureHandler : IStartupFailureHandler
    {
        /// <summary>
        ///     处理启动失败结果。
        /// </summary>
        public async Task HandleFailureAsync(
            StartupLifecycleSnapshot snapshot,
            StartupTaskContext context,
            CancellationToken cancellationToken)
        {
            if (snapshot.Stage != StartupLifecycleStage.Failed)
                return;

            var failureResult = snapshot.PipelineResult.FailureResult;
            LogFailure(context, snapshot.PipelineResult.FailedTaskName, failureResult);

            if (context?.Services == null)
                return;

            if (!context.Services.TryResolve(out IStartupErrorPresentationService presentationService))
                return;

            await presentationService.PresentErrorAsync(snapshot, context, cancellationToken);
        }

        private static void LogFailure(
            StartupTaskContext context,
            string failedTaskName,
            StartupTaskResult failureResult)
        {
            if (context?.Services == null)
                return;

            if (!context.Services.TryResolve(out IXLogManager logManager))
                return;

            var logger = logManager.GetLogger("Startup");
            var message = CreateMessage(failedTaskName, failureResult);
            if (failureResult.Exception != null)
            {
                logger.Error(message, failureResult.Exception);
                return;
            }

            logger.Error(message);
        }

        private static string CreateMessage(string failedTaskName, StartupTaskResult failureResult)
        {
            var taskName = string.IsNullOrWhiteSpace(failedTaskName) ? "UnknownTask" : failedTaskName;
            var errorCode = string.IsNullOrWhiteSpace(failureResult.ErrorCode) ? "Unknown" : failureResult.ErrorCode;
            var errorMessage = string.IsNullOrWhiteSpace(failureResult.ErrorMessage)
                ? "启动失败，未提供错误信息。"
                : failureResult.ErrorMessage;
            return $"启动失败，任务: {taskName}，错误码: {errorCode}，信息: {errorMessage}";
        }
    }
}
