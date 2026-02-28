using System;

namespace xFrame.Runtime.ObjectPool
{
    /// <summary>
    /// 对象池工厂类
    /// 提供便捷的对象池创建方法
    /// </summary>
    public static class ObjectPoolFactory
    {
        /// <summary>
        /// 创建一个基本的对象池
        /// </summary>
        /// <typeparam name="T">池化对象的类型</typeparam>
        /// <param name="createFunc">对象创建函数</param>
        /// <param name="maxSize">池的最大容量，-1表示无限制</param>
        /// <param name="threadSafe">是否启用线程安全</param>
        /// <returns>对象池实例</returns>
        public static IObjectPool<T> Create<T>(
            Func<T> createFunc,
            int maxSize = -1,
            bool threadSafe = false) where T : class
        {
            return new ObjectPool<T>(createFunc, null, null, null, maxSize, threadSafe);
        }

        /// <summary>
        /// 创建一个带有回调的对象池
        /// </summary>
        /// <typeparam name="T">池化对象的类型</typeparam>
        /// <param name="createFunc">对象创建函数</param>
        /// <param name="onGet">获取对象时的回调</param>
        /// <param name="onRelease">释放对象时的回调</param>
        /// <param name="onDestroy">销毁对象时的回调</param>
        /// <param name="maxSize">池的最大容量，-1表示无限制</param>
        /// <param name="threadSafe">是否启用线程安全</param>
        /// <returns>对象池实例</returns>
        public static IObjectPool<T> Create<T>(
            Func<T> createFunc,
            Action<T> onGet,
            Action<T> onRelease,
            Action<T> onDestroy,
            int maxSize = -1,
            bool threadSafe = false) where T : class
        {
            return new ObjectPool<T>(createFunc, onGet, onRelease, onDestroy, maxSize, threadSafe);
        }

        /// <summary>
        /// 创建一个支持重置接口的对象池
        /// 自动为实现了IPoolable接口的对象添加重置回调
        /// </summary>
        /// <typeparam name="T">池化对象的类型，必须实现IPoolable接口</typeparam>
        /// <param name="createFunc">对象创建函数</param>
        /// <param name="maxSize">池的最大容量，-1表示无限制</param>
        /// <param name="threadSafe">是否启用线程安全</param>
        /// <returns>对象池实例</returns>
        public static IObjectPool<T> CreateForPoolable<T>(
            Func<T> createFunc,
            int maxSize = -1,
            bool threadSafe = false) where T : class, IPoolable
        {
            return new ObjectPool<T>(
                createFunc,
                obj => obj.OnGet(),
                obj => obj.OnRelease(),
                obj => obj.OnDestroy(),
                maxSize,
                threadSafe);
        }

        /// <summary>
        /// 创建一个默认构造函数的对象池
        /// 适用于有无参构造函数的类型
        /// </summary>
        /// <typeparam name="T">池化对象的类型，必须有无参构造函数</typeparam>
        /// <param name="maxSize">池的最大容量，-1表示无限制</param>
        /// <param name="threadSafe">是否启用线程安全</param>
        /// <returns>对象池实例</returns>
        public static IObjectPool<T> CreateDefault<T>(
            int maxSize = -1,
            bool threadSafe = false) where T : class, new()
        {
            return new ObjectPool<T>(() => new T(), null, null, null, maxSize, threadSafe);
        }

        /// <summary>
        /// 创建一个默认构造函数且支持重置接口的对象池
        /// </summary>
        /// <typeparam name="T">池化对象的类型，必须有无参构造函数且实现IPoolable接口</typeparam>
        /// <param name="maxSize">池的最大容量，-1表示无限制</param>
        /// <param name="threadSafe">是否启用线程安全</param>
        /// <returns>对象池实例</returns>
        public static IObjectPool<T> CreateDefaultForPoolable<T>(
            int maxSize = -1,
            bool threadSafe = false) where T : class, IPoolable, new()
        {
            return new ObjectPool<T>(
                () => new T(),
                obj => obj.OnGet(),
                obj => obj.OnRelease(),
                obj => obj.OnDestroy(),
                maxSize,
                threadSafe);
        }
    }
}