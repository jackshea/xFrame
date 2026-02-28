using VContainer;

namespace xFrame.Runtime.Platform
{
    /// <summary>
    /// 平台模块注册扩展。
    /// </summary>
    public static class PlatformServiceExtensions
    {
        /// <summary>
        /// 注册平台模块默认服务。
        /// </summary>
        public static void RegisterPlatformModule(this IContainerBuilder builder)
        {
            builder.Register<UnityPlatformService>(Lifetime.Singleton)
                .As<IPlatformService>()
                .AsSelf();
        }
    }
}
