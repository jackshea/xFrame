using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using xFrame.Runtime.Logging;
using xFrame.Runtime.Logging.Appenders;

namespace xFrame.Tests
{
    /// <summary>
    ///     日志基础设施回归测试。
    /// </summary>
    [TestFixture]
    public class LoggingInfrastructureTests
    {
        private string _logFilePath;

        [SetUp]
        public void SetUp()
        {
            _logFilePath = Path.Combine(Path.GetTempPath(), $"xframe-log-{Guid.NewGuid():N}.log");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_logFilePath)) File.Delete(_logFilePath);
        }

        [Test]
        public void FileLogAppender_AutoFlushDisabled_ShouldWriteAfterExplicitFlush()
        {
            var appender = new FileLogAppender(_logFilePath, autoFlush: false);
            try
            {
                appender.WriteLog(new LogEntry(LogLevel.Info, "buffered-message", "LoggingTests"));
                appender.Flush();
            }
            finally
            {
                appender.Dispose();
            }

            var contentAfterFlush = File.ReadAllText(_logFilePath);
            Assert.That(contentAfterFlush, Does.Contain("buffered-message"));
        }

        [Test]
        public void XLogManager_Shutdown_ShouldDisposeGlobalAppender()
        {
            var manager = new XLogManager();
            var appender = new TrackingAppender();
            manager.AddGlobalAppender(appender);

            manager.Shutdown();

            Assert.That(appender.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void XLogger_Log_WhenAppenderThrows_ShouldNotThrow()
        {
            var logger = new XLogger("LoggingTests");
            logger.AddAppender(new ThrowingAppender());
            LogAssert.Expect(LogType.Error, new Regex("日志输出器 Throwing 写入失败: mock failure"));

            Assert.DoesNotThrow(() => logger.Info("safe"));
        }

        private sealed class TrackingAppender : ILogAppender
        {
            public int DisposeCount { get; private set; }

            public string Name => "Tracking";

            public bool IsEnabled { get; set; } = true;

            public LogLevel MinLevel { get; set; } = LogLevel.Debug;

            public void WriteLog(LogEntry entry)
            {
            }

            public void Flush()
            {
            }

            public void Dispose()
            {
                DisposeCount++;
            }
        }

        private sealed class ThrowingAppender : ILogAppender
        {
            public string Name => "Throwing";

            public bool IsEnabled { get; set; } = true;

            public LogLevel MinLevel { get; set; } = LogLevel.Debug;

            public void WriteLog(LogEntry entry)
            {
                throw new InvalidOperationException("mock failure");
            }

            public void Flush()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
