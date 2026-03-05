using NUnit.Framework;
using UnityEngine;
using xFrame.Runtime.Networking.AgentBridge;

namespace xFrame.Tests
{
    [TestFixture]
    public class AgentBridgeEndpointPersistenceTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(AgentBridgeEndpointPersistence.HostKey);
            PlayerPrefs.DeleteKey(AgentBridgeEndpointPersistence.PortKey);
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(AgentBridgeEndpointPersistence.HostKey);
            PlayerPrefs.DeleteKey(AgentBridgeEndpointPersistence.PortKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void TrySaveAndLoad_ValidEndpoint_ShouldPersist()
        {
            var persistence = new AgentBridgeEndpointPersistence();

            var saveResult = persistence.TrySave("192.168.0.8", 19001, out var saveError);
            var loadResult = persistence.Load(out var host, out var port, out var loadError);

            Assert.That(saveResult, Is.True, saveError);
            Assert.That(loadResult, Is.EqualTo(AgentBridgeEndpointLoadResult.Loaded), loadError);
            Assert.That(host, Is.EqualTo("192.168.0.8"));
            Assert.That(port, Is.EqualTo(19001));
        }

        [Test]
        public void TrySave_InvalidEndpoint_ShouldReturnFalse()
        {
            var persistence = new AgentBridgeEndpointPersistence();

            var result = persistence.TrySave("", 70000, out var error);

            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Load_InvalidPersistedEndpoint_ShouldFallbackToDefault()
        {
            PlayerPrefs.SetString(AgentBridgeEndpointPersistence.HostKey, " ");
            PlayerPrefs.SetInt(AgentBridgeEndpointPersistence.PortKey, 70000);
            PlayerPrefs.Save();

            var persistence = new AgentBridgeEndpointPersistence();
            var result = persistence.Load(out var host, out var port, out var error);

            Assert.That(result, Is.EqualTo(AgentBridgeEndpointLoadResult.Invalid));
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(host, Is.EqualTo(AgentBridgeOptions.DefaultHost));
            Assert.That(port, Is.EqualTo(AgentBridgeOptions.DefaultPort));
        }
    }
}
