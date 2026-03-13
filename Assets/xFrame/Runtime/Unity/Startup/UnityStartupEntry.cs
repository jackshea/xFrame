using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using xFrame.Runtime.Startup;

namespace xFrame.Runtime.Unity.Startup
{
    /// <summary>
    ///     Unity 启动入口薄层，仅负责拼装并触发纯 C# 流程。
    /// </summary>
    public class UnityStartupEntry : MonoBehaviour
    {
        [Header("Startup")] [SerializeField] private BootEnvironment _environment = BootEnvironment.DevFull;

        [SerializeField] private bool _autoRunOnStart = true;

        [SerializeField] private bool _dontDestroyScopeOnLoad = true;

        [Header("Dependencies")] [SerializeField]
        private UnityStartupView _view;

        [SerializeField] private UnityStartupInstallerBase _installer;
        [SerializeField] private LifetimeScope _lifetimeScopePrefab;
        private CancellationTokenSource _lifetimeTokenSource;
        private UnityStartupCompositionRoot _compositionRoot;
        private StartupRuntime _runtime;

        /// <summary>
        ///     当前启动链路绑定的 DI 容器。
        /// </summary>
        public LifetimeScope LifetimeScope => _compositionRoot?.LifetimeScope;

        private void Awake()
        {
            _lifetimeTokenSource = new CancellationTokenSource();
            _compositionRoot = new UnityStartupCompositionRoot(transform, _lifetimeScopePrefab, _dontDestroyScopeOnLoad);
            if (_view != null)
            {
                _compositionRoot.RegisterLocalService<IStartupView>(_view);
                _compositionRoot.RegisterLocalService<IStartupErrorPresentationService>(_view);
            }

            _runtime = new StartupRuntime(
                _compositionRoot,
                _installer,
                view: (IStartupView)_view ?? NullStartupView.Instance);
            _runtime.EnsureInitialized();
        }

        private async void Start()
        {
            if (!_autoRunOnStart) return;

            await RunAsync();
        }

        private void OnDestroy()
        {
            if (_lifetimeTokenSource == null) return;

            _lifetimeTokenSource.Cancel();

            if (_runtime != null)
            {
                _runtime.ShutdownAsync(CancellationToken.None).GetAwaiter().GetResult();
                _runtime.Dispose();
                _runtime = null;
            }

            _lifetimeTokenSource.Dispose();
            _lifetimeTokenSource = null;
        }

        public async Task<StartupPipelineResult> RunAsync()
        {
            return await _runtime.RunAsync(_environment, _lifetimeTokenSource.Token);
        }
    }
}
