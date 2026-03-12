using Newtonsoft.Json.Linq;
using NUnit.Framework;
using xFrame.Runtime.Networking.AgentBridge;

namespace xFrame.Tests
{
    [TestFixture]
    public class AgentRpcContextTests
    {
        [Test]
        public void PublishEvent_ShouldWrapPayloadIntoAgentEventNotification()
        {
            var captured = string.Empty;
            var context = new AgentRpcContext(
                "ctx-1",
                new AgentBridgeOptions(),
                new AgentCommandRegistry(),
                payload => captured = payload);

            context.PublishEvent("unity.tests.progress", new
            {
                state = "running",
                completed = 3
            });

            var notification = JObject.Parse(captured);
            Assert.That(notification.Value<string>("jsonrpc"), Is.EqualTo("2.0"));
            Assert.That(notification.Value<string>("method"), Is.EqualTo("agent.event"));
            Assert.That(notification["params"]?["name"]?.Value<string>(), Is.EqualTo("unity.tests.progress"));
            Assert.That(notification["params"]?["payload"]?["state"]?.Value<string>(), Is.EqualTo("running"));
            Assert.That(notification["params"]?["payload"]?["completed"]?.Value<int>(), Is.EqualTo(3));
        }
    }
}
