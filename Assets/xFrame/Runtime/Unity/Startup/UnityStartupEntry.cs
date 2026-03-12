using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using xFrame.Runtime.Startup;
using xFrame.Runtime.UI;

namespace xFrame.Runtime.Unity.Startup
{
    /// <summary>
    ///     Unity 启动入口薄层，仅负责拼装并触发纯 C# 流程。
    /// </summary>
    public class UnityStartupEntry : MonoBehaviour
    {
        [Header("Startup")] [SerializeField] private BootEnvironment _environment = BootEnvironment.DevFull;

        [SerializeField] private bool _autoRunOnStart = true;

        [Header("Dependencies")] [SerializeField]
        private UnityStartupView _view;

        [SerializeField] private UnityStartupInstallerBase _installer;
        private CancellationTokenSource _lifetimeTokenSource;

        private IStartupOrchestrator _orchestrator;

        private void Awake()
        {
            _lifetimeTokenSource = new CancellationTokenSource();
            UIEventSystemUtility.EnsureEventSystem(transform);
            EnsureOrchestrator();
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

            if (_orchestrator != null)
            {
                _orchestrator.ShutdownAsync(CancellationToken.None).GetAwaiter().GetResult();
                _orchestrator = null;
            }

            _lifetimeTokenSource.Dispose();
            _lifetimeTokenSource = null;
        }

        public async Task<StartupPipelineResult> RunAsync()
        {
            EnsureOrchestrator();
            return await _orchestrator.RunAsync(_environment, _lifetimeTokenSource.Token);
        }

        private void EnsureOrchestrator()
        {
            if (_orchestrator != null) return;

            StartupOrchestratorHost.Configure(
                _installer,
                view: (IStartupView)_view ?? NullStartupView.Instance);

            _orchestrator = StartupOrchestratorHost.GetOrCreate();
        }
    }
}
