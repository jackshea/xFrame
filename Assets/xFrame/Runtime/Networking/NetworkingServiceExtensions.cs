using VContainer;
using xFrame.Runtime.Networking.AgentBridge;
using xFrame.Runtime.Networking.AgentBridge.Commands;

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

            builder.Register<AgentBridgeOptions>(Lifetime.Singleton).AsSelf();
            builder.Register<AgentCommandRegistry>(Lifetime.Singleton).AsSelf();
            builder.Register<AgentReflectionInvoker>(Lifetime.Singleton).AsSelf();
            builder.Register<AgentRpcRouter>(Lifetime.Singleton).AsSelf();

            builder.Register<PingCommandHandler>(Lifetime.Singleton).AsSelf().As<IAgentRpcCommandHandler>();
            builder.Register<AuthenticateCommandHandler>(Lifetime.Singleton).AsSelf().As<IAgentRpcCommandHandler>();
            builder.Register<ListCommandsHandler>(Lifetime.Singleton).AsSelf().As<IAgentRpcCommandHandler>();
            builder.Register<FindGameObjectCommandHandler>(Lifetime.Singleton).AsSelf().As<IAgentRpcCommandHandler>();
            builder.Register<InvokeComponentCommandHandler>(Lifetime.Singleton).AsSelf().As<IAgentRpcCommandHandler>();
        }
    }
}
