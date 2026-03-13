using System.Threading;
using NUnit.Framework;
using xFrame.Runtime.Startup;

namespace xFrame.Tests
{
    /// <summary>
    ///     默认启动业务流转处理器回归测试。
    /// </summary>
    [TestFixture]
    public class StartupBusinessFlowLifecycleHandlerTests
    {
        [Test]
        public void HandleAsync_OnSucceededWithoutTerminalFlow_ShouldEnterLobby()
        {
            var handler = new StartupBusinessFlowLifecycleHandler();
            var resolver = new TestResolver();
            var flowService = new FakeFlowService();
            resolver.Register<IStartupFlowService>(flowService);
            var context = new StartupTaskContext(resolver);
            var snapshot = new StartupLifecycleSnapshot(
                StartupLifecycleStage.Succeeded,
                BootEnvironment.DevFull,
                StartupPipelineResult.Succeeded());

            handler.HandleAsync(snapshot, context, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(flowService.EnterLobbyCalled);
            Assert.IsFalse(flowService.LoadBattleCalled);
            Assert.IsTrue(context.TryGetValue(StartupContextKeys.TerminalFlowHandled, out bool handled));
            Assert.IsTrue(handled);
        }

        [Test]
        public void HandleAsync_OnSucceededSkipToBattle_ShouldLoadBattle()
        {
            var handler = new StartupBusinessFlowLifecycleHandler();
            var resolver = new TestResolver();
            var flowService = new FakeFlowService();
            resolver.Register<IStartupFlowService>(flowService);
            var context = new StartupTaskContext(resolver);
            var snapshot = new StartupLifecycleSnapshot(
                StartupLifecycleStage.Succeeded,
                BootEnvironment.DevSkipToBattle,
                StartupPipelineResult.Succeeded());

            handler.HandleAsync(snapshot, context, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsFalse(flowService.EnterLobbyCalled);
            Assert.IsTrue(flowService.LoadBattleCalled);
            Assert.IsTrue(context.TryGetValue(StartupContextKeys.TerminalFlowHandled, out bool handled));
            Assert.IsTrue(handled);
        }

        [Test]
        public void HandleAsync_OnSucceededWithHandledFlow_ShouldNotRunTwice()
        {
            var handler = new StartupBusinessFlowLifecycleHandler();
            var resolver = new TestResolver();
            var flowService = new FakeFlowService();
            resolver.Register<IStartupFlowService>(flowService);
            var context = new StartupTaskContext(resolver);
            context.SetValue(StartupContextKeys.TerminalFlowHandled, true);
            var snapshot = new StartupLifecycleSnapshot(
                StartupLifecycleStage.Succeeded,
                BootEnvironment.DevFull,
                StartupPipelineResult.Succeeded());

            handler.HandleAsync(snapshot, context, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsFalse(flowService.EnterLobbyCalled);
            Assert.IsFalse(flowService.LoadBattleCalled);
        }

        [Test]
        public void HandleAsync_OnFailed_ShouldDelegateToFailureHandler()
        {
            var handler = new StartupBusinessFlowLifecycleHandler();
            var resolver = new TestResolver();
            var failureHandler = new FakeFailureHandler();
            resolver.Register<IStartupFailureHandler>(failureHandler);
            var context = new StartupTaskContext(resolver);
            var snapshot = new StartupLifecycleSnapshot(
                StartupLifecycleStage.Failed,
                BootEnvironment.Release,
                StartupPipelineResult.Failed(
                    "NetworkConnect",
                    StartupTaskResult.Failed("ConnectFailed", "网络连接失败", true),
                    false));

            handler.HandleAsync(snapshot, context, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(failureHandler.Called);
            Assert.AreEqual("NetworkConnect", failureHandler.LastSnapshot.PipelineResult.FailedTaskName);
        }

        private sealed class TestResolver : IStartupServiceResolver
        {
            private readonly System.Collections.Generic.Dictionary<System.Type, object> _services = new();

            public void Register<T>(T service) where T : class
            {
                _services[typeof(T)] = service;
            }

            public T Resolve<T>() where T : class
            {
                return _services.TryGetValue(typeof(T), out var service) ? service as T : null;
            }

            public bool TryResolve<T>(out T service) where T : class
            {
                service = Resolve<T>();
                return service != null;
            }
        }

        private sealed class FakeFlowService : IStartupFlowService
        {
            public bool EnterLobbyCalled { get; private set; }
            public bool LoadBattleCalled { get; private set; }

            public System.Threading.Tasks.Task<StartupTaskResult> EnterLobbyAsync(
                StartupTaskContext context,
                CancellationToken cancellationToken)
            {
                EnterLobbyCalled = true;
                return System.Threading.Tasks.Task.FromResult(StartupTaskResult.Success());
            }

            public System.Threading.Tasks.Task<StartupTaskResult> LoadTestBattleSceneAsync(
                StartupTaskContext context,
                CancellationToken cancellationToken)
            {
                LoadBattleCalled = true;
                return System.Threading.Tasks.Task.FromResult(StartupTaskResult.Success());
            }
        }

        private sealed class FakeFailureHandler : IStartupFailureHandler
        {
            public bool Called { get; private set; }
            public StartupLifecycleSnapshot LastSnapshot { get; private set; }

            public System.Threading.Tasks.Task HandleFailureAsync(
                StartupLifecycleSnapshot snapshot,
                StartupTaskContext context,
                CancellationToken cancellationToken)
            {
                Called = true;
                LastSnapshot = snapshot;
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }
    }
}
