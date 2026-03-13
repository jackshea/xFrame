using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using xFrame.Runtime.Networking;
using xFrame.Runtime.Platform;
using xFrame.Runtime.Startup;

namespace xFrame.Tests
{
    /// <summary>
    ///     启动内置任务回归测试。
    /// </summary>
    [TestFixture]
    public class StartupBuiltinTasksTests
    {
        [Test]
        public void StartupTaskRegistry_ContextFactory_ShouldReceiveRuntimeContext()
        {
            var resolver = new TestResolver();
            var context = new StartupTaskContext(resolver);
            context.SetValue("sample", 7);

            var registry = new StartupTaskRegistry();
            registry.SetContext(context);
            registry.Register(StartupTaskKey.InitLogger, taskContext =>
            {
                Assert.AreSame(context, taskContext);
                Assert.IsTrue(taskContext.TryGetValue("sample", out int sample));
                Assert.AreEqual(7, sample);
                return new DelegateStartupTask("InitLogger", 1f, (_, _) => System.Threading.Tasks.Task.FromResult(StartupTaskResult.Success()));
            });

            var task = registry.Resolve(StartupTaskKey.InitLogger);

            Assert.IsNotNull(task);
            Assert.AreEqual("InitLogger", task.TaskName);
        }

        [Test]
        public void LoadLocalConfigTask_ShouldCapturePlatformPathsIntoContext()
        {
            var resolver = new TestResolver();
            resolver.Register<IPlatformService>(new FakePlatformService());
            var context = new StartupTaskContext(resolver);

            var task = StartupBuiltinTaskFactory.CreateLoadLocalConfigTask(context, 1f);
            var result = task.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(context.TryGetValue(StartupContextKeys.PersistentDataPath, out string persistentDataPath));
            Assert.AreEqual("/persistent", persistentDataPath);
            Assert.IsTrue(context.TryGetValue(StartupContextKeys.StreamingAssetsPath, out string streamingAssetsPath));
            Assert.AreEqual("/streaming", streamingAssetsPath);
            Assert.IsTrue(context.TryGetValue(StartupContextKeys.PlatformInfo, out PlatformInfo platformInfo));
            Assert.AreEqual("Editor", platformInfo.Platform);
        }

        [Test]
        public void NetworkConnectTask_WithoutEndpointProviderAndContinuePolicy_ShouldSkip()
        {
            var resolver = new TestResolver();
            resolver.Register<INetworkClient>(new FakeNetworkClient());
            var context = new StartupTaskContext(resolver);
            context.SetValue(StartupContextKeys.BootEnvironment, BootEnvironment.DevFull);

            var task = StartupBuiltinTaskFactory.CreateNetworkConnectTask(
                context,
                1f,
                StartupTaskFailurePolicy.ContinuePipeline);

            var result = task.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(result.IsFatal);
            Assert.AreEqual("OptionalTaskSkipped", result.ErrorCode);
        }

        [Test]
        public void NetworkConnectTask_WithEndpointProvider_ShouldUseEnvironmentEndpoint()
        {
            var resolver = new TestResolver();
            var networkClient = new FakeNetworkClient();
            resolver.Register<INetworkClient>(networkClient);
            resolver.Register<IStartupNetworkEndpointProvider>(new FakeEndpointProvider());
            var context = new StartupTaskContext(resolver);
            context.SetValue(StartupContextKeys.BootEnvironment, BootEnvironment.DevSkipToBattle);

            var task = StartupBuiltinTaskFactory.CreateNetworkConnectTask(context, 1f);
            var result = task.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(networkClient.IsConnected);
            Assert.AreEqual("wss://startup/dev-skip-to-battle", networkClient.LastEndpoint);
        }

        [Test]
        public void EnterLobbyTask_WithFlowService_ShouldInvokeFlowService()
        {
            var resolver = new TestResolver();
            var flowService = new FakeFlowService();
            resolver.Register<IStartupFlowService>(flowService);
            var context = new StartupTaskContext(resolver);

            var task = StartupBuiltinTaskFactory.CreateEnterLobbyTask(context, 1f);
            var result = task.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(flowService.EnterLobbyCalled);
        }

        private sealed class TestResolver : IStartupServiceResolver
        {
            private readonly Dictionary<System.Type, object> _services = new();

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

        private sealed class FakePlatformService : IPlatformService
        {
            public string PersistentDataPath => "/persistent";
            public string StreamingAssetsPath => "/streaming";
            public bool IsEditor => true;

            public PlatformInfo GetPlatformInfo()
            {
                return new PlatformInfo
                {
                    Platform = "Editor",
                    OperatingSystem = "TestOS",
                    UnityVersion = "2021.3.51f1",
                    DeviceModel = "TestDevice"
                };
            }
        }

        private sealed class FakeNetworkClient : INetworkClient
        {
            public bool IsConnected { get; private set; }
            public string LastEndpoint { get; private set; }

            public Cysharp.Threading.Tasks.UniTask ConnectAsync(string endpoint, CancellationToken ct = default)
            {
                IsConnected = true;
                LastEndpoint = endpoint;
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }

            public Cysharp.Threading.Tasks.UniTask SendAsync(byte[] payload, CancellationToken ct = default)
            {
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }

            public Cysharp.Threading.Tasks.UniTask<byte[]> ReceiveAsync(CancellationToken ct = default)
            {
                return Cysharp.Threading.Tasks.UniTask.FromResult(System.Array.Empty<byte>());
            }

            public Cysharp.Threading.Tasks.UniTask DisconnectAsync()
            {
                IsConnected = false;
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }
        }

        private sealed class FakeEndpointProvider : IStartupNetworkEndpointProvider
        {
            public string GetStartupEndpoint(BootEnvironment environment, StartupTaskContext context)
            {
                return environment == BootEnvironment.DevSkipToBattle
                    ? "wss://startup/dev-skip-to-battle"
                    : "wss://startup/default";
            }
        }

        private sealed class FakeFlowService : IStartupFlowService
        {
            public bool EnterLobbyCalled { get; private set; }

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
                return System.Threading.Tasks.Task.FromResult(StartupTaskResult.Success());
            }
        }
    }
}
