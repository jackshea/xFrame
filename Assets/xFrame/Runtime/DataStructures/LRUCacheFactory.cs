namespace xFrame.Runtime.DataStructures
{
    /// <summary>
    /// LRU缓存工厂类
    /// 提供便捷的LRU缓存创建方法
    /// </summary>
    public static class LRUCacheFactory
    {
        /// <summary>
        /// 创建一个基本的LRU缓存
        /// </summary>
        /// <typeparam name="TKey">键的类型</typeparam>
        /// <typeparam name="TValue">值的类型</typeparam>
        /// <param name="capacity">缓存容量</param>
        /// <returns>LRU缓存实例</returns>
        public static ILRUCache<TKey, TValue> Create<TKey, TValue>(int capacity)
        {
            return new LRUCache<TKey, TValue>(capacity);
        }

        /// <summary>
        /// 创建一个字符串键的LRU缓存
        /// </summary>
        /// <typeparam name="TValue">值的类型</typeparam>
        /// <param name="capacity">缓存容量</param>
        /// <returns>LRU缓存实例</returns>
        public static ILRUCache<string, TValue> CreateStringCache<TValue>(int capacity)
        {
            return new LRUCache<string, TValue>(capacity);
        }

        /// <summary>
        /// 创建一个整数键的LRU缓存
        /// </summary>
        /// <typeparam name="TValue">值的类型</typeparam>
        /// <param name="capacity">缓存容量</param>
        /// <returns>LRU缓存实例</returns>
        public static ILRUCache<int, TValue> CreateIntCache<TValue>(int capacity)
        {
            return new LRUCache<int, TValue>(capacity);
        }

        /// <summary>
        /// 创建一个字符串到字符串的LRU缓存
        /// </summary>
        /// <param name="capacity">缓存容量</param>
        /// <returns>LRU缓存实例</returns>
        public static ILRUCache<string, string> CreateStringToStringCache(int capacity)
        {
            return new LRUCache<string, string>(capacity);
        }

        /// <summary>
        /// 创建一个字符串到对象的LRU缓存
        /// </summary>
        /// <param name="capacity">缓存容量</param>
        /// <returns>LRU缓存实例</returns>
        public static ILRUCache<string, object> CreateStringToObjectCache(int capacity)
        {
            return new LRUCache<string, object>(capacity);
        }
    }
}