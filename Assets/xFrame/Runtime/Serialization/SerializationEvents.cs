using xFrame.Runtime.EventBus;

namespace xFrame.Runtime.Serialization
{
    /// <summary>
    /// 序列化器注册事件
    /// 当新的序列化器被注册时触发
    /// </summary>
    public struct SerializerRegisteredEvent : IEvent
    {
        /// <summary>
        /// 序列化器名称
        /// </summary>
        public string SerializerName { get; }

        /// <summary>
        /// 序列化器实例
        /// </summary>
        public ISerializer Serializer { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serializerName">序列化器名称</param>
        /// <param name="serializer">序列化器实例</param>
        public SerializerRegisteredEvent(string serializerName, ISerializer serializer)
        {
            SerializerName = serializerName;
            Serializer = serializer;
        }
    }

    /// <summary>
    /// 序列化器注销事件
    /// 当序列化器被注销时触发
    /// </summary>
    public struct SerializerUnregisteredEvent : IEvent
    {
        /// <summary>
        /// 序列化器名称
        /// </summary>
        public string SerializerName { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serializerName">序列化器名称</param>
        public SerializerUnregisteredEvent(string serializerName)
        {
            SerializerName = serializerName;
        }
    }

    /// <summary>
    /// 默认序列化器变更事件
    /// 当默认序列化器被更改时触发
    /// </summary>
    public struct DefaultSerializerChangedEvent : IEvent
    {
        /// <summary>
        /// 之前的默认序列化器名称
        /// </summary>
        public string PreviousSerializerName { get; }

        /// <summary>
        /// 新的默认序列化器名称
        /// </summary>
        public string NewSerializerName { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="previousSerializerName">之前的默认序列化器名称</param>
        /// <param name="newSerializerName">新的默认序列化器名称</param>
        public DefaultSerializerChangedEvent(string previousSerializerName, string newSerializerName)
        {
            PreviousSerializerName = previousSerializerName;
            NewSerializerName = newSerializerName;
        }
    }
}
