using System;

namespace xFrame.Runtime.Serialization
{
    /// <summary>
    /// 序列化管理器接口
    /// 提供统一的序列化服务入口，支持多种序列化器
    /// </summary>
    public interface ISerializerManager
    {
        /// <summary>
        /// 默认序列化器
        /// </summary>
        ISerializer DefaultSerializer { get; }

        /// <summary>
        /// 注册序列化器
        /// </summary>
        /// <param name="name">序列化器名称</param>
        /// <param name="serializer">序列化器实例</param>
        void RegisterSerializer(string name, ISerializer serializer);

        /// <summary>
        /// 注销序列化器
        /// </summary>
        /// <param name="name">序列化器名称</param>
        /// <returns>是否成功注销</returns>
        bool UnregisterSerializer(string name);

        /// <summary>
        /// 获取指定名称的序列化器
        /// </summary>
        /// <param name="name">序列化器名称</param>
        /// <returns>序列化器实例，如果不存在则返回null</returns>
        ISerializer GetSerializer(string name);

        /// <summary>
        /// 设置默认序列化器
        /// </summary>
        /// <param name="name">序列化器名称</param>
        void SetDefaultSerializer(string name);

        /// <summary>
        /// 使用默认序列化器将对象序列化为字节数组
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>序列化后的字节数组</returns>
        byte[] Serialize<T>(T obj);

        /// <summary>
        /// 使用默认序列化器将对象序列化为字符串
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>序列化后的字符串</returns>
        string SerializeToString<T>(T obj);

        /// <summary>
        /// 使用默认序列化器从字节数组反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="data">字节数组</param>
        /// <returns>反序列化后的对象</returns>
        T Deserialize<T>(byte[] data);

        /// <summary>
        /// 使用默认序列化器从字符串反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="data">字符串数据</param>
        /// <returns>反序列化后的对象</returns>
        T DeserializeFromString<T>(string data);

        /// <summary>
        /// 使用指定序列化器将对象序列化为字节数组
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="serializerName">序列化器名称</param>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>序列化后的字节数组</returns>
        byte[] Serialize<T>(string serializerName, T obj);

        /// <summary>
        /// 使用指定序列化器将对象序列化为字符串
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="serializerName">序列化器名称</param>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>序列化后的字符串</returns>
        string SerializeToString<T>(string serializerName, T obj);

        /// <summary>
        /// 使用指定序列化器从字节数组反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="serializerName">序列化器名称</param>
        /// <param name="data">字节数组</param>
        /// <returns>反序列化后的对象</returns>
        T Deserialize<T>(string serializerName, byte[] data);

        /// <summary>
        /// 使用指定序列化器从字符串反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="serializerName">序列化器名称</param>
        /// <param name="data">字符串数据</param>
        /// <returns>反序列化后的对象</returns>
        T DeserializeFromString<T>(string serializerName, string data);
    }
}
