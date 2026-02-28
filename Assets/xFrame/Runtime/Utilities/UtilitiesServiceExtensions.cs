using VContainer;

namespace xFrame.Runtime.Utilities
{
    /// <summary>
    /// 通用工具模块注册扩展。
    /// </summary>
    public static class UtilitiesServiceExtensions
    {
        /// <summary>
        /// 注册工具模块默认服务。
        /// </summary>
        public static void RegisterUtilitiesModule(this IContainerBuilder builder)
        {
            builder.Register<GuidService>(Lifetime.Singleton)
                .As<IGuidService>()
                .AsSelf();
        }
    }
}
