using System;
using System.Collections.Generic;
using UnityEngine;
using xFrame.Runtime.EventBus;

namespace xFrame.Runtime.Serialization
{
    /// <summary>
    /// 序列化管理器实现
    /// 管理多个序列化器，提供统一的序列化服务入口
    /// </summary>
    public class SerializerManager : ISerializerManager
    {
        /// <summary>
        /// 已注册的序列化器字典
        /// </summary>
        private readonly Dictionary<string, ISerializer> _serializers = new Dictionary<string, ISerializer>();

        /// <summary>
        /// 默认序列化器名称
        /// </summary>
        private string _defaultSerializerName;

        /// <summary>
        /// 默认序列化器
        /// </summary>
        public ISerializer DefaultSerializer
        {
            get
            {
                if (string.IsNullOrEmpty(_defaultSerializerName))
                {
                    return null;
                }

                _serializers.TryGetValue(_defaultSerializerName, out var serializer);
                return serializer;
            }
        }

        /// <summary>
        /// 构造函数
        /// 自动注册默认的JSON序列化器
        /// </summary>
        public SerializerManager()
        {
            // 注册默认的JSON序列化器
            var jsonSerializer = new JsonSerializer();
            RegisterSerializer(JsonSerializer.SerializerName, jsonSerializer);
            SetDefaultSerializer(JsonSerializer.SerializerName);
        }

        /// <summary>
        /// 注册序列化器
        /// </summary>
        /// <param name="name">序列化器名称</param>
        /// <param name="serializer">序列化器实例</param>
        public void RegisterSerializer(string name, ISerializer serializer)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), "序列化器名称不能为空");
            }

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer), "序列化器实例不能为空");
            }

            if (_serializers.ContainsKey(name))
            {
                Debug.LogWarning($"[SerializerManager] 序列化器 '{name}' 已存在，将被覆盖");
            }

            _serializers[name] = serializer;
            Debug.Log($"[SerializerManager] 注册序列化器: {name}");

            // 触发序列化器注册事件
            xFrameEventBus.Raise(new SerializerRegisteredEvent(name, serializer));
        }

        /// <summary>
        /// 注销序列化器
        /// </summary>
        /// <param name="name">序列化器名称</param>
        /// <returns>是否成功注销</returns>
        public bool UnregisterSerializer(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (_serializers.Remove(name))
            {
                // 如果注销的是默认序列化器，清空默认设置
                if (_defaultSerializerName == name)
                {
                    _defaultSerializerName = null;
                    Debug.LogWarning($"[SerializerManager] 默认序列化器 '{name}' 已被注销");
                }

                Debug.Log($"[SerializerManager] 注销序列化器: {name}");

                // 触发序列化器注销事件
                xFrameEventBus.Raise(new SerializerUnregisteredEvent(name));
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取指定名称的序列化器
        /// </summary>
        /// <param name="name">序列化器名称</param>
        /// <returns>序列化器实例，如果不存在则返回null</returns>
        public ISerializer GetSerializer(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            _serializers.TryGetValue(name, out var serializer);
            return serializer;
        }

        /// <summary>
        /// 设置默认序列化器
        /// </summary>
        /// <param name="name">序列化器名称</param>
        public void SetDefaultSerializer(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), "序列化器名称不能为空");
            }

            if (!_serializers.ContainsKey(name))
            {
                throw new InvalidOperationException($"序列化器 '{name}' 未注册");
            }

            var previousDefault = _defaultSerializerName;
            _defaultSerializerName = name;
            Debug.Log($"[SerializerManager] 设置默认序列化器: {name}");

            // 触发默认序列化器变更事件
            xFrameEventBus.Raise(new DefaultSerializerChangedEvent(previousDefault, name));
        }

        /// <summary>
        /// 使用默认序列化器将对象序列化为字节数组
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>序列化后的字节数组</returns>
        public byte[] Serialize<T>(T obj)
        {
            var serializer = GetDefaultSerializerOrThrow();
            return serializer.Serialize(obj);
        }

        /// <summary>
        /// 使用默认序列化器将对象序列化为字符串
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>序列化后的字符串</returns>
        public string SerializeToString<T>(T obj)
        {
            var serializer = GetDefaultSerializerOrThrow();
            return serializer.SerializeToString(obj);
        }

        /// <summary>
        /// 使用默认序列化器从字节数组反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="data">字节数组</param>
        /// <returns>反序列化后的对象</returns>
        public T Deserialize<T>(byte[] data)
        {
            var serializer = GetDefaultSerializerOrThrow();
            return serializer.Deserialize<T>(data);
        }

        /// <summary>
        /// 使用默认序列化器从字符串反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="data">字符串数据</param>
        /// <returns>反序列化后的对象</returns>
        public T DeserializeFromString<T>(string data)
        {
            var serializer = GetDefaultSerializerOrThrow();
            return serializer.DeserializeFromString<T>(data);
        }

        /// <summary>
        /// 使用指定序列化器将对象序列化为字节数组
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="serializerName">序列化器名称</param>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>序列化后的字节数组</returns>
        public byte[] Serialize<T>(string serializerName, T obj)
        {
            var serializer = GetSerializerOrThrow(serializerName);
            return serializer.Serialize(obj);
        }

        /// <summary>
        /// 使用指定序列化器将对象序列化为字符串
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="serializerName">序列化器名称</param>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>序列化后的字符串</returns>
        public string SerializeToString<T>(string serializerName, T obj)
        {
            var serializer = GetSerializerOrThrow(serializerName);
            return serializer.SerializeToString(obj);
        }

        /// <summary>
        /// 使用指定序列化器从字节数组反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="serializerName">序列化器名称</param>
        /// <param name="data">字节数组</param>
        /// <returns>反序列化后的对象</returns>
        public T Deserialize<T>(string serializerName, byte[] data)
        {
            var serializer = GetSerializerOrThrow(serializerName);
            return serializer.Deserialize<T>(data);
        }

        /// <summary>
        /// 使用指定序列化器从字符串反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="serializerName">序列化器名称</param>
        /// <param name="data">字符串数据</param>
        /// <returns>反序列化后的对象</returns>
        public T DeserializeFromString<T>(string serializerName, string data)
        {
            var serializer = GetSerializerOrThrow(serializerName);
            return serializer.DeserializeFromString<T>(data);
        }

        /// <summary>
        /// 获取默认序列化器，如果不存在则抛出异常
        /// </summary>
        /// <returns>默认序列化器</returns>
        private ISerializer GetDefaultSerializerOrThrow()
        {
            var serializer = DefaultSerializer;
            if (serializer == null)
            {
                throw new InvalidOperationException("未设置默认序列化器");
            }

            return serializer;
        }

        /// <summary>
        /// 获取指定名称的序列化器，如果不存在则抛出异常
        /// </summary>
        /// <param name="name">序列化器名称</param>
        /// <returns>序列化器实例</returns>
        private ISerializer GetSerializerOrThrow(string name)
        {
            var serializer = GetSerializer(name);
            if (serializer == null)
            {
                throw new InvalidOperationException($"序列化器 '{name}' 未注册");
            }

            return serializer;
        }
    }
}
