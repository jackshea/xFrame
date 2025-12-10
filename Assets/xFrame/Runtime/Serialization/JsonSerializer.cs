using System;
using System.Text;
using UnityEngine;

namespace xFrame.Runtime.Serialization
{
    /// <summary>
    /// 基于Unity JsonUtility的JSON序列化器实现
    /// 作为默认的序列化器使用
    /// </summary>
    public class JsonSerializer : ISerializer
    {
        /// <summary>
        /// 序列化器名称常量
        /// </summary>
        public const string SerializerName = "Json";

        /// <summary>
        /// 是否使用格式化输出（美化JSON）
        /// </summary>
        private readonly bool _prettyPrint;

        /// <summary>
        /// 序列化器名称
        /// </summary>
        public string Name => SerializerName;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="prettyPrint">是否使用格式化输出</param>
        public JsonSerializer(bool prettyPrint = false)
        {
            _prettyPrint = prettyPrint;
        }

        /// <summary>
        /// 将对象序列化为字节数组
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>序列化后的字节数组</returns>
        public byte[] Serialize<T>(T obj)
        {
            var json = SerializeToString(obj);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// 将对象序列化为字符串
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>序列化后的字符串</returns>
        public string SerializeToString<T>(T obj)
        {
            if (obj == null)
            {
                return "null";
            }

            return JsonUtility.ToJson(obj, _prettyPrint);
        }

        /// <summary>
        /// 从字节数组反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="data">字节数组</param>
        /// <returns>反序列化后的对象</returns>
        public T Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return default;
            }

            var json = Encoding.UTF8.GetString(data);
            return DeserializeFromString<T>(json);
        }

        /// <summary>
        /// 从字符串反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="data">字符串数据</param>
        /// <returns>反序列化后的对象</returns>
        public T DeserializeFromString<T>(string data)
        {
            if (string.IsNullOrEmpty(data) || data == "null")
            {
                return default;
            }

            return JsonUtility.FromJson<T>(data);
        }

        /// <summary>
        /// 从字节数组反序列化为指定类型的对象
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="data">字节数组</param>
        /// <returns>反序列化后的对象</returns>
        public object Deserialize(Type type, byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }

            var json = Encoding.UTF8.GetString(data);
            return DeserializeFromString(type, json);
        }

        /// <summary>
        /// 从字符串反序列化为指定类型的对象
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="data">字符串数据</param>
        /// <returns>反序列化后的对象</returns>
        public object DeserializeFromString(Type type, string data)
        {
            if (string.IsNullOrEmpty(data) || data == "null")
            {
                return null;
            }

            return JsonUtility.FromJson(data, type);
        }
    }
}
