using NUnit.Framework;
using VContainer;
using xFrame.Runtime.Platform;

namespace xFrame.Tests
{
    [TestFixture]
    public class PlatformSmokeTests
    {
        [Test]
        public void UnityPlatformService_GetPlatformInfo_ShouldReturnNonNullInfo()
        {
            var builder = new ContainerBuilder();
            builder.RegisterPlatformModule();
            var resolver = builder.Build();

            IPlatformService service = resolver.Resolve<IPlatformService>();

            var info = service.GetPlatformInfo();

            Assert.IsNotNull(info);
            Assert.IsNotEmpty(service.PersistentDataPath);
        }
    }
}
