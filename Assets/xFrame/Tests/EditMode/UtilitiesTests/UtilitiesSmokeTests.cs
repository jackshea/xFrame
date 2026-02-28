using System;
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

            IGuidService guidService = resolver.Resolve<IGuidService>();
            string value = guidService.NewGuid();

            bool parsed = guidService.TryParse(value, out Guid guid);

            Assert.IsTrue(parsed);
            Assert.AreNotEqual(Guid.Empty, guid);
        }

        [Test]
        public void RetryUtility_ExecuteAsync_ShouldRetryUntilSuccess()
        {
            int attempts = 0;

            int result = RetryUtility.ExecuteAsync(async () =>
            {
                await Cysharp.Threading.Tasks.UniTask.Yield();
                attempts++;
                if (attempts < 3)
                {
                    throw new InvalidOperationException("failed");
                }

                return 7;
            }, 3, TimeSpan.Zero).GetAwaiter().GetResult();

            Assert.AreEqual(7, result);
            Assert.AreEqual(3, attempts);
        }
    }
}
