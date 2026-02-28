using VContainer;

namespace xFrame.Runtime.Networking
{
    /// <summary>
    /// 网络模块注册扩展。
    /// </summary>
    public static class NetworkingServiceExtensions
    {
        /// <summary>
        /// 注册网络模块默认服务。
        /// </summary>
        public static void RegisterNetworkingModule(this IContainerBuilder builder)
        {
            builder.Register<NetworkClientOptions>(Lifetime.Singleton).AsSelf();
            builder.Register<NullNetworkClient>(Lifetime.Singleton)
                .As<INetworkClient>()
                .AsSelf();
        }
    }
}
