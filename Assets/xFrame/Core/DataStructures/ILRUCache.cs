using System;
using System.Collections.Generic;

namespace xFrame.Core.DataStructures
{
    /// <summary>
    /// LRU缓存接口
    /// 定义了LRU缓存的基本操作方法
    /// </summary>
    /// <typeparam name="TKey">键的类型</typeparam>
    /// <typeparam name="TValue">值的类型</typeparam>
    public interface ILRUCache<TKey, TValue>
    {
        /// <summary>
        /// 缓存的最大容量
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// 当前缓存中的元素数量
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 获取指定键对应的值
        /// </summary>
        /// <param name="key">要查找的键</param>
        /// <param name="value">输出参数，如果找到则包含对应的值</param>
        /// <returns>如果找到键则返回true，否则返回false</returns>
        bool TryGet(TKey key, out TValue value);

        /// <summary>
        /// 获取指定键对应的值
        /// </summary>
        /// <param name="key">要查找的键</param>
        /// <returns>如果找到键则返回对应的值，否则抛出异常</returns>
        /// <exception cref="KeyNotFoundException">当键不存在时抛出</exception>
        TValue Get(TKey key);

        /// <summary>
        /// 设置指定键的值
        /// 如果键已存在，则更新其值并将其移到最近使用位置
        /// 如果键不存在，则添加新的键值对
        /// 如果缓存已满，则淘汰最久未使用的项
        /// </summary>
        /// <param name="key">要设置的键</param>
        /// <param name="value">要设置的值</param>
        void Put(TKey key, TValue value);

        /// <summary>
        /// 检查缓存中是否包含指定的键
        /// </summary>
        /// <param name="key">要检查的键</param>
        /// <returns>如果包含键则返回true，否则返回false</returns>
        bool ContainsKey(TKey key);

        /// <summary>
        /// 从缓存中移除指定的键
        /// </summary>
        /// <param name="key">要移除的键</param>
        /// <returns>如果成功移除则返回true，否则返回false</returns>
        bool Remove(TKey key);

        /// <summary>
        /// 清空缓存中的所有元素
        /// </summary>
        void Clear();

        /// <summary>
        /// 获取所有键的集合（按最近使用顺序排列）
        /// </summary>
        /// <returns>键的集合</returns>
        IEnumerable<TKey> Keys { get; }

        /// <summary>
        /// 获取所有值的集合（按最近使用顺序排列）
        /// </summary>
        /// <returns>值的集合</returns>
        IEnumerable<TValue> Values { get; }
    }
}
