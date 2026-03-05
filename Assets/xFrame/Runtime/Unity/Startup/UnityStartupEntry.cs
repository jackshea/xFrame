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

        private readonly StartupTaskRegistry _registry = new StartupTaskRegistry();
        private CancellationTokenSource _lifetimeTokenSource;

        private void Awake()
        {
            _lifetimeTokenSource = new CancellationTokenSource();
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
            var launcher = new StartupPipelineLauncher(_registry);
            var pipeline = launcher.Create(_environment, _installer, _view);
            return await pipeline.RunAsync(_lifetimeTokenSource.Token);
        }

        private void OnDestroy()
        {
            if (_lifetimeTokenSource == null)
            {
                return;
            }

            _lifetimeTokenSource.Cancel();
            _lifetimeTokenSource.Dispose();
            _lifetimeTokenSource = null;
        }
    }
}
