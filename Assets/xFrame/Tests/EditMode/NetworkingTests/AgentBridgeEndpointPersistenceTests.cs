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
                Port = 70000
            });

            var persistence = new AgentBridgeEndpointPersistence();
            var result = persistence.Load(out var host, out var port, out var error);

            Assert.That(result, Is.EqualTo(AgentBridgeEndpointLoadResult.Invalid));
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(host, Is.EqualTo(AgentBridgeOptions.DefaultHost));
            Assert.That(port, Is.EqualTo(AgentBridgeOptions.DefaultPort));
        }

        [Test]
        public void Load_CurrentInstanceEndpoint_ShouldPreferInstanceRegistration()
        {
            AgentBridgeLocalSettingsStorage.Save(new AgentBridgeLocalSettings
            {
                Host = "10.0.0.10",
                Port = 18888,
                Instances =
                {
                    new AgentBridgeInstanceRegistration
                    {
                        InstanceId = AgentBridgeLocalSettingsStorage.CurrentInstanceId,
                        ProcessId = AgentBridgeLocalSettingsStorage.CurrentProcessId,
                        ProjectPath = System.IO.Directory.GetParent(UnityEngine.Application.dataPath)?.FullName,
                        Host = "10.0.0.20",
                        Port = 19999,
                        IsRunning = true,
                        LastSeenUtc = "2026-03-11T00:00:00.0000000Z"
                    }
                }
            });

            var persistence = new AgentBridgeEndpointPersistence();
            var loadResult = persistence.Load(out var host, out var port, out var error);

            Assert.That(loadResult, Is.EqualTo(AgentBridgeEndpointLoadResult.Loaded), error);
            Assert.That(host, Is.EqualTo("10.0.0.20"));
            Assert.That(port, Is.EqualTo(19999));
        }

        [Test]
        public void Load_CurrentInstanceEndpointInvalid_ShouldFallbackToProjectDefault()
        {
            AgentBridgeLocalSettingsStorage.Save(new AgentBridgeLocalSettings
            {
                Host = "10.0.0.10",
                Port = 18888,
                Instances =
                {
                    new AgentBridgeInstanceRegistration
                    {
                        InstanceId = AgentBridgeLocalSettingsStorage.CurrentInstanceId,
                        ProcessId = AgentBridgeLocalSettingsStorage.CurrentProcessId,
                        ProjectPath = System.IO.Directory.GetParent(UnityEngine.Application.dataPath)?.FullName,
                        Host = "127.0.0.1",
                        Port = 70000,
                        IsRunning = true,
                        LastSeenUtc = "2026-03-11T00:00:00.0000000Z"
                    }
                }
            });

            var persistence = new AgentBridgeEndpointPersistence();
            var loadResult = persistence.Load(out var host, out var port, out var error);

            Assert.That(loadResult, Is.EqualTo(AgentBridgeEndpointLoadResult.Loaded), error);
            Assert.That(host, Is.EqualTo("10.0.0.10"));
            Assert.That(port, Is.EqualTo(18888));
        }

        [Test]
        public void UpsertCurrentInstance_ShouldKeepMultipleUnityInstanceRegistrations()
        {
            AgentBridgeLocalSettingsStorage.Save(new AgentBridgeLocalSettings
            {
                Instances =
                {
                    new AgentBridgeInstanceRegistration
                    {
                        InstanceId = "other-project::1234",
                        ProcessId = 1234,
                        ProjectPath = System.IO.Directory.GetParent(UnityEngine.Application.dataPath)?.FullName,
                        Host = "127.0.0.1",
                        Port = 17777,
                        IsRunning = true,
                        LastSeenUtc = "2026-03-11T00:00:00.0000000Z"
                    }
                }
            });

            AgentBridgeLocalSettingsStorage.UpsertCurrentInstance("127.0.0.1", 17778, true);
            var settings = AgentBridgeLocalSettingsStorage.Load(out var error);

            Assert.That(error, Is.Null.Or.Empty);
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.Instances, Has.Count.EqualTo(2));
            Assert.That(settings.Instances, Has.Some.Matches<AgentBridgeInstanceRegistration>(instance =>
                instance.InstanceId == "other-project::1234" && instance.Port == 17777));
            Assert.That(settings.Instances, Has.Some.Matches<AgentBridgeInstanceRegistration>(instance =>
                instance.InstanceId == AgentBridgeLocalSettingsStorage.CurrentInstanceId &&
                instance.Port == 17778 &&
                instance.IsRunning));
        }
    }
}
