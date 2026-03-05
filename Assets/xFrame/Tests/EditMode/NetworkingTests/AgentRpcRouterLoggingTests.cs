using System;
using System.Collections.Generic;
using NUnit.Framework;
using xFrame.Runtime.Logging;
using xFrame.Runtime.Networking.AgentBridge;
using xFrame.Runtime.Networking.AgentBridge.Commands;

namespace xFrame.Tests
{
    [TestFixture]
    public class AgentRpcRouterLoggingTests
    {
        [Test]
        public void Handle_ParseError_ShouldWriteWarningLog()
        {
            var logger = new CapturingLogger();
            var router = CreateRouter(logger);

            var response = router.Handle("{bad-json", "c-log-1");

            Assert.That(response, Is.Not.Null.And.Not.Empty);
            Assert.That(logger.Messages, Has.Some.Contains("direction=receive"));
            Assert.That(logger.Messages, Has.Some.Contains("parse error"));
            Assert.That(logger.Levels, Has.Member(LogLevel.Warning));
        }

        [Test]
        public void Handle_Ping_ShouldWriteReceiveAndSendDebugLog()
        {
            var logger = new CapturingLogger();
            var router = CreateRouter(logger);

            var response = router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"agent.ping\",\"params\":{}}",
                "c-log-2");

            Assert.That(response, Is.Not.Null.And.Not.Empty);
            Assert.That(logger.Messages, Has.Some.Contains("direction=receive"));
            Assert.That(logger.Messages, Has.Some.Contains("direction=send"));
            Assert.That(logger.Levels, Has.Member(LogLevel.Debug));
        }

        [Test]
        public void Handle_HandlerThrows_ShouldWriteErrorLog()
        {
            var logger = new CapturingLogger();
            var options = new AgentBridgeOptions();
            var registry = new AgentCommandRegistry();
            registry.Register(new ThrowCommandHandler());
            var router = new AgentRpcRouter(options, registry, logger: logger);

            var response = router.Handle("{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"test.throw\",\"params\":{}}",
                "c-log-3");

            Assert.That(response, Is.Not.Null.And.Not.Empty);
            Assert.That(logger.Messages, Has.Some.Contains("direction=error"));
            Assert.That(logger.Levels, Has.Member(LogLevel.Error));
        }

        private static AgentRpcRouter CreateRouter(CapturingLogger logger)
        {
            var options = new AgentBridgeOptions();
            var registry = new AgentCommandRegistry();
            registry.Register(new PingCommandHandler());
            return new AgentRpcRouter(options, registry, logger: logger);
        }

        private sealed class ThrowCommandHandler : IAgentRpcCommandHandler
        {
            public string Method => "test.throw";

            public bool RequiresAuthentication => false;

            public AgentRpcExecutionResult Execute(JsonRpcRequest request, AgentRpcContext context)
            {
                throw new InvalidOperationException("mock command failure");
            }
        }

        private sealed class CapturingLogger : IXLogger
        {
            public List<string> Messages { get; } = new();

            public List<LogLevel> Levels { get; } = new();
            public string ModuleName => "AgentBridgeTests";

            public bool IsEnabled { get; set; } = true;

            public LogLevel MinLevel { get; set; } = LogLevel.Debug;

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
                if (!IsLevelEnabled(level)) return;

                Levels.Add(level);
                Messages.Add(message);
            }
        }
    }
}