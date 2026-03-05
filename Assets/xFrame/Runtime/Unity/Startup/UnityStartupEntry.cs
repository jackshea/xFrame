using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using xFrame.Runtime.Startup;

namespace xFrame.Runtime.Unity.Startup
{
    /// <summary>
    /// Unity 启动入口薄层，仅负责拼装并触发纯 C# 流程。
    /// </summary>
    public class UnityStartupEntry : MonoBehaviour
    {
        [Header("Startup")]
        [SerializeField] private BootEnvironment _environment = BootEnvironment.DevFull;
        [SerializeField] private bool _autoRunOnStart = true;

        [Header("Dependencies")]
        [SerializeField] private UnityStartupView _view;
        [SerializeField] private UnityStartupInstallerBase _installer;

        private IStartupOrchestrator _orchestrator;
        private CancellationTokenSource _lifetimeTokenSource;

        private void Awake()
        {
            _lifetimeTokenSource = new CancellationTokenSource();
            EnsureOrchestrator();
        }

        private async void Start()
        {
            if (!_autoRunOnStart)
            {
                return;
            }

            await RunAsync();
        }

        public async Task<StartupPipelineResult> RunAsync()
        {
            EnsureOrchestrator();
            return await _orchestrator.RunAsync(_environment, _lifetimeTokenSource.Token);
        }

        private void OnDestroy()
        {
            if (_lifetimeTokenSource == null)
            {
                return;
            }

            _lifetimeTokenSource.Cancel();

            if (_orchestrator != null)
            {
                _orchestrator.ShutdownAsync(CancellationToken.None).GetAwaiter().GetResult();
                _orchestrator = null;
            }

            _lifetimeTokenSource.Dispose();
            _lifetimeTokenSource = null;
        }

        private void EnsureOrchestrator()
        {
            if (_orchestrator != null)
            {
                return;
            }

            StartupOrchestratorHost.Configure(
                installer: _installer,
                view: (IStartupView)_view ?? NullStartupView.Instance);

            _orchestrator = StartupOrchestratorHost.GetOrCreate();
        }
    }
}
