using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using xFrame.Runtime.Logging;
using xFrame.Runtime.Startup;

namespace xFrame.Tests
{
    /// <summary>
    ///     默认启动失败处理器回归测试。
    /// </summary>
    [TestFixture]
    public class StartupDefaultFailureHandlerTests
    {
        [Test]
        public void HandleFailureAsync_ShouldLogAndPresentError()
        {
            var handler = new StartupDefaultFailureHandler();
            var resolver = new TestResolver();
            var logManager = new FakeLogManager();
            var presentationService = new FakePresentationService();
            resolver.Register<IXLogManager>(logManager);
            resolver.Register<IStartupErrorPresentationService>(presentationService);
            var context = new StartupTaskContext(resolver);
            var failure = StartupTaskResult.Failed("ConnectFailed", "网络连接失败", true);
            var snapshot = new StartupLifecycleSnapshot(
                StartupLifecycleStage.Failed,
                BootEnvironment.Release,
                StartupPipelineResult.Failed("NetworkConnect", failure, false));

            handler.HandleFailureAsync(snapshot, context, CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, logManager.Logger.ErrorMessages.Count);
            StringAssert.Contains("NetworkConnect", logManager.Logger.ErrorMessages[0]);
            StringAssert.Contains("ConnectFailed", logManager.Logger.ErrorMessages[0]);
            Assert.IsTrue(presentationService.Called);
            Assert.AreEqual("NetworkConnect", presentationService.LastSnapshot.PipelineResult.FailedTaskName);
        }

        [Test]
        public void HandleFailureAsync_WithException_ShouldLogException()
        {
            var handler = new StartupDefaultFailureHandler();
            var resolver = new TestResolver();
            var logManager = new FakeLogManager();
            resolver.Register<IXLogManager>(logManager);
            var context = new StartupTaskContext(resolver);
            var exception = new InvalidOperationException("broken");
            var failure = StartupTaskResult.Failed("Exception", "执行异常", true, exception);
            var snapshot = new StartupLifecycleSnapshot(
                StartupLifecycleStage.Failed,
                BootEnvironment.DevFull,
                StartupPipelineResult.Failed("SdkInit", failure, false));

            handler.HandleFailureAsync(snapshot, context, CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, logManager.Logger.ErrorExceptions.Count);
            Assert.AreSame(exception, logManager.Logger.ErrorExceptions[0]);
        }

        private sealed class TestResolver : IStartupServiceResolver
        {
            private readonly Dictionary<Type, object> _services = new();

            public void Register<T>(T service) where T : class
            {
                _services[typeof(T)] = service;
            }

            public T Resolve<T>() where T : class
            {
                return _services.TryGetValue(typeof(T), out var service) ? service as T : null;
            }

            public bool TryResolve<T>(out T service) where T : class
            {
                service = Resolve<T>();
                return service != null;
            }
        }

        private sealed class FakePresentationService : IStartupErrorPresentationService
        {
            public bool Called { get; private set; }
            public StartupLifecycleSnapshot LastSnapshot { get; private set; }

            public System.Threading.Tasks.Task PresentErrorAsync(
                StartupLifecycleSnapshot snapshot,
                StartupTaskContext context,
                CancellationToken cancellationToken)
            {
                Called = true;
                LastSnapshot = snapshot;
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }

        private sealed class FakeLogManager : IXLogManager
        {
            public FakeLogger Logger { get; } = new();

            public LogLevel GlobalMinLevel { get; set; }
            public bool IsGlobalEnabled { get; set; }

            public IXLogger GetLogger(string moduleName)
            {
                return Logger;
            }

            public IXLogger GetLogger(Type type)
            {
                return Logger;
            }

            public IXLogger GetLogger<T>()
            {
                return Logger;
            }

            public void AddGlobalAppender(ILogAppender appender)
            {
            }

            public void RemoveGlobalAppender(ILogAppender appender)
            {
            }

            public void FlushAll()
            {
            }

            public void Shutdown()
            {
            }
        }

        private sealed class FakeLogger : IXLogger
        {
            public List<string> ErrorMessages { get; } = new();
            public List<Exception> ErrorExceptions { get; } = new();

            public string ModuleName => "Startup";
            public LogLevel MinLevel { get; set; }
            public bool IsEnabled { get; set; }

            public void Verbose(string message)
            {
            }

            public void Debug(string message)
            {
            }

            public void Info(string message)
            {
            }

            public void Warning(string message)
            {
            }

            public void Error(string message)
            {
                ErrorMessages.Add(message);
            }

            public void Error(string message, Exception exception)
            {
                ErrorMessages.Add(message);
                ErrorExceptions.Add(exception);
            }

            public void Fatal(string message)
            {
            }

            public void Fatal(string message, Exception exception)
            {
            }

            public void Log(LogLevel level, string message, Exception exception = null)
            {
                if (level >= LogLevel.Error)
                    Error(message, exception);
            }

            public bool IsLevelEnabled(LogLevel level)
            {
                return true;
            }
        }
    }
}
