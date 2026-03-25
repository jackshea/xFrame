using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using VContainer;
using xFrame.Runtime.Utilities;

namespace xFrame.Tests
{
    /// <summary>
    ///     运行时ID生成器测试。
    ///     验证默认注册、递增顺序和并发唯一性，防止回归破坏全局唯一约束。
    /// </summary>
    [TestFixture]
    public class RuntimeIdGeneratorTests
    {
        /// <summary>
        ///     验证首次生成的ID从0开始，并按调用顺序递增。
        /// </summary>
        [Test]
        public void NextId_ShouldStartFromZero()
        {
            var generator = new RuntimeIdGenerator();

            Assert.AreEqual(0L, generator.NextId());
            Assert.AreEqual(1L, generator.NextId());
            Assert.AreEqual(2L, generator.NextId());
        }

        /// <summary>
        ///     验证工具模块注册后可解析运行时ID生成器，并在容器单例内维持全局递增状态。
        /// </summary>
        [Test]
        public void RegisterUtilitiesModule_ShouldResolveSingletonRuntimeIdGenerator()
        {
            var builder = new ContainerBuilder();
            builder.RegisterUtilitiesModule();
            var resolver = builder.Build();

            var generatorA = resolver.Resolve<IRuntimeIdGenerator>();
            var generatorB = resolver.Resolve<IRuntimeIdGenerator>();

            Assert.AreSame(generatorA, generatorB);
            Assert.AreEqual(0L, generatorA.NextId());
            Assert.AreEqual(1L, generatorB.NextId());
        }

        /// <summary>
        ///     验证并发生成场景下不会产生重复ID。
        /// </summary>
        [Test]
        public void NextId_ShouldBeUniqueAcrossConcurrentCalls()
        {
            const int count = 1000;
            var generator = new RuntimeIdGenerator();
            var results = new ConcurrentBag<long>();

            Parallel.For(0, count, _ =>
            {
                results.Add(generator.NextId());
            });

            var orderedResults = results.OrderBy(value => value).ToArray();

            Assert.AreEqual(count, orderedResults.Length);
            CollectionAssert.AreEqual(Enumerable.Range(0, count).Select(value => (long)value).ToArray(), orderedResults);
        }
    }
}
