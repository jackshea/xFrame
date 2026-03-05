using System;
using System.Collections.Generic;
using NUnit.Framework;
using xFrame.Editor.AgentBridge;
using xFrame.Runtime.Logging;
using xFrame.Runtime.Networking.AgentBridge;

namespace xFrame.Tests
{
    [TestFixture]
    public class AgentBridgeServerEndpointTests
    {
        [Test]
        public void Ctor_LoadInvalidPersistedEndpoint_ShouldFallbackAndWarn()
        {
            var logger = new CapturingLogger();
            var persistence = new StubPersistence
            {
                LoadResult = AgentBridgeEndpointLoadResult.Invalid,
                LoadError = "port out of range"
            };

            using var server = new FleckAgentBridgeServer(new AgentBridgeOptions(), logger, persistence);

            Assert.That(server.Endpoint, Is.EqualTo($"ws://{AgentBridgeOptions.DefaultHost}:{AgentBridgeOptions.DefaultPort}"));
            Assert.That(logger.Levels, Has.Member(LogLevel.Warning));
            Assert.That(logger.Messages, Has.Some.Contains("fallback to default"));
        }

        [Test]
        public void SetEndpoint_ValidChangedValue_ShouldPersistAndUpdateEndpoint()
        {
            var logger = new CapturingLogger();
            var persistence = new StubPersistence
            {
                LoadResult = AgentBridgeEndpointLoadResult.NotFound
            };

            using var server = new FleckAgentBridgeServer(new AgentBridgeOptions(), logger, persistence);
            var result = server.SetEndpoint("192.168.31.9", 18888, out var error);

            Assert.That(result, Is.True, error);
            Assert.That(server.Endpoint, Is.EqualTo("ws://192.168.31.9:18888"));
            Assert.That(persistence.SaveCallCount, Is.EqualTo(1));
            Assert.That(logger.Messages, Has.Some.Contains("endpoint updated"));
        }

        private sealed class StubPersistence : IAgentBridgeEndpointPersistence
        {
            public AgentBridgeEndpointLoadResult LoadResult { get; set; }

            public string LoadHost { get; set; } = AgentBridgeOptions.DefaultHost;

            public int LoadPort { get; set; } = AgentBridgeOptions.DefaultPort;

            public string LoadError { get; set; }

            public int SaveCallCount { get; private set; }

            public bool SaveResult { get; set; } = true;

            public string SaveError { get; set; }

            public bool TrySave(string host, int port, out string error)
            {
                SaveCallCount++;
                error = SaveError;
                return SaveResult;
            }

            public AgentBridgeEndpointLoadResult Load(out string host, out int port, out string error)
            {
                host = LoadHost;
                port = LoadPort;
                error = LoadError;
                return LoadResult;
            }
        }

        private sealed class CapturingLogger : IXLogger
        {
            public string ModuleName => "AgentBridgeTests";

            public bool IsEnabled { get; set; } = true;

            public LogLevel MinLevel { get; set; } = LogLevel.Debug;

            public List<string> Messages { get; } = new();

            public List<LogLevel> Levels { get; } = new();

            public void Verbose(string message)
            {
                Record(LogLevel.Verbose, message);
            }

            public void Debug(string message)
            {
                Record(LogLevel.Debug, message);
            }

            public void Info(string message)
            {
                Record(LogLevel.Info, message);
            }

            public void Warning(string message)
            {
                Record(LogLevel.Warning, message);
            }

            public void Error(string message)
            {
                Record(LogLevel.Error, message);
            }

            public void Error(string message, Exception exception)
            {
                Record(LogLevel.Error, $"{message} | {exception?.Message}");
            }

            public void Fatal(string message)
            {
                Record(LogLevel.Fatal, message);
            }

            public void Fatal(string message, Exception exception)
            {
                Record(LogLevel.Fatal, $"{message} | {exception?.Message}");
            }

            public void Log(LogLevel level, string message, Exception exception = null)
            {
                Record(level, exception == null ? message : $"{message} | {exception.Message}");
            }

            public bool IsLevelEnabled(LogLevel level)
            {
                return IsEnabled && level >= MinLevel;
            }

            private void Record(LogLevel level, string message)
            {
                if (!IsLevelEnabled(level))
                {
                    return;
                }

                Levels.Add(level);
                Messages.Add(message);
            }
        }
    }
}
