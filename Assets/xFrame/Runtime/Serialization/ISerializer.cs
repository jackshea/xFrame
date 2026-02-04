using System;

namespace xFrame.Runtime.Serialization
{
    /// <summary>
    /// 序列化器接口
    /// 定义统一的序列化和反序列化操作
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// 序列化器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 将对象序列化为字节数组
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>序列化后的字节数组</returns>
        byte[] Serialize<T>(T obj);

        /// <summary>
        /// 将对象序列化为字符串
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>序列化后的字符串</returns>
        string SerializeToString<T>(T obj);

        /// <summary>
        /// 从字节数组反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="data">字节数组</param>
        /// <returns>反序列化后的对象</returns>
        T Deserialize<T>(byte[] data);

        /// <summary>
        /// 从字符串反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="data">字符串数据</param>
        /// <returns>反序列化后的对象</returns>
        T DeserializeFromString<T>(string data);

        /// <summary>
        /// 从字节数组反序列化为指定类型的对象
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="data">字节数组</param>
        /// <returns>反序列化后的对象</returns>
        object Deserialize(Type type, byte[] data);

        /// <summary>
        /// 从字符串反序列化为指定类型的对象
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="data">字符串数据</param>
        /// <returns>反序列化后的对象</returns>
        object DeserializeFromString(Type type, string data);
    }
}
