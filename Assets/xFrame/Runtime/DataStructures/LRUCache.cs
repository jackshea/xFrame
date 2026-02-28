using System;
using System.Collections.Generic;

namespace xFrame.Runtime.DataStructures
{
    /// <summary>
    /// LRU缓存实现
    /// 使用双向链表和哈希表实现O(1)的Get和Put操作
    /// </summary>
    /// <typeparam name="TKey">键的类型</typeparam>
    /// <typeparam name="TValue">值的类型</typeparam>
    public class LRUCache<TKey, TValue> : ILRUCache<TKey, TValue>
    {
        private readonly Dictionary<TKey, Node> _cache;

        private readonly Node _head;
        private readonly Node _tail;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="capacity">缓存的最大容量，必须大于0</param>
        /// <exception cref="ArgumentException">当容量小于等于0时抛出</exception>
        public LRUCache(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("容量必须大于0", nameof(capacity));

            Capacity = capacity;
            _cache = new Dictionary<TKey, Node>();

            // 创建虚拟头尾节点，简化链表操作
            _head = new Node(default, default);
            _tail = new Node(default, default);
            _head.Next = _tail;
            _tail.Previous = _head;
        }

        /// <summary>
        /// 缓存的最大容量
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// 当前缓存中的元素数量
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// 获取所有键的集合（按最近使用顺序排列，最近使用的在前）
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                var current = _head.Next;
                while (current != _tail)
                {
                    yield return current.Key;
                    current = current.Next;
                }
            }
        }

        /// <summary>
        /// 获取所有值的集合（按最近使用顺序排列，最近使用的在前）
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                var current = _head.Next;
                while (current != _tail)
                {
                    yield return current.Value;
                    current = current.Next;
                }
            }
        }

        /// <summary>
        /// 获取指定键对应的值
        /// </summary>
        /// <param name="key">要查找的键</param>
        /// <param name="value">输出参数，如果找到则包含对应的值</param>
        /// <returns>如果找到键则返回true，否则返回false</returns>
        public bool TryGet(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                // 将访问的节点移到链表头部（最近使用位置）
                MoveToHead(node);
                value = node.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 获取指定键对应的值
        /// </summary>
        /// <param name="key">要查找的键</param>
        /// <returns>如果找到键则返回对应的值，否则抛出异常</returns>
        /// <exception cref="KeyNotFoundException">当键不存在时抛出</exception>
        public TValue Get(TKey key)
        {
            if (TryGet(key, out var value)) return value;

            throw new KeyNotFoundException($"键 '{key}' 在缓存中不存在");
        }

        /// <summary>
        /// 设置指定键的值
        /// </summary>
        /// <param name="key">要设置的键</param>
        /// <param name="value">要设置的值</param>
        public void Put(TKey key, TValue value)
        {
            if (_cache.TryGetValue(key, out var existingNode))
            {
                // 键已存在，更新值并移到头部
                existingNode.Value = value;
                MoveToHead(existingNode);
            }
            else
            {
                // 键不存在，创建新节点
                var newNode = new Node(key, value);

                // 检查是否需要淘汰最久未使用的项
                if (_cache.Count >= Capacity) RemoveLeastRecentlyUsed();

                // 添加新节点到头部
                _cache[key] = newNode;
                AddToHead(newNode);
            }
        }

        /// <summary>
        /// 检查缓存中是否包含指定的键
        /// </summary>
        /// <param name="key">要检查的键</param>
        /// <returns>如果包含键则返回true，否则返回false</returns>
        public bool ContainsKey(TKey key)
        {
            return _cache.ContainsKey(key);
        }

        /// <summary>
        /// 从缓存中移除指定的键
        /// </summary>
        /// <param name="key">要移除的键</param>
        /// <returns>如果成功移除则返回true，否则返回false</returns>
        public bool Remove(TKey key)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                _cache.Remove(key);
                RemoveNode(node);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 清空缓存中的所有元素
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            _head.Next = _tail;
            _tail.Previous = _head;
        }

        /// <summary>
        /// 将节点添加到链表头部（最近使用位置）
        /// </summary>
        /// <param name="node">要添加的节点</param>
        private void AddToHead(Node node)
        {
            node.Previous = _head;
            node.Next = _head.Next;
            _head.Next.Previous = node;
            _head.Next = node;
        }

        /// <summary>
        /// 从链表中移除指定节点
        /// </summary>
        /// <param name="node">要移除的节点</param>
        private void RemoveNode(Node node)
        {
            node.Previous.Next = node.Next;
            node.Next.Previous = node.Previous;
        }

        /// <summary>
        /// 将节点移动到链表头部
        /// </summary>
        /// <param name="node">要移动的节点</param>
        private void MoveToHead(Node node)
        {
            RemoveNode(node);
            AddToHead(node);
        }

        /// <summary>
        /// 移除最久未使用的项（链表尾部的节点）
        /// </summary>
        private void RemoveLeastRecentlyUsed()
        {
            var lastNode = _tail.Previous;
            if (lastNode != _head)
            {
                _cache.Remove(lastNode.Key);
                RemoveNode(lastNode);
            }
        }

        /// <summary>
        /// 获取缓存的统计信息
        /// </summary>
        /// <returns>包含容量、当前数量等信息的字符串</returns>
        public override string ToString()
        {
            return $"LRUCache[Count={Count}, Capacity={Capacity}]";
        }

        /// <summary>
        /// 双向链表节点
        /// </summary>
        private class Node
        {
            public Node(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }

            public TKey Key { get; }
            public TValue Value { get; set; }
            public Node Previous { get; set; }
            public Node Next { get; set; }
        }
    }
}