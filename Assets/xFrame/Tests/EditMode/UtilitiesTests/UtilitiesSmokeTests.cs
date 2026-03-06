using System;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using VContainer;
using xFrame.Runtime.Utilities;

namespace xFrame.Tests
{
    [TestFixture]
    public class UtilitiesSmokeTests
    {
        [Test]
        public void GuidService_NewGuid_ShouldBeParsable()
        {
            var builder = new ContainerBuilder();
            builder.RegisterUtilitiesModule();
            var resolver = builder.Build();

            var guidService = resolver.Resolve<IGuidService>();
            var value = guidService.NewGuid();

            var parsed = guidService.TryParse(value, out var guid);

            Assert.IsTrue(parsed);
            Assert.AreNotEqual(Guid.Empty, guid);
        }

        [Test]
        public void RetryUtility_ExecuteAsync_ShouldRetryUntilSuccess()
        {
            var attempts = 0;

            var result = RetryUtility.ExecuteAsync(async () =>
            {
                attempts++;
                if (attempts < 3) throw new InvalidOperationException("failed");

                return await UniTask.FromResult(7);
            }, 3, TimeSpan.Zero).GetAwaiter().GetResult();

            Assert.AreEqual(7, result);
            Assert.AreEqual(3, attempts);
        }
    }
}