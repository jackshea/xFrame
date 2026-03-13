using System.Collections.Generic;
using System.Threading;
using GenericEventBus;
using NUnit.Framework;
using xFrame.Runtime.EventBus;
using xFrame.Runtime.Startup;

namespace xFrame.Tests
{
    /// <summary>
    ///     启动运行时生命周期回归测试。
    /// </summary>
    [TestFixture]
    public class StartupRuntimeLifecycleTests
    {
        [Test]
        public void RunAsync_OnSuccess_ShouldPublishStartingAndSucceeded()
        {
            var lifecycleStore = new StartupLifecycleStateStore();
            var handler = new RecordingLifecycleHandler();
            var compositionRoot = new TestCompositionRoot(lifecycleStore, handler);
            var profileProvider = new TestProfileProvider(StartupTaskKey.EnterLobby);
            var installer = new DelegateInstaller(registry =>
            {
                registry.Register(
                    StartupTaskKey.EnterLobby,
                    () => new DelegateStartupTask(
                        "EnterLobby",
                        1f,
                        (_, _) => System.Threading.Tasks.Task.FromResult(StartupTaskResult.Success())));
            });

            var runtime = new StartupRuntime(compositionRoot, installer, profileProvider);
            var snapshots = new List<StartupLifecycleSnapshot>();
            lifecycleStore.Changed += snapshots.Add;

            var result = runtime.RunAsync(BootEnvironment.DevFull, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2, snapshots.Count);
            Assert.AreEqual(StartupLifecycleStage.Starting, snapshots[0].Stage);
            Assert.AreEqual(StartupLifecycleStage.Succeeded, snapshots[1].Stage);
            Assert.AreEqual(BootEnvironment.DevFull, snapshots[1].Environment);
            Assert.AreEqual(StartupLifecycleStage.Succeeded, lifecycleStore.Current.Stage);
            Assert.AreEqual(2, handler.ReceivedSnapshots.Count);
            Assert.AreEqual(StartupLifecycleStage.Starting, handler.ReceivedSnapshots[0].Stage);
            Assert.AreEqual(StartupLifecycleStage.Succeeded, handler.ReceivedSnapshots[1].Stage);
        }

        [Test]
        public void RunAsync_OnFailureThenShutdown_ShouldPublishFailedAndStopped()
        {
            var lifecycleStore = new StartupLifecycleStateStore();
            var handler = new RecordingLifecycleHandler();
            var compositionRoot = new TestCompositionRoot(lifecycleStore, handler);
            var profileProvider = new TestProfileProvider(StartupTaskKey.NetworkConnect);
            var installer = new DelegateInstaller(registry =>
            {
                registry.Register(
                    StartupTaskKey.NetworkConnect,
                    () => new DelegateStartupTask(
                        "NetworkConnect",
                        1f,
                        (_, _) => System.Threading.Tasks.Task.FromResult(
                            StartupTaskResult.Failed("ConnectFailed", "网络连接失败", true))));
            });

            var runtime = new StartupRuntime(compositionRoot, installer, profileProvider);
            var snapshots = new List<StartupLifecycleSnapshot>();
            lifecycleStore.Changed += snapshots.Add;

            var result = runtime.RunAsync(BootEnvironment.Release, CancellationToken.None).GetAwaiter().GetResult();
            runtime.ShutdownAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("NetworkConnect", result.FailedTaskName);
            Assert.AreEqual(3, snapshots.Count);
            Assert.AreEqual(StartupLifecycleStage.Starting, snapshots[0].Stage);
            Assert.AreEqual(StartupLifecycleStage.Failed, snapshots[1].Stage);
            Assert.AreEqual(StartupLifecycleStage.Stopped, snapshots[2].Stage);
            Assert.AreEqual(BootEnvironment.Release, snapshots[1].Environment);
            Assert.AreEqual(BootEnvironment.Release, snapshots[2].Environment);
            Assert.AreEqual(StartupLifecycleStage.Stopped, lifecycleStore.Current.Stage);
            Assert.AreEqual(3, handler.ReceivedSnapshots.Count);
            Assert.AreEqual(StartupLifecycleStage.Failed, handler.ReceivedSnapshots[1].Stage);
            Assert.AreEqual(StartupLifecycleStage.Stopped, handler.ReceivedSnapshots[2].Stage);
        }

        [Test]
        public void RunAsync_WithEventBusBridge_ShouldRaiseLifecycleEvents()
        {
            xFrameEventBus.ClearListeners<StartupStartedEvent>();
            xFrameEventBus.ClearListeners<StartupSucceededEvent>();

            var startedCount = 0;
            var succeededCount = 0;
            GenericEventBus<IEvent>.EventHandler<StartupStartedEvent> startedHandler =
                (ref StartupStartedEvent e) => startedCount++;
            GenericEventBus<IEvent>.EventHandler<StartupSucceededEvent> succeededHandler =
                (ref StartupSucceededEvent e) => succeededCount++;

            xFrameEventBus.SubscribeTo(startedHandler);
            xFrameEventBus.SubscribeTo(succeededHandler);

            try
            {
                var lifecycleStore = new StartupLifecycleStateStore();
                var bridge = new StartupLifecycleEventBusBridge();
                var compositionRoot = new TestCompositionRoot(lifecycleStore, bridge);
                var profileProvider = new TestProfileProvider(StartupTaskKey.EnterLobby);
                var installer = new DelegateInstaller(registry =>
                {
                    registry.Register(
                        StartupTaskKey.EnterLobby,
                        () => new DelegateStartupTask(
                            "EnterLobby",
                            1f,
                            (_, _) => System.Threading.Tasks.Task.FromResult(StartupTaskResult.Success())));
                });

                var runtime = new StartupRuntime(compositionRoot, installer, profileProvider);
                var result = runtime.RunAsync(BootEnvironment.DevFull, CancellationToken.None).GetAwaiter().GetResult();

                Assert.IsTrue(result.IsSuccess);
                Assert.AreEqual(1, startedCount);
                Assert.AreEqual(1, succeededCount);
            }
            finally
            {
                xFrameEventBus.UnsubscribeFrom(startedHandler);
                xFrameEventBus.UnsubscribeFrom(succeededHandler);
                xFrameEventBus.ClearListeners<StartupStartedEvent>();
                xFrameEventBus.ClearListeners<StartupSucceededEvent>();
            }
        }

        private sealed class DelegateInstaller : IStartupTaskInstaller
        {
            private readonly System.Action<StartupTaskRegistry> _install;

            public DelegateInstaller(System.Action<StartupTaskRegistry> install)
            {
                _install = install;
            }

            public void Install(StartupTaskRegistry registry)
            {
                _install.Invoke(registry);
            }
        }

        private sealed class TestProfileProvider : IStartupProfileProvider
        {
            private readonly StartupTaskKey _taskKey;

            public TestProfileProvider(StartupTaskKey taskKey)
            {
                _taskKey = taskKey;
            }

            public StartupProfile GetProfile(BootEnvironment environment)
            {
                return new StartupProfile("TestProfile", new[] { _taskKey });
            }
        }

        private sealed class TestCompositionRoot : IStartupCompositionRoot
        {
            private readonly IStartupLifecycleHandler[] _handlers;
            private readonly StartupLifecycleStateStore _lifecycleStore;

            public TestCompositionRoot(
                StartupLifecycleStateStore lifecycleStore,
                params IStartupLifecycleHandler[] handlers)
            {
                _lifecycleStore = lifecycleStore;
                _handlers = handlers ?? System.Array.Empty<IStartupLifecycleHandler>();
            }

            public void EnsureInitialized()
            {
            }

            public T Resolve<T>() where T : class
            {
                return TryResolve<T>(out var service) ? service : null;
            }

            public bool TryResolve<T>(out T service) where T : class
            {
                if (typeof(T) == typeof(IEnumerable<IStartupLifecycleHandler>))
                {
                    service = _handlers as T;
                    return service != null;
                }

                service = _lifecycleStore as T;
                return service != null;
            }
        }

        private sealed class RecordingLifecycleHandler : IStartupLifecycleHandler
        {
            public List<StartupLifecycleSnapshot> ReceivedSnapshots { get; } = new();

            public System.Threading.Tasks.Task HandleAsync(
                StartupLifecycleSnapshot snapshot,
                StartupTaskContext context,
                CancellationToken cancellationToken)
            {
                ReceivedSnapshots.Add(snapshot);
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }
    }
}
