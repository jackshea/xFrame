using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using xFrame.Runtime.Startup;

namespace xFrame.Runtime.Unity.Startup
{
    /// <summary>
    ///     Unity 启动视图薄适配层。
    /// </summary>
    public class UnityStartupView : MonoBehaviour, IStartupView
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

            Debug.LogError($"[Startup] {message}");
            return Task.FromResult(_autoRetryOnError);
        }

        public void HideLoading()
        {
            if (_loadingRoot != null) _loadingRoot.SetActive(false);

            Debug.Log("[Startup] completed");
        }
    }
}