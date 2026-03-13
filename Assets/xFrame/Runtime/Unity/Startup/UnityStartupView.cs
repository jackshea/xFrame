using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using xFrame.Runtime.Startup;

namespace xFrame.Runtime.Unity.Startup
{
    /// <summary>
    ///     Unity 启动视图薄适配层。
    /// </summary>
    public class UnityStartupView : MonoBehaviour, IStartupView, IStartupErrorPresentationService
    {
        [SerializeField] private bool _autoRetryOnError;
        [SerializeField] private GameObject _loadingRoot;
        [SerializeField] private GameObject _errorRoot;

        public string LastMessage { get; private set; }

        public float LastProgress { get; private set; }

        public void ShowLoading(string message, float progress)
        {
            LastMessage = message;
            LastProgress = progress;

            if (_loadingRoot != null) _loadingRoot.SetActive(true);

            if (_errorRoot != null) _errorRoot.SetActive(false);

            Debug.Log($"[Startup] {message} ({progress:P0})");
        }

        public Task<bool> ShowErrorDialogAsync(string message, CancellationToken cancellationToken)
        {
            if (_errorRoot != null) _errorRoot.SetActive(true);

            LastMessage = message;
            Debug.LogError($"[Startup] {message}");
            return Task.FromResult(_autoRetryOnError);
        }

        /// <summary>
        ///     展示统一启动失败出口传入的错误信息。
        /// </summary>
        public Task PresentErrorAsync(
            StartupLifecycleSnapshot snapshot,
            StartupTaskContext context,
            CancellationToken cancellationToken)
        {
            return ShowErrorDialogAsync(CreateErrorMessage(snapshot), cancellationToken);
        }

        public void HideLoading()
        {
            if (_loadingRoot != null) _loadingRoot.SetActive(false);

            Debug.Log("[Startup] completed");
        }

        private static string CreateErrorMessage(StartupLifecycleSnapshot snapshot)
        {
            var failureResult = snapshot.PipelineResult.FailureResult;
            if (!string.IsNullOrWhiteSpace(failureResult.ErrorMessage))
                return failureResult.ErrorMessage;

            return string.IsNullOrWhiteSpace(snapshot.PipelineResult.FailedTaskName)
                ? "启动失败"
                : $"启动失败: {snapshot.PipelineResult.FailedTaskName}";
        }
    }
}
