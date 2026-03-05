using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace xFrame.Runtime.Startup
{
    /// <summary>
    /// 启动任务失败时的流程策略。
    /// </summary>
    public enum StartupTaskFailurePolicy
    {
        StopPipeline = 0,
        ContinuePipeline = 1
    }

    /// <summary>
    /// 启动环境。
    /// </summary>
    public enum BootEnvironment
    {
        Release = 0,
        DevFull = 1,
        DevSkipToBattle = 2
    }

    /// <summary>
    /// 启动任务键。
    /// </summary>
    public enum StartupTaskKey
    {
        InitLogger = 0,
        LoadLocalConfig = 1,
        CheckUpdate = 2,
        SdkInit = 3,
        NetworkConnect = 4,
        MockLogin = 5,
        LoadTestBattleScene = 6,
        EnterLobby = 7
    }

    /// <summary>
    /// 任务执行选项。
    /// </summary>
    public struct StartupTaskExecutionOptions
    {
        public static StartupTaskExecutionOptions Default => new StartupTaskExecutionOptions(0, Timeout.InfiniteTimeSpan);

        public StartupTaskExecutionOptions(int maxRetryCount, TimeSpan timeoutDuration = default)
        {
            if (maxRetryCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRetryCount), "maxRetryCount 不能小于 0");
            }

            if (timeoutDuration == default)
            {
                timeoutDuration = Timeout.InfiniteTimeSpan;
            }

            if (timeoutDuration <= TimeSpan.Zero && timeoutDuration != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutDuration), "timeoutDuration 必须是正数或 Infinite");
            }

            MaxRetryCount = maxRetryCount;
            TimeoutDuration = timeoutDuration;
        }

        public int MaxRetryCount { get; }

        public TimeSpan TimeoutDuration { get; }

        public bool HasTimeout => TimeoutDuration > TimeSpan.Zero && TimeoutDuration != Timeout.InfiniteTimeSpan;
    }

    /// <summary>
    /// 启动任务执行结果。
    /// </summary>
    public struct StartupTaskResult
    {
        public bool IsSuccess { get; set; }

        public bool IsFatal { get; set; }

        public string ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        public Exception Exception { get; set; }

        public static StartupTaskResult Success()
        {
            return new StartupTaskResult
            {
                IsSuccess = true,
                IsFatal = false,
                ErrorCode = string.Empty,
                ErrorMessage = string.Empty,
                Exception = null
            };
        }

        public static StartupTaskResult Failed(string errorCode, string errorMessage, bool isFatal, Exception exception = null)
        {
            return new StartupTaskResult
            {
                IsSuccess = false,
                IsFatal = isFatal,
                ErrorCode = errorCode ?? string.Empty,
                ErrorMessage = errorMessage ?? string.Empty,
                Exception = exception
            };
        }
    }

    /// <summary>
    /// Unity 表现层接口。
    /// </summary>
    public interface IStartupView
    {
        void ShowLoading(string message, float progress);

        Task<bool> ShowErrorDialogAsync(string message, CancellationToken cancellationToken);

        void HideLoading();
    }

    /// <summary>
    /// 空启动视图，适用于无 UI 启动。
    /// </summary>
    public sealed class NullStartupView : IStartupView
    {
        public static readonly NullStartupView Instance = new NullStartupView();

        private NullStartupView()
        {
        }

        public void ShowLoading(string message, float progress)
        {
        }

        public Task<bool> ShowErrorDialogAsync(string message, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public void HideLoading()
        {
        }
    }

    /// <summary>
    /// 启动任务接口。
    /// </summary>
    public interface IStartupTask
    {
        string TaskName { get; }

        float Weight { get; }

        StartupTaskFailurePolicy FailurePolicy { get; }

        StartupTaskExecutionOptions ExecutionOptions { get; }

        void InjectView(IStartupView view);

        Task<StartupTaskResult> ExecuteAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// 基于委托的启动任务，便于快速组装。
    /// </summary>
    public sealed class DelegateStartupTask : IStartupTask
    {
        private readonly Func<IStartupView, CancellationToken, Task<StartupTaskResult>> _execute;
        private IStartupView _view;

        public DelegateStartupTask(
            string taskName,
            float weight,
            Func<IStartupView, CancellationToken, Task<StartupTaskResult>> execute,
            StartupTaskFailurePolicy failurePolicy = StartupTaskFailurePolicy.StopPipeline,
            StartupTaskExecutionOptions? executionOptions = null)
        {
            if (string.IsNullOrEmpty(taskName))
            {
                throw new ArgumentException("taskName 不能为空", nameof(taskName));
            }

            if (weight < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(weight), "weight 不能小于 0");
            }

            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            TaskName = taskName;
            Weight = weight;
            FailurePolicy = failurePolicy;
            ExecutionOptions = executionOptions ?? StartupTaskExecutionOptions.Default;
        }

        public string TaskName { get; }

        public float Weight { get; }

        public StartupTaskFailurePolicy FailurePolicy { get; }

        public StartupTaskExecutionOptions ExecutionOptions { get; }

        public void InjectView(IStartupView view)
        {
            _view = view;
        }

        public Task<StartupTaskResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            return _execute.Invoke(_view, cancellationToken);
        }
    }

    /// <summary>
    /// 启动流程运行结果。
    /// </summary>
    public struct StartupPipelineResult
    {
        public bool IsSuccess { get; set; }

        public bool IsCancelled { get; set; }

        public string FailedTaskName { get; set; }

        public StartupTaskResult FailureResult { get; set; }

        public static StartupPipelineResult Succeeded()
        {
            return new StartupPipelineResult
            {
                IsSuccess = true,
                IsCancelled = false,
                FailedTaskName = string.Empty,
                FailureResult = StartupTaskResult.Success()
            };
        }

        public static StartupPipelineResult Failed(string failedTaskName, StartupTaskResult failureResult, bool isCancelled)
        {
            return new StartupPipelineResult
            {
                IsSuccess = false,
                IsCancelled = isCancelled,
                FailedTaskName = failedTaskName ?? string.Empty,
                FailureResult = failureResult
            };
        }
    }

    /// <summary>
    /// 启动流程构建器。
    /// </summary>
    public sealed class StartupPipelineBuilder
    {
        private readonly List<IStartupTask> _tasks = new List<IStartupTask>();
        private IStartupView _view;

        public StartupPipelineBuilder AddTask(IStartupTask task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (task.Weight < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(task), "任务权重不能小于 0");
            }

            _tasks.Add(task);
            return this;
        }

        public StartupPipelineBuilder WithView(IStartupView view)
        {
            _view = view;
            return this;
        }

        public StartupPipeline Build()
        {
            return new StartupPipeline(_tasks, _view);
        }
    }

    /// <summary>
    /// 启动流程执行器。
    /// </summary>
    public sealed class StartupPipeline
    {
        private readonly IReadOnlyList<IStartupTask> _tasks;
        private readonly IStartupView _view;

        public StartupPipeline(IReadOnlyList<IStartupTask> tasks, IStartupView view)
        {
            if (tasks == null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            _tasks = new List<IStartupTask>(tasks);
            _view = view;
        }

        public async Task<StartupPipelineResult> RunAsync(CancellationToken cancellationToken)
        {
            if (_tasks.Count == 0)
            {
                _view?.HideLoading();
                return StartupPipelineResult.Succeeded();
            }

            var totalWeight = 0f;
            for (var i = 0; i < _tasks.Count; i++)
            {
                totalWeight += _tasks[i].Weight;
            }

            if (totalWeight <= 0f)
            {
                totalWeight = 1f;
            }

            var currentWeight = 0f;

            for (var i = 0; i < _tasks.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var task = _tasks[i];
                task.InjectView(_view);
                _view?.ShowLoading($"正在执行: {task.TaskName}", currentWeight / totalWeight);

                var taskResult = await ExecuteTaskWithRetryAsync(task, cancellationToken);
                if (taskResult.IsSuccess)
                {
                    currentWeight += task.Weight;
                    continue;
                }

                if (!taskResult.IsFatal && task.FailurePolicy == StartupTaskFailurePolicy.ContinuePipeline)
                {
                    currentWeight += task.Weight;
                    continue;
                }

                if (_view != null)
                {
                    var shouldRetry = await _view.ShowErrorDialogAsync(CreateErrorMessage(task.TaskName, taskResult), cancellationToken);
                    if (shouldRetry)
                    {
                        var manualRetryResult = await ExecuteTaskWithRetryAsync(task, cancellationToken);
                        if (manualRetryResult.IsSuccess)
                        {
                            currentWeight += task.Weight;
                            continue;
                        }

                        if (!manualRetryResult.IsFatal && task.FailurePolicy == StartupTaskFailurePolicy.ContinuePipeline)
                        {
                            currentWeight += task.Weight;
                            continue;
                        }

                        _view.HideLoading();
                        return StartupPipelineResult.Failed(task.TaskName, manualRetryResult, false);
                    }

                    _view.HideLoading();
                    return StartupPipelineResult.Failed(task.TaskName, taskResult, true);
                }

                _view?.HideLoading();
                return StartupPipelineResult.Failed(task.TaskName, taskResult, false);
            }

            _view?.HideLoading();
            return StartupPipelineResult.Succeeded();
        }

        private static async Task<StartupTaskResult> ExecuteTaskWithRetryAsync(IStartupTask task, CancellationToken cancellationToken)
        {
            var executionOptions = task.ExecutionOptions;
            if (executionOptions.MaxRetryCount < 0)
            {
                executionOptions = StartupTaskExecutionOptions.Default;
            }

            StartupTaskResult lastResult = StartupTaskResult.Failed("Unknown", "任务执行失败", true);
            var maxAttempts = executionOptions.MaxRetryCount + 1;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lastResult = await ExecuteSingleAttemptAsync(task, executionOptions, cancellationToken);
                if (lastResult.IsSuccess)
                {
                    return lastResult;
                }
            }

            return lastResult;
        }

        private static async Task<StartupTaskResult> ExecuteSingleAttemptAsync(IStartupTask task, StartupTaskExecutionOptions options, CancellationToken cancellationToken)
        {
            CancellationTokenSource timeoutTokenSource = null;
            CancellationTokenSource linkedTokenSource = null;

            try
            {
                var executeToken = cancellationToken;
                if (options.HasTimeout)
                {
                    timeoutTokenSource = new CancellationTokenSource(options.TimeoutDuration);
                    linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                    executeToken = linkedTokenSource.Token;
                }

                var result = await task.ExecuteAsync(executeToken);
                return NormalizeResult(result);
            }
            catch (OperationCanceledException ex) when (timeoutTokenSource != null && timeoutTokenSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                return StartupTaskResult.Failed("Timeout", $"任务超时: {task.TaskName}", true, ex);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return StartupTaskResult.Failed("Exception", ex.Message, true, ex);
            }
            finally
            {
                linkedTokenSource?.Dispose();
                timeoutTokenSource?.Dispose();
            }
        }

        private static StartupTaskResult NormalizeResult(StartupTaskResult result)
        {
            if (result.IsSuccess)
            {
                return result;
            }

            if (string.IsNullOrEmpty(result.ErrorCode) && string.IsNullOrEmpty(result.ErrorMessage) && result.Exception == null)
            {
                return StartupTaskResult.Failed("Unknown", "任务返回失败但未提供错误信息", true);
            }

            return result;
        }

        private static string CreateErrorMessage(string taskName, StartupTaskResult result)
        {
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                return result.ErrorMessage;
            }

            return $"任务 {taskName} 执行失败";
        }
    }

    /// <summary>
    /// 启动任务注册中心。
    /// </summary>
    public sealed class StartupTaskRegistry
    {
        private readonly Dictionary<StartupTaskKey, Func<IStartupTask>> _factories = new Dictionary<StartupTaskKey, Func<IStartupTask>>();

        public void Register(StartupTaskKey key, Func<IStartupTask> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _factories[key] = factory;
        }

        public IStartupTask Resolve(StartupTaskKey key)
        {
            if (!_factories.TryGetValue(key, out var factory))
            {
                throw new KeyNotFoundException($"未注册启动任务: {key}");
            }

            var task = factory.Invoke();
            if (task == null)
            {
                throw new InvalidOperationException($"任务工厂返回 null: {key}");
            }

            return task;
        }
    }

    /// <summary>
    /// 启动任务安装器（纯 C#）。
    /// </summary>
    public interface IStartupTaskInstaller
    {
        void Install(StartupTaskRegistry registry);
    }

    /// <summary>
    /// 启动任务注册安装器（兼容旧接口）。
    /// </summary>
    public interface IStartupTaskRegistryInstaller : IStartupTaskInstaller
    {
    }

    /// <summary>
    /// 启动配置提供器。
    /// </summary>
    public interface IStartupProfileProvider
    {
        StartupProfile GetProfile(BootEnvironment environment);
    }

    /// <summary>
    /// 启动配置。
    /// </summary>
    public sealed class StartupProfile
    {
        public StartupProfile(string name, IReadOnlyList<StartupTaskKey> taskKeys)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            TaskKeys = taskKeys ?? throw new ArgumentNullException(nameof(taskKeys));
        }

        public string Name { get; }

        public IReadOnlyList<StartupTaskKey> TaskKeys { get; }

        public static StartupProfile Create(BootEnvironment environment)
        {
            return CodeStartupProfileProvider.Default.GetProfile(environment);
        }
    }

    /// <summary>
    /// 代码化启动配置提供器。
    /// </summary>
    public sealed class CodeStartupProfileProvider : IStartupProfileProvider
    {
        public static readonly CodeStartupProfileProvider Default = new CodeStartupProfileProvider();

        public StartupProfile GetProfile(BootEnvironment environment)
        {
            switch (environment)
            {
                case BootEnvironment.Release:
                case BootEnvironment.DevFull:
                    return new StartupProfile(
                        environment.ToString(),
                        new[]
                        {
                            StartupTaskKey.InitLogger,
                            StartupTaskKey.LoadLocalConfig,
                            StartupTaskKey.CheckUpdate,
                            StartupTaskKey.SdkInit,
                            StartupTaskKey.NetworkConnect,
                            StartupTaskKey.EnterLobby
                        });
                case BootEnvironment.DevSkipToBattle:
                    return new StartupProfile(
                        environment.ToString(),
                        new[]
                        {
                            StartupTaskKey.InitLogger,
                            StartupTaskKey.LoadLocalConfig,
                            StartupTaskKey.MockLogin,
                            StartupTaskKey.LoadTestBattleScene
                        });
                default:
                    throw new ArgumentOutOfRangeException(nameof(environment), environment, "未知启动环境");
            }
        }
    }

    /// <summary>
    /// 启动配置构建器（纯 C# 代码配置）。
    /// </summary>
    public sealed class StartupProfileBuilder
    {
        private readonly string _name;
        private readonly List<StartupTaskKey> _taskKeys = new List<StartupTaskKey>();

        public StartupProfileBuilder(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("profile 名称不能为空", nameof(name));
            }

            _name = name;
        }

        public StartupProfileBuilder Add(StartupTaskKey taskKey)
        {
            _taskKeys.Add(taskKey);
            return this;
        }

        public StartupProfileBuilder AddIf(bool condition, StartupTaskKey taskKey)
        {
            if (condition)
            {
                _taskKeys.Add(taskKey);
            }

            return this;
        }

        public StartupProfileBuilder AddRange(IEnumerable<StartupTaskKey> taskKeys)
        {
            if (taskKeys == null)
            {
                throw new ArgumentNullException(nameof(taskKeys));
            }

            foreach (var taskKey in taskKeys)
            {
                _taskKeys.Add(taskKey);
            }

            return this;
        }

        public StartupProfile Build()
        {
            return new StartupProfile(_name, new List<StartupTaskKey>(_taskKeys));
        }
    }

    /// <summary>
    /// 默认启动任务安装器（纯 C#）。
    /// </summary>
    public sealed class DefaultStartupTaskInstaller : IStartupTaskInstaller
    {
        public void Install(StartupTaskRegistry registry)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            registry.Register(StartupTaskKey.InitLogger, () => CreateTask(StartupTaskKey.InitLogger, 1f));
            registry.Register(StartupTaskKey.LoadLocalConfig, () => CreateTask(StartupTaskKey.LoadLocalConfig, 1f));
            registry.Register(StartupTaskKey.CheckUpdate, () => CreateTask(StartupTaskKey.CheckUpdate, 1f));
            registry.Register(StartupTaskKey.SdkInit, () => CreateTask(StartupTaskKey.SdkInit, 1f));
            registry.Register(StartupTaskKey.NetworkConnect, () => CreateTask(StartupTaskKey.NetworkConnect, 1f));
            registry.Register(StartupTaskKey.MockLogin, () => CreateTask(StartupTaskKey.MockLogin, 0.5f));
            registry.Register(StartupTaskKey.LoadTestBattleScene, () => CreateTask(StartupTaskKey.LoadTestBattleScene, 0.5f));
            registry.Register(StartupTaskKey.EnterLobby, () => CreateTask(StartupTaskKey.EnterLobby, 1f));
        }

        private static IStartupTask CreateTask(StartupTaskKey taskKey, float weight)
        {
            return new DelegateStartupTask(
                taskKey.ToString(),
                weight,
                (_, _) => Task.FromResult(StartupTaskResult.Success()));
        }
    }

    /// <summary>
    /// 启动任务执行约束。
    /// </summary>
    public static class StartupTaskConstraints
    {
        private static readonly HashSet<StartupTaskKey> HeadlessBlockedTaskKeys = new HashSet<StartupTaskKey>
        {
            StartupTaskKey.LoadTestBattleScene,
            StartupTaskKey.EnterLobby
        };

        public static bool RequiresPlayMode(StartupTaskKey taskKey)
        {
            return HeadlessBlockedTaskKeys.Contains(taskKey);
        }

        public static IReadOnlyList<StartupTaskKey> GetHeadlessBlockedTasks(StartupProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var blocked = new List<StartupTaskKey>();
            for (var i = 0; i < profile.TaskKeys.Count; i++)
            {
                var taskKey = profile.TaskKeys[i];
                if (HeadlessBlockedTaskKeys.Contains(taskKey))
                {
                    blocked.Add(taskKey);
                }
            }

            return blocked;
        }
    }

    /// <summary>
    /// 启动编排状态。
    /// </summary>
    public enum StartupOrchestratorState
    {
        Idle = 0,
        Running = 1,
        Stopping = 2,
        Stopped = 3
    }

    /// <summary>
    /// 启动编排器接口。
    /// </summary>
    public interface IStartupOrchestrator
    {
        StartupOrchestratorState State { get; }

        Task<StartupPipelineResult> RunAsync(BootEnvironment environment, CancellationToken cancellationToken);

        Task<StartupPipelineResult> RunAsync(StartupProfile profile, CancellationToken cancellationToken);

        Task ShutdownAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// 默认启动编排器。
    /// </summary>
    public sealed class StartupOrchestrator : IStartupOrchestrator, IDisposable
    {
        private readonly StartupTaskRegistry _registry;
        private readonly IStartupTaskInstaller _installer;
        private readonly IStartupProfileProvider _profileProvider;
        private readonly IStartupView _view;
        private readonly SemaphoreSlim _stateLock = new SemaphoreSlim(1, 1);

        private CancellationTokenSource _runningTokenSource;
        private Task<StartupPipelineResult> _runningTask;
        private bool _installed;

        public StartupOrchestrator(
            StartupTaskRegistry registry,
            IStartupTaskInstaller installer = null,
            IStartupProfileProvider profileProvider = null,
            IStartupView view = null)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _installer = installer;
            _profileProvider = profileProvider ?? CodeStartupProfileProvider.Default;
            _view = view ?? NullStartupView.Instance;
            State = StartupOrchestratorState.Idle;
        }

        public StartupOrchestratorState State { get; private set; }

        public Task<StartupPipelineResult> RunAsync(BootEnvironment environment, CancellationToken cancellationToken)
        {
            var profile = _profileProvider.GetProfile(environment);
            return RunAsync(profile, cancellationToken);
        }

        public async Task<StartupPipelineResult> RunAsync(StartupProfile profile, CancellationToken cancellationToken)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            Task<StartupPipelineResult> runningTask;
            await _stateLock.WaitAsync(cancellationToken);
            try
            {
                if (State == StartupOrchestratorState.Running || State == StartupOrchestratorState.Stopping)
                {
                    throw new InvalidOperationException("启动流程正在执行中，不能重复启动。");
                }

                EnsureInstalled();
                var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _runningTokenSource = linkedTokenSource;

                var pipeline = StartupPipelineFactory.Create(profile, _registry, _view);
                _runningTask = ExecutePipelineAsync(pipeline, linkedTokenSource.Token);
                runningTask = _runningTask;
                State = StartupOrchestratorState.Running;
            }
            finally
            {
                _stateLock.Release();
            }

            try
            {
                return await runningTask;
            }
            finally
            {
                await FinalizeRunAsync(runningTask);
            }
        }

        public async Task ShutdownAsync(CancellationToken cancellationToken)
        {
            Task<StartupPipelineResult> runningTask;

            await _stateLock.WaitAsync(cancellationToken);
            try
            {
                if (_runningTask == null)
                {
                    State = StartupOrchestratorState.Stopped;
                    return;
                }

                State = StartupOrchestratorState.Stopping;
                _runningTokenSource?.Cancel();
                runningTask = _runningTask;
            }
            finally
            {
                _stateLock.Release();
            }

            try
            {
                await runningTask;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await _stateLock.WaitAsync(cancellationToken);
                try
                {
                    if (ReferenceEquals(_runningTask, runningTask))
                    {
                        _runningTask = null;
                        _runningTokenSource?.Dispose();
                        _runningTokenSource = null;
                    }

                    State = StartupOrchestratorState.Stopped;
                }
                finally
                {
                    _stateLock.Release();
                }
            }
        }

        public void Dispose()
        {
            _runningTokenSource?.Dispose();
            _stateLock.Dispose();
        }

        private void EnsureInstalled()
        {
            if (_installed)
            {
                return;
            }

            _installer?.Install(_registry);
            _installed = true;
        }

        private async Task FinalizeRunAsync(Task<StartupPipelineResult> completedTask)
        {
            await _stateLock.WaitAsync();
            try
            {
                if (!ReferenceEquals(_runningTask, completedTask))
                {
                    return;
                }

                _runningTask = null;
                _runningTokenSource?.Dispose();
                _runningTokenSource = null;
                State = StartupOrchestratorState.Stopped;
            }
            finally
            {
                _stateLock.Release();
            }
        }

        private static async Task<StartupPipelineResult> ExecutePipelineAsync(StartupPipeline pipeline, CancellationToken cancellationToken)
        {
            try
            {
                return await pipeline.RunAsync(cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                return StartupPipelineResult.Failed(
                    string.Empty,
                    StartupTaskResult.Failed("Cancelled", "启动流程已取消", true, ex),
                    true);
            }
        }
    }

    /// <summary>
    /// 启动编排共享入口。
    /// </summary>
    public static class StartupOrchestratorHost
    {
        private static readonly object SyncRoot = new object();

        private static StartupTaskRegistry _registry = new StartupTaskRegistry();
        private static IStartupTaskInstaller _installer = new DefaultStartupTaskInstaller();
        private static IStartupProfileProvider _profileProvider = CodeStartupProfileProvider.Default;
        private static IStartupView _view = NullStartupView.Instance;
        private static IStartupOrchestrator _orchestrator;

        public static void Configure(
            IStartupTaskInstaller installer = null,
            IStartupProfileProvider profileProvider = null,
            IStartupView view = null,
            StartupTaskRegistry registry = null)
        {
            lock (SyncRoot)
            {
                _registry = registry ?? new StartupTaskRegistry();
                _installer = installer ?? new DefaultStartupTaskInstaller();
                _profileProvider = profileProvider ?? CodeStartupProfileProvider.Default;
                _view = view ?? NullStartupView.Instance;

                if (_orchestrator is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _orchestrator = null;
            }
        }

        public static IStartupOrchestrator GetOrCreate()
        {
            lock (SyncRoot)
            {
                if (_orchestrator == null)
                {
                    _orchestrator = new StartupOrchestrator(_registry, _installer, _profileProvider, _view);
                }

                return _orchestrator;
            }
        }
    }

    /// <summary>
    /// 启动流程工厂。
    /// </summary>
    public static class StartupPipelineFactory
    {
        public static StartupPipeline Create(StartupProfile profile, StartupTaskRegistry registry, IStartupView view = null)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            var builder = new StartupPipelineBuilder().WithView(view);

            for (var i = 0; i < profile.TaskKeys.Count; i++)
            {
                var key = profile.TaskKeys[i];
                builder.AddTask(registry.Resolve(key));
            }

            return builder.Build();
        }

        public static StartupPipeline Create(
            BootEnvironment environment,
            StartupTaskRegistry registry,
            IStartupView view = null,
            IStartupProfileProvider profileProvider = null)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            var provider = profileProvider ?? CodeStartupProfileProvider.Default;
            var profile = provider.GetProfile(environment);
            return Create(profile, registry, view);
        }
    }

    /// <summary>
    /// 启动流程启动器。
    /// </summary>
    public sealed class StartupPipelineLauncher
    {
        private readonly StartupTaskRegistry _registry;

        public StartupPipelineLauncher(StartupTaskRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public StartupPipeline Create(BootEnvironment environment, IStartupTaskRegistryInstaller installer, IStartupView view)
        {
            return Create(environment, installer as IStartupTaskInstaller, view, null);
        }

        public StartupPipeline Create(
            BootEnvironment environment,
            IStartupTaskInstaller installer,
            IStartupView view,
            IStartupProfileProvider profileProvider = null)
        {
            installer?.Install(_registry);
            return StartupPipelineFactory.Create(environment, _registry, view, profileProvider);
        }

        public StartupPipeline Create(StartupProfile profile, IStartupTaskRegistryInstaller installer, IStartupView view)
        {
            return Create(profile, installer as IStartupTaskInstaller, view);
        }

        public StartupPipeline Create(StartupProfile profile, IStartupTaskInstaller installer, IStartupView view)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            installer?.Install(_registry);
            return StartupPipelineFactory.Create(profile, _registry, view);
        }
    }
}
