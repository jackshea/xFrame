using NUnit.Framework;
using System.IO;
using xFrame.Runtime.Networking.AgentBridge;

namespace xFrame.Tests
{
    [TestFixture]
    public class AgentBridgeEndpointPersistenceTests
    {
        [SetUp]
        public void SetUp()
        {
            if (File.Exists(AgentBridgeLocalSettingsStorage.SettingsFilePath))
                File.Delete(AgentBridgeLocalSettingsStorage.SettingsFilePath);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(AgentBridgeLocalSettingsStorage.SettingsFilePath))
                File.Delete(AgentBridgeLocalSettingsStorage.SettingsFilePath);
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
            AgentBridgeLocalSettingsStorage.Save(new AgentBridgeLocalSettings
            {
                Host = " ",
                Port = 70000,
                AuthToken = "persisted-token"
            });

            var persistence = new AgentBridgeEndpointPersistence();
            var result = persistence.Load(out var host, out var port, out var error);

            Assert.That(result, Is.EqualTo(AgentBridgeEndpointLoadResult.Invalid));
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(host, Is.EqualTo(AgentBridgeOptions.DefaultHost));
            Assert.That(port, Is.EqualTo(AgentBridgeOptions.DefaultPort));
        }

        [Test]
        public void TrySave_ShouldPreserveExistingAuthToken()
        {
            AgentBridgeLocalSettingsStorage.Save(new AgentBridgeLocalSettings
            {
                Host = AgentBridgeOptions.DefaultHost,
                Port = AgentBridgeOptions.DefaultPort,
                AuthToken = "persisted-token"
            });

            var persistence = new AgentBridgeEndpointPersistence();
            var saveResult = persistence.TrySave("10.0.0.15", 18888, out var error);
            var settings = AgentBridgeLocalSettingsStorage.Load(out var loadError);

            Assert.That(saveResult, Is.True, error);
            Assert.That(loadError, Is.Null.Or.Empty);
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.Host, Is.EqualTo("10.0.0.15"));
            Assert.That(settings.Port, Is.EqualTo(18888));
            Assert.That(settings.AuthToken, Is.EqualTo("persisted-token"));
        }
    }
}
