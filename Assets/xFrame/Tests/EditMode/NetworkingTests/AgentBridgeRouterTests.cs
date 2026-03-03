using NUnit.Framework;
using UnityEngine;
using xFrame.Runtime.Networking.AgentBridge;
using xFrame.Runtime.Networking.AgentBridge.Commands;

namespace xFrame.Tests
{
    [TestFixture]
    public class AgentBridgeRouterTests
    {
        private AgentBridgeOptions _options;
        private AgentCommandRegistry _registry;
        private AgentRpcRouter _router;

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
            _registry.Register(new PingCommandHandler());
            _registry.Register(new AuthenticateCommandHandler());
            _registry.Register(new ListCommandsHandler());
            _registry.Register(new FindGameObjectCommandHandler());
            _registry.Register(new InvokeComponentCommandHandler());

            _router = new AgentRpcRouter(_options, _registry);
        }

        [TearDown]
        public void TearDown()
        {
            var obj = GameObject.Find("AgentBridgeTestObject");
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void Handle_FindGameObjectWithoutAuth_ShouldReturnUnauthenticated()
        {
            var response = _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"unity.gameobject.find\",\"params\":{\"name\":\"Player\"}}", "c1");
            StringAssert.Contains("\"code\":-32001", response);
        }

        [Test]
        public void Handle_AuthenticateThenFind_ShouldSucceed()
        {
            var go = new GameObject("AgentBridgeTestObject");
            _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}", "c2");

            var response = _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"unity.gameobject.find\",\"params\":{\"name\":\"AgentBridgeTestObject\"}}", "c2");

            Assert.That(go, Is.Not.Null);
            StringAssert.Contains("\"found\":true", response);
        }

        [Test]
        public void Handle_Commands_ShouldReturnRegisteredMethods()
        {
            _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}", "c3");
            var response = _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"agent.commands\",\"params\":{}}", "c3");

            StringAssert.Contains("agent.ping", response);
            StringAssert.Contains("unity.component.invoke", response);
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
            _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}", "c4");
            var response = _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"unity.reflect.invoke\",\"params\":{\"assembly\":\"Assembly-CSharp\",\"type\":\"xFrame.Tests.AgentBridgeRouterTests\",\"method\":\"TestStatic\"}}", "c4");

            StringAssert.Contains("\"code\":-32012", response);
        }

        [Test]
        public void Handle_ReflectionEnabledButTypeNotAllowed_ShouldReturnDenied()
        {
            _options.EnableReflectionBridge = true;
            _options.AllowedTypePrefixes = new[] { "Allowed.Only" };
            _router = new AgentRpcRouter(_options, _registry);

            _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}", "c6");
            var response = _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"unity.reflect.invoke\",\"params\":{\"assembly\":\"Assembly-CSharp\",\"type\":\"xFrame.Tests.AgentBridgeRouterTests\",\"method\":\"TestStatic\"}}", "c6");

            StringAssert.Contains("\"code\":-32012", response);
        }

        [Test]
        public void Handle_ReflectionEnabled_CommandsShouldContainReflectInvoke()
        {
            _options.EnableReflectionBridge = true;
            _router = new AgentRpcRouter(_options, _registry);

            _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}", "c7");
            var response = _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"agent.commands\",\"params\":{}}", "c7");

            StringAssert.Contains("unity.reflect.invoke", response);
        }

        [Test]
        public void RemoveContext_AfterAuthenticate_ShouldRequireAuthenticateAgain()
        {
            _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}", "c8");
            _router.RemoveContext("c8");

            var response = _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"unity.gameobject.find\",\"params\":{\"name\":\"Player\"}}", "c8");
            StringAssert.Contains("\"code\":-32001", response);
        }

        [Test]
        public void Handle_InvokeComponentWithInvalidParams_ShouldReturnInvalidParams()
        {
            _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.authenticate\",\"params\":{\"token\":\"test-token\"}}", "c5");
            var response = _router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"unity.component.invoke\",\"params\":{\"gameObjectName\":\"AgentBridgeTestObject\"}}", "c5");

            StringAssert.Contains("\"code\":-32602", response);
        }

        public static string TestStatic()
        {
            return "ok";
        }
    }
}
