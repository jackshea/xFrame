using NUnit.Framework;
using VContainer;
using xFrame.Runtime.Networking;

namespace xFrame.Tests
{
    [TestFixture]
    public class NetworkingSmokeTests
    {
        [Test]
        public void NullNetworkClient_DefaultState_ShouldBeDisconnected()
        {
            var builder = new ContainerBuilder();
            builder.RegisterNetworkingModule();
            var resolver = builder.Build();

            var client = resolver.Resolve<INetworkClient>();

            Assert.IsNotNull(client);
            Assert.IsFalse(client.IsConnected);
        }
    }
}