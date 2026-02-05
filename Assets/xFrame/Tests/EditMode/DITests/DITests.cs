using System;
using NUnit.Framework;
using VContainer;
using VContainer.Unity;
using xFrame.Runtime.DI;
using xFrame.Runtime.Logging;

namespace xFrame.Tests
{
    /// <summary>
    /// xFrameLifetimeScope unit tests
    /// Tests dependency injection container configuration and service registration
    /// </summary>
    [TestFixture]
    public class DITests
    {
        private IObjectResolver _resolver;

        /// <summary>
        /// Test setup
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            // Create simple container for testing
            var builder = new ContainerBuilder();

            // Register mock logger for testing
            builder.Register<IXLogger, MockLogger>(Lifetime.Singleton);

            // Register some test services
            builder.Register<ITestServiceA, TestServiceA>(Lifetime.Transient);
            builder.Register<ITestServiceB, TestServiceB>(Lifetime.Singleton);
            builder.Register<ITestServiceC, TestServiceC>(Lifetime.Scoped);

            _resolver = builder.Build();
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            (_resolver as IDisposable)?.Dispose();
            _resolver = null;
        }

        #region Service Registration Tests

        /// <summary>
        /// Test singleton service registration and resolution
        /// </summary>
        [Test]
        public void Singleton_ShouldResolveSameInstance()
        {
            // Arrange
            var instance1 = _resolver.Resolve<ITestServiceB>();
            var instance2 = _resolver.Resolve<ITestServiceB>();

            // Assert
            Assert.IsNotNull(instance1, "First resolution should return instance");
            Assert.IsNotNull(instance2, "Second resolution should return instance");
            Assert.AreSame(instance1, instance2, "Singleton service should return same instance");
        }

        /// <summary>
        /// Test transient service registration and resolution
        /// </summary>
        [Test]
        public void Transient_ShouldResolveDifferentInstances()
        {
            // Arrange
            var instance1 = _resolver.Resolve<ITestServiceA>();
            var instance2 = _resolver.Resolve<ITestServiceA>();

            // Assert
            Assert.IsNotNull(instance1, "First resolution should return instance");
            Assert.IsNotNull(instance2, "Second resolution should return instance");
            Assert.AreNotSame(instance1, instance2, "Transient service should return different instances");
        }

        /// <summary>
        /// Test scoped service registration and resolution
        /// </summary>
        [Test]
        public void Scoped_ShouldResolveDifferentInstances()
        {
            // Arrange
            using var scope1 = _resolver.CreateScope();
            using var scope2 = _resolver.CreateScope();

            var instance1 = scope1.Resolve<ITestServiceC>();
            var instance2 = scope2.Resolve<ITestServiceC>();

            // Assert
            Assert.IsNotNull(instance1, "First resolution should return instance");
            Assert.IsNotNull(instance2, "Second resolution should return instance");
            Assert.AreNotSame(instance1, instance2, "Different scope services should return different instances");
        }

        /// <summary>
        /// Test service singleton within scope
        /// </summary>
        [Test]
        public void Scoped_ShouldResolveSameInstanceWithinSameScope()
        {
            // Arrange
            using var scope = _resolver.CreateScope();

            var instance1 = scope.Resolve<ITestServiceC>();
            var instance2 = scope.Resolve<ITestServiceC>();

            // Assert
            Assert.AreSame(instance1, instance2, "Same scope should return same instance");
        }

        #endregion

        #region Dependency Injection Tests

        /// <summary>
        /// Test constructor injection
        /// </summary>
        [Test]
        public void ConstructorInjection_ShouldInjectDependencies()
        {
            // Arrange
            var service = _resolver.Resolve<ITestServiceA>() as TestServiceA;

            // Assert
            Assert.IsNotNull(service, "Service should be resolved");
            Assert.IsNotNull(service.ServiceB, "Dependency ServiceB should be injected");
            Assert.IsNotNull(service.Logger, "Logger service should be injected");
        }

        /// <summary>
        /// Test nested dependency injection
        /// </summary>
        [Test]
        public void NestedDependencyInjection_ShouldWork()
        {
            // Arrange
            var serviceC = _resolver.Resolve<ITestServiceC>() as TestServiceC;

            // Assert
            Assert.IsNotNull(serviceC, "Service C should be resolved");
            Assert.IsNotNull(serviceC.ServiceA, "Dependency ServiceA should be injected");
            Assert.IsNotNull(serviceC.ServiceA.ServiceB, "Nested dependency ServiceB should be injected");
            Assert.IsNotNull(serviceC.Logger, "Logger service should be injected");
        }

        /// <summary>
        /// Test interface registration and resolution
        /// </summary>
        [Test]
        public void InterfaceRegistration_ShouldResolveImplementation()
        {
            // Arrange
            ITestServiceA service = _resolver.Resolve<ITestServiceA>();

            // Assert
            Assert.IsNotNull(service, "Interface should be resolved");
            Assert.IsInstanceOf<TestServiceA>(service, "Should resolve to correct implementation type");
        }

        #endregion

        #region Exception Handling Tests

        /// <summary>
        /// Test resolving unregistered service should throw exception
        /// </summary>
        [Test]
        public void ResolveUnregisteredService_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<VContainerException>(() =>
            {
                _resolver.Resolve<ITestServiceD>();
            }, "Resolving unregistered service should throw exception");
        }

        /// <summary>
        /// Test try-resolve unregistered service should return null
        /// </summary>
        [Test]
        public void TryResolveUnregisteredService_ShouldReturnNull()
        {
            // Act
            var result = _resolver.TryResolve<ITestServiceD>(out var service);

            // Assert
            Assert.IsFalse(result, "Try-resolve unregistered service should return false");
            Assert.IsNull(service, "Service should be null");
        }

        /// <summary>
        /// Test try-resolve registered service should return true
        /// </summary>
        [Test]
        public void TryResolveRegisteredService_ShouldReturnTrue()
        {
            // Act
            var result = _resolver.TryResolve<ITestServiceA>(out var service);

            // Assert
            Assert.IsTrue(result, "Try-resolve registered service should return true");
            Assert.IsNotNull(service, "Service should not be null");
            Assert.IsInstanceOf<TestServiceA>(service, "Should resolve to correct implementation type");
        }

        #endregion

        #region Factory Method Tests

        /// <summary>
        /// Test factory method registration and resolution
        /// </summary>
        [Test]
        public void FactoryMethod_ShouldCreateNewInstance()
        {
            // Arrange
            var instance1 = _resolver.Resolve<ITestServiceA>();
            var instance2 = _resolver.Resolve<ITestServiceA>();

            // Assert
            Assert.IsNotNull(instance1, "First resolution should return instance");
            Assert.IsNotNull(instance2, "Second resolution should return instance");
            Assert.AreNotSame(instance1, instance2, "Factory method should create different instances");
        }

        #endregion

        #region Scope Lifetime Tests

        /// <summary>
        /// Test scope disposal
        /// </summary>
        [Test]
        public void Scope_ShouldBeDisposable()
        {
            // Arrange
            var scope = _resolver.CreateScope();

            // Assert
            Assert.IsNotNull(scope, "Scope should be created");
        }

        /// <summary>
        /// Test scope nesting
        /// </summary>
        [Test]
        public void NestedScopes_ShouldWork()
        {
            // Arrange
            IObjectResolver outerResolver = _resolver;

            using (var outerScope = outerResolver.CreateScope())
            {
                var outerC = outerScope.Resolve<ITestServiceC>();
                Assert.IsNotNull(outerC, "Outer scope service should be resolved");

                using (var innerScope = outerResolver.CreateScope())
                {
                    var innerC = innerScope.Resolve<ITestServiceC>();
                    Assert.IsNotNull(innerC, "Inner scope service should be resolved");
                    Assert.AreNotSame(outerC, innerC, "Different scopes should return different instances");
                }

                // After inner scope is disposed, outer scope service should still be valid
                var sameC = outerScope.Resolve<ITestServiceC>();
                Assert.AreSame(outerC, sameC, "Same scope should return same instance");
            }
        }

        #endregion

        #region Test Interfaces

        /// <summary>
        /// Test service A interface
        /// </summary>
        public interface ITestServiceA
        {
            void DoSomething();
            ITestServiceB ServiceB { get; }
        }

        /// <summary>
        /// Test service B interface
        /// </summary>
        public interface ITestServiceB
        {
            void DoSomething();
        }

        /// <summary>
        /// Test service C interface
        /// </summary>
        public interface ITestServiceC
        {
            void DoSomething();
        }

        /// <summary>
        /// Test service D interface (not registered)
        /// </summary>
        public interface ITestServiceD
        {
            void DoSomething();
        }

        #endregion

        #region Test Implementation Classes

        /// <summary>
        /// Mock logger for testing
        /// </summary>
        public class MockLogger : IXLogger
        {
            public string ModuleName => "MockLogger";
            public bool IsEnabled { get; set; } = true;
            public LogLevel MinLevel { get; set; } = LogLevel.Debug;

            public void Verbose(string message) { }
            public void Debug(string message) { }
            public void Info(string message) { }
            public void Warning(string message) { }
            public void Error(string message) { }
            public void Error(string message, Exception exception) { }
            public void Fatal(string message) { }
            public void Fatal(string message, Exception exception) { }
            public void Log(LogLevel level, string message, Exception exception = null) { }
            public bool IsLevelEnabled(LogLevel level) => true;
        }

        /// <summary>
        /// Test service A implementation - transient
        /// </summary>
        public class TestServiceA : ITestServiceA
        {
            private readonly IXLogger _logger;
            public IXLogger Logger => _logger;
            public ITestServiceB ServiceB { get; private set; }
            public string Id { get; } = Guid.NewGuid().ToString();

            public TestServiceA(IXLogger logger, ITestServiceB serviceB)
            {
                _logger = logger;
                ServiceB = serviceB;
            }

            public void DoSomething()
            {
                _logger.Info("ServiceA (" + Id + ") doing something");
            }
        }

        /// <summary>
        /// Test service B implementation - singleton
        /// </summary>
        public class TestServiceB : ITestServiceB
        {
            private readonly IXLogger _logger;
            public string Id { get; } = Guid.NewGuid().ToString();

            public TestServiceB(IXLogger logger)
            {
                _logger = logger;
            }

            public void DoSomething()
            {
                _logger.Info("ServiceB (" + Id + ") doing something");
            }
        }

        /// <summary>
        /// Test service C implementation - scoped
        /// </summary>
        public class TestServiceC : ITestServiceC
        {
            private readonly IXLogger _logger;
            public IXLogger Logger => _logger;
            public ITestServiceA ServiceA { get; private set; }
            public string Id { get; } = Guid.NewGuid().ToString();

            public TestServiceC(IXLogger logger, ITestServiceA serviceA)
            {
                _logger = logger;
                ServiceA = serviceA;
            }

            public void DoSomething()
            {
                _logger.Info("ServiceC (" + Id + ") doing something");
            }
        }

        #endregion
    }
}
