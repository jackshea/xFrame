using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace xFrame.Runtime.Startup
{
    /// <summary>
    ///     启动期服务解析器接口。
    /// </summary>
    public interface IStartupServiceResolver
    {
        /// <summary>
        ///     解析指定类型的启动服务。
        /// </summary>
        T Resolve<T>() where T : class;

        /// <summary>
        ///     尝试解析指定类型的启动服务。
        /// </summary>
        bool TryResolve<T>(out T service) where T : class;
    }

    /// <summary>
    ///     启动依赖装配根接口。
    ///     用于在具体宿主环境中准备启动流程所需的基础依赖。
    /// </summary>
    public interface IStartupCompositionRoot : IStartupServiceResolver
    {
        /// <summary>
        ///     确保宿主环境完成启动所需的最小依赖装配。
        /// </summary>
        void EnsureInitialized();
    }

    /// <summary>
    ///     启动任务运行时上下文。
    /// </summary>
    public sealed class StartupTaskContext
    {
        private readonly Dictionary<string, object> _items = new();

        public StartupTaskContext(IStartupServiceResolver services)
        {
            Services = services;
        }

        /// <summary>
        ///     启动期服务解析入口。
        /// </summary>
        public IStartupServiceResolver Services { get; }

        /// <summary>
        ///     写入启动上下文共享值。
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("key 不能为空", nameof(key));

            _items[key] = value;
        }

        /// <summary>
        ///     尝试读取启动上下文共享值。
        /// </summary>
        public bool TryGetValue<T>(string key, out T value)
        {
            if (_items.TryGetValue(key, out var rawValue) && rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }
    }

    /// <summary>
    ///     纯 C# 启动运行时。
    ///     负责协调依赖装配与启动编排，不直接依赖 Unity 生命周期。
    /// </summary>
    public sealed class StartupRuntime : IDisposable
    {
        private readonly IStartupCompositionRoot _compositionRoot;
        private readonly IStartupTaskInstaller _installer;
        private readonly IStartupLifecycleSink _lifecycleSinkOverride;
        private readonly IStartupProfileProvider _profileProvider;
        private readonly IStartupView _view;
        private IReadOnlyList<IStartupLifecycleHandler> _lifecycleHandlers;
        private IStartupLifecycleState _lifecycleState;
        private IStartupLifecycleSink _lifecycleSink;
        private readonly StartupTaskRegistry _registry;
        private IStartupOrchestrator _orchestrator;
        private StartupTaskContext _taskContext;

        public StartupRuntime(
            IStartupCompositionRoot compositionRoot,
            IStartupTaskInstaller installer = null,
            IStartupProfileProvider profileProvider = null,
            IStartupView view = null,
            IStartupLifecycleSink lifecycleSink = null)
        {
            _compositionRoot = compositionRoot;
            _installer = installer;
            _profileProvider = profileProvider;
            _view = view;
            _lifecycleSinkOverride = lifecycleSink;
            _registry = new StartupTaskRegistry();
        }

        /// <summary>
        ///     当前启动生命周期只读状态。
        /// </summary>
        public IStartupLifecycleState LifecycleState
        {
            get
            {
                EnsureInitialized();
                return _lifecycleState;
            }
        }

        public void Dispose()
        {
            if (_orchestrator is IDisposable disposable) disposable.Dispose();
            _orchestrator = null;
        }

        /// <summary>
        ///     确保装配根、任务上下文和启动编排器已就绪。
        /// </summary>
        public void EnsureInitialized()
        {
            _compositionRoot?.EnsureInitialized();
            EnsureLifecycleState();
            EnsureLifecycleHandlers();
            _taskContext ??= new StartupTaskContext(_compositionRoot);
            _registry.SetContext(_taskContext);

            if (_orchestrator != null) return;

            StartupOrchestratorHost.Configure(
                _installer,
                _profileProvider,
                _view ?? NullStartupView.Instance,
                _registry);

            _orchestrator = StartupOrchestratorHost.GetOrCreate();
        }

        /// <summary>
        ///     运行指定环境下的启动流程。
        /// </summary>
        public async Task<StartupPipelineResult> RunAsync(BootEnvironment environment, CancellationToken cancellationToken)
        {
            EnsureInitialized();
            await PublishLifecycleAsync(StartupLifecycleStage.Starting, environment, default, cancellationToken);

            try
            {
                var result = await _orchestrator.RunAsync(environment, cancellationToken);
                await PublishLifecycleAsync(
                    result.IsSuccess ? StartupLifecycleStage.Succeeded : StartupLifecycleStage.Failed,
                    environment,
                    result,
                    cancellationToken);
                return result;
            }
            catch (OperationCanceledException)
            {
                await PublishLifecycleAsync(StartupLifecycleStage.Stopped, environment, default, CancellationToken.None);
                throw;
            }
        }

        /// <summary>
        ///     关闭当前启动运行时。
        /// </summary>
        public async Task ShutdownAsync(CancellationToken cancellationToken)
        {
            if (_orchestrator == null) return;

            await _orchestrator.ShutdownAsync(cancellationToken);
            await PublishLifecycleAsync(
                StartupLifecycleStage.Stopped,
                ResolveCurrentEnvironment(),
                default,
                cancellationToken);
        }

        private void EnsureLifecycleState()
        {
            if (_lifecycleSink != null && _lifecycleState != null) return;

            _lifecycleSink = _lifecycleSinkOverride;
            if (_lifecycleSink == null && _compositionRoot != null &&
                _compositionRoot.TryResolve(out IStartupLifecycleSink resolvedSink))
                _lifecycleSink = resolvedSink;

            if (_lifecycleSink == null)
                _lifecycleSink = new StartupLifecycleStateStore();

            _lifecycleState = _lifecycleSink as IStartupLifecycleState;
            if (_lifecycleState == null)
            {
                var fallbackStore = new StartupLifecycleStateStore();
                _lifecycleSink = fallbackStore;
                _lifecycleState = fallbackStore;
            }
        }

        private void EnsureLifecycleHandlers()
        {
            if (_lifecycleHandlers != null) return;

            if (_compositionRoot != null &&
                _compositionRoot.TryResolve(out IEnumerable<IStartupLifecycleHandler> resolvedHandlers) &&
                resolvedHandlers != null)
            {
                _lifecycleHandlers = resolvedHandlers.ToArray();
                return;
            }

            _lifecycleHandlers = Array.Empty<IStartupLifecycleHandler>();
        }

        private async Task PublishLifecycleAsync(
            StartupLifecycleStage stage,
            BootEnvironment environment,
            StartupPipelineResult pipelineResult,
            CancellationToken cancellationToken)
        {
            var snapshot = new StartupLifecycleSnapshot(stage, environment, pipelineResult);
            _lifecycleSink?.Publish(snapshot);

            for (var i = 0; i < _lifecycleHandlers.Count; i++)
                await _lifecycleHandlers[i].HandleAsync(snapshot, _taskContext, cancellationToken);
        }

        private BootEnvironment ResolveCurrentEnvironment()
        {
            if (_taskContext != null &&
                _taskContext.TryGetValue(StartupContextKeys.BootEnvironment, out BootEnvironment environment))
                return environment;

            return BootEnvironment.DevFull;
        }
    }
}
