using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor.TestTools.TestRunner.Api;
using xFrame.Editor.AgentBridge;
using xFrame.Editor.Tests;
using xFrame.Runtime.Networking.AgentBridge;

namespace xFrame.Tests
{
    [TestFixture]
    public class EditorRunTestsCommandHandlerTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorRunTestsCommandHandler.ResetSnapshotForTests();
        }

        [TearDown]
        public void TearDown()
        {
            EditorRunTestsCommandHandler.ResetSnapshotForTests();
        }

        [Test]
        public void LastResultHandler_RunningSnapshot_ShouldContainProgressSummary()
        {
            EditorRunTestsCommandHandler.MarkRunProgress(
                "run-progress",
                TestMode.EditMode,
                "SchedulerServiceTests",
                10,
                3,
                2,
                1,
                0,
                0,
                1.25d,
                "xFrame.Tests.SchedulerTests.SchedulerServiceTests.Sample");

            var payload = ReadLastResultPayload();

            Assert.That(payload.Value<string>("status"), Is.EqualTo("running"));
            Assert.That(payload["summary"]?["total"]?.Value<int>(), Is.EqualTo(10));
            Assert.That(payload["summary"]?["completed"]?.Value<int>(), Is.EqualTo(3));
            Assert.That(payload["summary"]?["currentTest"]?.Value<string>(),
                Is.EqualTo("xFrame.Tests.SchedulerTests.SchedulerServiceTests.Sample"));
        }

        [Test]
        public void LastResultHandler_FinishedSnapshot_ShouldContainCompletedSummary()
        {
            EditorRunTestsCommandHandler.MarkRunFinished(
                "run-finished",
                TestMode.PlayMode,
                null,
                "failed",
                4,
                3,
                1,
                0,
                0,
                2.5d,
                new object[]
                {
                    new
                    {
                        name = "SampleTest",
                        message = "boom"
                    }
                });

            var payload = ReadLastResultPayload();

            Assert.That(payload.Value<string>("status"), Is.EqualTo("failed"));
            Assert.That(payload["summary"]?["total"]?.Value<int>(), Is.EqualTo(4));
            Assert.That(payload["summary"]?["completed"]?.Value<int>(), Is.EqualTo(4));
            Assert.That(payload["failures"]?.First?["name"]?.Value<string>(), Is.EqualTo("SampleTest"));
        }

        [Test]
        public void UnityTestFilterFactory_Create_WithNameFragment_ShouldUseEscapedGroupNames()
        {
            var filter = UnityTestFilterFactory.Create(TestMode.EditMode, "SchedulerServiceTests");

            Assert.That(filter.testMode, Is.EqualTo(TestMode.EditMode));
            Assert.That(filter.groupNames, Is.EqualTo(new[] { "SchedulerServiceTests" }));
            Assert.That(filter.testNames, Is.Null);
        }

        [Test]
        public void UnityTestFilterFactory_Create_WithSpecialCharacters_ShouldEscapeRegexCharacters()
        {
            var filter = UnityTestFilterFactory.Create(
                TestMode.EditMode,
                "xFrame.Tests.SchedulerTests.SchedulerServiceTests.Delay_ShouldExecuteAfterDelay()");

            Assert.That(
                filter.groupNames,
                Is.EqualTo(new[]
                {
                    @"xFrame\.Tests\.SchedulerTests\.SchedulerServiceTests\.Delay_ShouldExecuteAfterDelay\(\)"
                }));
        }

        private static JObject ReadLastResultPayload()
        {
            var handler = new EditorRunTestsCommandHandler.LastResultHandler();
            var result = handler.Execute(
                new JsonRpcRequest
                {
                    JsonRpc = "2.0",
                    Id = JValue.CreateString("1"),
                    Method = "unity.tests.lastResult",
                    Params = new JObject()
                },
                new AgentRpcContext("tests", new AgentBridgeOptions(), new AgentCommandRegistry()));

            return JObject.Parse(JsonConvert.SerializeObject(result.Result));
        }
    }
}
