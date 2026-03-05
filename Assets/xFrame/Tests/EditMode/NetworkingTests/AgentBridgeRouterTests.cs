using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using xFrame.Runtime.Networking.AgentBridge;
using xFrame.Runtime.Networking.AgentBridge.Commands;
using xFrame.Runtime.Startup;
using Object = UnityEngine.Object;

namespace xFrame.Tests
{
    [TestFixture]
    public class AgentBridgeRouterTests
    {
        [SetUp]
        public void SetUp()
        {
            _options = new AgentBridgeOptions
            {
                AuthToken = "test-token",
                EnableReflectionBridge = false,
                AllowedAssemblies = new[] { "Assembly-CSharp" },
                AllowedTypePrefixes = new[] { "xFrame" }
            };

            _registry = new AgentCommandRegistry();
            _startupOrchestrator = new TestStartupOrchestrator();
            _registry.Register(new PingCommandHandler());
            _registry.Register(new AuthenticateCommandHandler());
            _registry.Register(new ListCommandsHandler());
            _registry.Register(new FindGameObjectCommandHandler());
            _registry.Register(new InvokeComponentCommandHandler());
            _registry.Register(new StartupRunCommandHandler(_startupOrchestrator, new CodeStartupProfileProvider(),
                () => true));
            _registry.Register(new StartupStopCommandHandler(_startupOrchestrator));
            _registry.Register(new TestEditorExecuteMenuHandler());
            _registry.Register(new TestRunTestsHandler());

            _router = new AgentRpcRouter(_options, _registry);
        }

        [TearDown]
        public void TearDown()
        {
            var obj = GameObject.Find("AgentBridgeTestObject");
            if (obj != null) Object.DestroyImmediate(obj);
        }

        private AgentBridgeOptions _options;
        private AgentCommandRegistry _registry;
        private AgentRpcRouter _router;
        private TestStartupOrchestrator _startupOrchestrator;

        [Test]
        public void Handle_FindGameObjectWithoutAuth_ShouldReturnUnauthenticated()
        {
            var response =
                _router.Handle(
                    "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"unity.gameobject.find\",\"params\":{\"name\":\"Player\"}}",
                    "c1");
            StringAssert.Contains("\"code\":-32001", response);
        }

        [Test]
        public void Handle_AuthenticateThenFind_ShouldSucceed()
        {
            var go = new GameObject("AgentBridgeTestObject");
            _router.Handle(
                "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}",
                "c2");

            var response =
                _router.Handle(
                    "{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"unity.gameobject.find\",\"params\":{\"name\":\"AgentBridgeTestObject\"}}",
                    "c2");

            Assert.That(go, Is.Not.Null);
            StringAssert.Contains("\"found\":true", response);
        }

        [Test]
        public void Handle_Commands_ShouldReturnRegisteredMethods()
        {
            _router.Handle(
                "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}",
                "c3");
            var response =
                _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"agent.commands\",\"params\":{}}", "c3");

            StringAssert.Contains("agent.ping", response);
            StringAssert.Contains("unity.component.invoke", response);
            StringAssert.Contains("startup.run", response);
            StringAssert.Contains("startup.stop", response);
            StringAssert.Contains("unity.editor.executeMenu", response);
            StringAssert.Contains("unity.tests.run", response);
        }

        [Test]
        public void Handle_StartupRun_ShouldSucceed()
        {
            _router.Handle(
                "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}",
                "s1");

            var response =
                _router.Handle(
                    "{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"startup.run\",\"params\":{\"environment\":\"DevFull\"}}",
                    "s1");

            StringAssert.Contains("\"success\":true", response);
            StringAssert.Contains("\"environment\":\"DevFull\"", response);
            Assert.That(_startupOrchestrator.RunCalledCount, Is.EqualTo(1));
        }

        [Test]
        public void Handle_StartupStop_ShouldSucceed()
        {
            _router.Handle(
                "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}",
                "s2");

            var response =
                _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"startup.stop\",\"params\":{}}", "s2");

            StringAssert.Contains("\"stopped\":true", response);
            Assert.That(_startupOrchestrator.StopCalledCount, Is.EqualTo(1));
        }

        [Test]
        public void Handle_StartupRun_NonPlayModeWithUnsupportedTasks_ShouldReturnInvalidParams()
        {
            var registry = new AgentCommandRegistry();
            var startupOrchestrator = new TestStartupOrchestrator();
            registry.Register(new AuthenticateCommandHandler());
            registry.Register(new StartupRunCommandHandler(
                startupOrchestrator,
                new FixedProfileProvider(new StartupProfile("HeadlessInvalid", new[] { StartupTaskKey.EnterLobby })),
                () => false));

            var router = new AgentRpcRouter(_options, registry);
            router.Handle(
                "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}",
                "s3");

            var response =
                router.Handle(
                    "{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"startup.run\",\"params\":{\"environment\":\"DevFull\"}}",
                    "s3");

            StringAssert.Contains("\"code\":-32602", response);
            StringAssert.Contains("EnterLobby", response);
            Assert.That(startupOrchestrator.RunCalledCount, Is.EqualTo(0));
        }

        [Test]
        public void Handle_ExecuteMenuWithInvalidParams_ShouldReturnInvalidParams()
        {
            _router.Handle(
                "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}",
                "cm1");
            var response =
                _router.Handle(
                    "{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"unity.editor.executeMenu\",\"params\":{}}", "cm1");

            StringAssert.Contains("\"code\":-32602", response);
        }

        [Test]
        public void Handle_RunTestsWithInvalidMode_ShouldReturnInvalidParams()
        {
            _router.Handle(
                "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}",
                "ct1");
            var response =
                _router.Handle(
                    "{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"unity.tests.run\",\"params\":{\"mode\":\"UnknownMode\"}}",
                    "ct1");

            StringAssert.Contains("\"code\":-32602", response);
        }

        [Test]
        public void Handle_Notification_ShouldReturnNull()
        {
            var response = _router.Handle("{\"jsonrpc\":\"2.0\",\"method\":\"agent.ping\",\"params\":{}}", "n1");
            Assert.That(response, Is.Null);
        }

        [Test]
        public void Handle_ReflectionDisabled_ShouldReturnDenied()
        {
            _router.Handle(
                "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}",
                "c4");
            var response =
                _router.Handle(
                    "{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"unity.reflect.invoke\",\"params\":{\"assembly\":\"Assembly-CSharp\",\"type\":\"xFrame.Tests.AgentBridgeRouterTests\",\"method\":\"TestStatic\"}}",
                    "c4");

            StringAssert.Contains("\"code\":-32012", response);
        }

        [Test]
        public void Handle_ReflectionEnabledButTypeNotAllowed_ShouldReturnDenied()
        {
            _options.EnableReflectionBridge = true;
            _options.AllowedTypePrefixes = new[] { "Allowed.Only" };
            _router = new AgentRpcRouter(_options, _registry);

            _router.Handle(
                "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}",
                "c6");
            var response =
                _router.Handle(
                    "{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"unity.reflect.invoke\",\"params\":{\"assembly\":\"Assembly-CSharp\",\"type\":\"xFrame.Tests.AgentBridgeRouterTests\",\"method\":\"TestStatic\"}}",
                    "c6");

            StringAssert.Contains("\"code\":-32012", response);
        }

        [Test]
        public void Handle_ReflectionEnabled_CommandsShouldContainReflectInvoke()
        {
            _options.EnableReflectionBridge = true;
            _router = new AgentRpcRouter(_options, _registry);

            _router.Handle(
                "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}",
                "c7");
            var response =
                _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"agent.commands\",\"params\":{}}", "c7");

            StringAssert.Contains("unity.reflect.invoke", response);
        }

        [Test]
        public void RemoveContext_AfterAuthenticate_ShouldRequireAuthenticateAgain()
        {
            _router.Handle(
                "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}",
                "c8");
            _router.RemoveContext("c8");

            var response =
                _router.Handle(
                    "{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"unity.gameobject.find\",\"params\":{\"name\":\"Player\"}}",
                    "c8");
            StringAssert.Contains("\"code\":-32001", response);
        }

        [Test]
        public void Handle_InvokeComponentWithInvalidParams_ShouldReturnInvalidParams()
        {
            _router.Handle(
                "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}",
                "c5");
            var response =
                _router.Handle(
                    "{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"unity.component.invoke\",\"params\":{\"gameObjectName\":\"AgentBridgeTestObject\"}}",
                    "c5");

            StringAssert.Contains("\"code\":-32602", response);
        }

        public static string TestStatic()
        {
            return "ok";
        }

        private sealed class TestEditorExecuteMenuHandler : IAgentRpcCommandHandler
        {
            public string Method => "unity.editor.executeMenu";

            public bool RequiresAuthentication => true;

            public AgentRpcExecutionResult Execute(JsonRpcRequest request, AgentRpcContext context)
            {
                var paramsText = request.Params?.ToString();
                if (string.IsNullOrWhiteSpace(paramsText))
                    return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "params must be object.");

                const string menuPathToken = "\"menuPath\"";
                if (!paramsText.Contains(menuPathToken, StringComparison.Ordinal))
                    return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "menuPath is required.");

                var menuPath = "mock-menu";
                if (string.IsNullOrWhiteSpace(menuPath))
                    return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "menuPath is required.");

                return AgentRpcExecutionResult.Success(new { executed = true, menuPath });
            }
        }

        private sealed class TestRunTestsHandler : IAgentRpcCommandHandler
        {
            public string Method => "unity.tests.run";

            public bool RequiresAuthentication => true;

            public AgentRpcExecutionResult Execute(JsonRpcRequest request, AgentRpcContext context)
            {
                var paramsText = request.Params?.ToString();
                if (string.IsNullOrWhiteSpace(paramsText))
                    return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "params must be object.");

                var containsMode = paramsText.Contains("\"mode\"", StringComparison.Ordinal);
                var containsEditMode = paramsText.Contains("\"EditMode\"", StringComparison.Ordinal);
                var containsPlayMode = paramsText.Contains("\"PlayMode\"", StringComparison.Ordinal);
                if (containsMode && !containsEditMode && !containsPlayMode)
                    return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams,
                        "mode must be EditMode or PlayMode.");

                return AgentRpcExecutionResult.Success(new { started = true });
            }
        }

        private sealed class TestStartupOrchestrator : IStartupOrchestrator
        {
            public int RunCalledCount { get; private set; }

            public int StopCalledCount { get; private set; }

            public StartupOrchestratorState State { get; private set; } = StartupOrchestratorState.Idle;

            public Task<StartupPipelineResult> RunAsync(BootEnvironment environment,
                CancellationToken cancellationToken)
            {
                RunCalledCount++;
                State = StartupOrchestratorState.Running;
                return Task.FromResult(StartupPipelineResult.Succeeded());
            }

            public Task<StartupPipelineResult> RunAsync(StartupProfile profile, CancellationToken cancellationToken)
            {
                RunCalledCount++;
                State = StartupOrchestratorState.Running;
                return Task.FromResult(StartupPipelineResult.Succeeded());
            }

            public Task ShutdownAsync(CancellationToken cancellationToken)
            {
                StopCalledCount++;
                State = StartupOrchestratorState.Stopped;
                return Task.CompletedTask;
            }
        }

        private sealed class FixedProfileProvider : IStartupProfileProvider
        {
            private readonly StartupProfile _profile;

            public FixedProfileProvider(StartupProfile profile)
            {
                _profile = profile;
            }

            public StartupProfile GetProfile(BootEnvironment environment)
            {
                return _profile;
            }
        }
    }
}