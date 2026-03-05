using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using xFrame.Runtime.Startup;

namespace xFrame.Tests.EditMode
{
    [TestFixture]
    public class StartupPipelineTests
    {
        [Test]
        public async Task RunAsync_AllTasksSuccess_ShouldReportWeightedProgress()
        {
            var view = new RecordingStartupView();
            var executionOrder = new List<string>();

            var pipeline = new StartupPipelineBuilder()
                .WithView(view)
                .AddTask(new FakeStartupTask("Task-A", 1f, _ =>
                {
                    executionOrder.Add("Task-A");
                    return Task.FromResult(StartupTaskResult.Success());
                }))
                .AddTask(new FakeStartupTask("Task-B", 3f, _ =>
                {
                    executionOrder.Add("Task-B");
                    return Task.FromResult(StartupTaskResult.Success());
                }))
                .Build();

            var result = await pipeline.RunAsync(CancellationToken.None);

            Assert.IsTrue(result.IsSuccess);
            CollectionAssert.AreEqual(new[] { "Task-A", "Task-B" }, executionOrder);
            Assert.AreEqual(2, view.LoadingSnapshots.Count);
            Assert.AreEqual(0f, view.LoadingSnapshots[0].Progress);
            Assert.AreEqual(0.25f, view.LoadingSnapshots[1].Progress);
            Assert.IsTrue(view.HideLoadingCalled);
        }

        [Test]
        public async Task RunAsync_TaskRetrySucceeded_ShouldContinuePipeline()
        {
            var attempts = 0;

            var retryTask = new FakeStartupTask(
                "Retry-Task",
                1f,
                _ =>
                {
                    attempts++;
                    if (attempts < 2)
                    {
                        throw new InvalidOperationException("transient");
                    }

                    return Task.FromResult(StartupTaskResult.Success());
                },
                executionOptions: new StartupTaskExecutionOptions(maxRetryCount: 1));

            var pipeline = new StartupPipelineBuilder()
                .AddTask(retryTask)
                .Build();

            var result = await pipeline.RunAsync(CancellationToken.None);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2, attempts);
        }

        [Test]
        public async Task RunAsync_FatalTaskFailedAndUserCancelled_ShouldStopPipeline()
        {
            var view = new RecordingStartupView
            {
                ErrorDialogResult = false
            };

            var task3Executed = false;

            var pipeline = new StartupPipelineBuilder()
                .WithView(view)
                .AddTask(new FakeStartupTask("Task-1", 1f, _ => Task.FromResult(StartupTaskResult.Success())))
                .AddTask(new FakeStartupTask("Task-2", 1f, _ => Task.FromResult(StartupTaskResult.Failed("Network", "failed", isFatal: true))))
                .AddTask(new FakeStartupTask("Task-3", 1f, _ =>
                {
                    task3Executed = true;
                    return Task.FromResult(StartupTaskResult.Success());
                }))
                .Build();

            var result = await pipeline.RunAsync(CancellationToken.None);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.IsCancelled);
            Assert.AreEqual("Task-2", result.FailedTaskName);
            Assert.IsFalse(task3Executed);
            Assert.AreEqual(1, view.ErrorDialogShownCount);
        }

        [Test]
        public async Task RunAsync_NonFatalFailedWithContinuePolicy_ShouldIgnoreAndContinue()
        {
            var view = new RecordingStartupView();
            var task3Executed = false;

            var pipeline = new StartupPipelineBuilder()
                .WithView(view)
                .AddTask(new FakeStartupTask("Task-1", 1f, _ => Task.FromResult(StartupTaskResult.Success())))
                .AddTask(new FakeStartupTask(
                    "Task-2",
                    1f,
                    _ => Task.FromResult(StartupTaskResult.Failed("SdkInit", "optional sdk failed", isFatal: false)),
                    failurePolicy: StartupTaskFailurePolicy.ContinuePipeline))
                .AddTask(new FakeStartupTask("Task-3", 1f, _ =>
                {
                    task3Executed = true;
                    return Task.FromResult(StartupTaskResult.Success());
                }))
                .Build();

            var result = await pipeline.RunAsync(CancellationToken.None);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(task3Executed);
            Assert.AreEqual(0, view.ErrorDialogShownCount);
        }

        [Test]
        public async Task PipelineFactory_DevSkipToBattle_ShouldAssembleExpectedTasks()
        {
            var executionOrder = new List<StartupTaskKey>();

            var registry = new StartupTaskRegistry();
            registry.Register(StartupTaskKey.InitLogger, CreateTask(StartupTaskKey.InitLogger, executionOrder));
            registry.Register(StartupTaskKey.LoadLocalConfig, CreateTask(StartupTaskKey.LoadLocalConfig, executionOrder));
            registry.Register(StartupTaskKey.CheckUpdate, CreateTask(StartupTaskKey.CheckUpdate, executionOrder));
            registry.Register(StartupTaskKey.SdkInit, CreateTask(StartupTaskKey.SdkInit, executionOrder));
            registry.Register(StartupTaskKey.NetworkConnect, CreateTask(StartupTaskKey.NetworkConnect, executionOrder));
            registry.Register(StartupTaskKey.MockLogin, CreateTask(StartupTaskKey.MockLogin, executionOrder));
            registry.Register(StartupTaskKey.LoadTestBattleScene, CreateTask(StartupTaskKey.LoadTestBattleScene, executionOrder));
            registry.Register(StartupTaskKey.EnterLobby, CreateTask(StartupTaskKey.EnterLobby, executionOrder));

            var pipeline = StartupPipelineFactory.Create(BootEnvironment.DevSkipToBattle, registry);
            var result = await pipeline.RunAsync(CancellationToken.None);

            Assert.IsTrue(result.IsSuccess);
            CollectionAssert.AreEqual(
                new[]
                {
                    StartupTaskKey.InitLogger,
                    StartupTaskKey.LoadLocalConfig,
                    StartupTaskKey.MockLogin,
                    StartupTaskKey.LoadTestBattleScene
                },
                executionOrder);
        }

        [Test]
        public async Task PipelineFactory_CustomProfile_ShouldUseCodeConfiguredTaskOrder()
        {
            var executionOrder = new List<StartupTaskKey>();

            var registry = new StartupTaskRegistry();
            registry.Register(StartupTaskKey.InitLogger, CreateTask(StartupTaskKey.InitLogger, executionOrder));
            registry.Register(StartupTaskKey.LoadLocalConfig, CreateTask(StartupTaskKey.LoadLocalConfig, executionOrder));
            registry.Register(StartupTaskKey.MockLogin, CreateTask(StartupTaskKey.MockLogin, executionOrder));

            var profile = new StartupProfileBuilder("CodeConfigured")
                .Add(StartupTaskKey.InitLogger)
                .Add(StartupTaskKey.LoadLocalConfig)
                .AddIf(false, StartupTaskKey.EnterLobby)
                .Add(StartupTaskKey.MockLogin)
                .Build();

            var pipeline = StartupPipelineFactory.Create(profile, registry);
            var result = await pipeline.RunAsync(CancellationToken.None);

            Assert.IsTrue(result.IsSuccess);
            CollectionAssert.AreEqual(
                new[]
                {
                    StartupTaskKey.InitLogger,
                    StartupTaskKey.LoadLocalConfig,
                    StartupTaskKey.MockLogin
                },
                executionOrder);
        }

        [Test]
        public async Task PipelineLauncher_WithInstaller_ShouldInstallAndRun()
        {
            var executionOrder = new List<StartupTaskKey>();
            var registry = new StartupTaskRegistry();
            var installer = new FakeInstaller(executionOrder);

            var launcher = new StartupPipelineLauncher(registry);
            var pipeline = launcher.Create(BootEnvironment.DevSkipToBattle, installer, null);
            var result = await pipeline.RunAsync(CancellationToken.None);

            Assert.IsTrue(result.IsSuccess);
            CollectionAssert.AreEqual(
                new[]
                {
                    StartupTaskKey.InitLogger,
                    StartupTaskKey.LoadLocalConfig,
                    StartupTaskKey.MockLogin,
                    StartupTaskKey.LoadTestBattleScene
                },
                executionOrder);
        }

        [Test]
        public async Task DelegateStartupTask_ShouldUseConfiguredCallback()
        {
            var injected = false;
            var executed = false;

            var task = new DelegateStartupTask(
                "DelegateTask",
                2f,
                (view, token) =>
                {
                    injected = view != null;
                    executed = true;
                    return Task.FromResult(StartupTaskResult.Success());
                });

            var pipeline = new StartupPipelineBuilder()
                .WithView(new RecordingStartupView())
                .AddTask(task)
                .Build();

            var result = await pipeline.RunAsync(CancellationToken.None);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(injected);
            Assert.IsTrue(executed);
        }

        private static Func<IStartupTask> CreateTask(StartupTaskKey key, List<StartupTaskKey> executionOrder)
        {
            return () => new FakeStartupTask(key.ToString(), 1f, _ =>
            {
                executionOrder.Add(key);
                return Task.FromResult(StartupTaskResult.Success());
            });
        }

        private sealed class FakeInstaller : IStartupTaskRegistryInstaller
        {
            private readonly List<StartupTaskKey> _executionOrder;

            public FakeInstaller(List<StartupTaskKey> executionOrder)
            {
                _executionOrder = executionOrder;
            }

            public void Install(StartupTaskRegistry registry)
            {
                registry.Register(StartupTaskKey.InitLogger, CreateTask(StartupTaskKey.InitLogger, _executionOrder));
                registry.Register(StartupTaskKey.LoadLocalConfig, CreateTask(StartupTaskKey.LoadLocalConfig, _executionOrder));
                registry.Register(StartupTaskKey.MockLogin, CreateTask(StartupTaskKey.MockLogin, _executionOrder));
                registry.Register(StartupTaskKey.LoadTestBattleScene, CreateTask(StartupTaskKey.LoadTestBattleScene, _executionOrder));
            }
        }

        private sealed class FakeStartupTask : IStartupTask
        {
            private readonly Func<CancellationToken, Task<StartupTaskResult>> _executeFunc;
            private bool _injectCalled;

            public FakeStartupTask(
                string taskName,
                float weight,
                Func<CancellationToken, Task<StartupTaskResult>> executeFunc,
                StartupTaskFailurePolicy failurePolicy = StartupTaskFailurePolicy.StopPipeline,
                StartupTaskExecutionOptions? executionOptions = null)
            {
                TaskName = taskName;
                Weight = weight;
                _executeFunc = executeFunc;
                FailurePolicy = failurePolicy;
                ExecutionOptions = executionOptions ?? StartupTaskExecutionOptions.Default;
            }

            public string TaskName { get; }

            public float Weight { get; }

            public StartupTaskFailurePolicy FailurePolicy { get; }

            public StartupTaskExecutionOptions ExecutionOptions { get; }

            public void InjectView(IStartupView view)
            {
                _injectCalled = true;
            }

            public Task<StartupTaskResult> ExecuteAsync(CancellationToken cancellationToken)
            {
                Assert.IsTrue(_injectCalled);
                return _executeFunc(cancellationToken);
            }
        }

        private sealed class RecordingStartupView : IStartupView
        {
            public bool ErrorDialogResult { get; set; } = true;

            public bool HideLoadingCalled { get; private set; }

            public int ErrorDialogShownCount { get; private set; }

            public List<(string Message, float Progress)> LoadingSnapshots { get; } = new List<(string Message, float Progress)>();

            public void ShowLoading(string message, float progress)
            {
                LoadingSnapshots.Add((message, progress));
            }

            public Task<bool> ShowErrorDialogAsync(string message, CancellationToken cancellationToken)
            {
                ErrorDialogShownCount++;
                return Task.FromResult(ErrorDialogResult);
            }

            public void HideLoading()
            {
                HideLoadingCalled = true;
            }
        }
    }
}
