using VContainer;

namespace xFrame.Runtime.Serialization
{
    /// <summary>
    /// 序列化模块的VContainer注册扩展方法
    /// </summary>
    public static class SerializationServiceExtensions
    {
        /// <summary>
        /// 注册序列化模块到VContainer容器
        /// </summary>
        /// <param name="builder">容器构建器</param>
        public static void RegisterSerializationModule(this IContainerBuilder builder)
        {
            // 注册序列化管理器为单例
            builder.Register<ISerializerManager, SerializerManager>(Lifetime.Singleton);

            // 注册默认的JSON序列化器为单例
            builder.Register<JsonSerializer>(Lifetime.Singleton);

            // 注册序列化器工厂方法，用于获取指定名称的序列化器
            builder.RegisterFactory<string, ISerializer>(container =>
            {
                var manager = container.Resolve<ISerializerManager>();
                return serializerName => manager.GetSerializer(serializerName);
            }, Lifetime.Singleton);
        }

        /// <summary>
        /// 注册自定义序列化器到VContainer容器
        /// </summary>
        /// <typeparam name="TSerializer">序列化器类型</typeparam>
        /// <param name="builder">容器构建器</param>
        public static void RegisterSerializer<TSerializer>(this IContainerBuilder builder)
            where TSerializer : class, ISerializer
        {
            builder.Register<TSerializer>(Lifetime.Singleton);
        }
    }
}
