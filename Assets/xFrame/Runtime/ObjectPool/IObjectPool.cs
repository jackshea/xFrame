namespace xFrame.Runtime.ObjectPool
{
    /// <summary>
    /// 对象池接口
    /// 定义了对象池的基本操作方法
    /// </summary>
    /// <typeparam name="T">池化对象的类型</typeparam>
    public interface IObjectPool<T> where T : class
    {
        /// <summary>
        /// 当前池中对象数量
        /// </summary>
        int CountInPool { get; }

        /// <summary>
        /// 已创建的对象总数
        /// </summary>
        int CountAll { get; }

        /// <summary>
        /// 从对象池中获取一个对象
        /// 如果池中没有对象，将创建一个新对象
        /// </summary>
        /// <returns>池化对象实例</returns>
        T Get();

        /// <summary>
        /// 将对象归还到对象池
        /// </summary>
        /// <param name="obj">要归还的对象</param>
        void Release(T obj);

        /// <summary>
        /// 预热对象池，预先创建指定数量的对象
        /// </summary>
        /// <param name="count">要预创建的对象数量</param>
        void WarmUp(int count);

        /// <summary>
        /// 清空对象池，销毁所有池中的对象
        /// </summary>
        void Clear();

        /// <summary>
        /// 销毁对象池，释放所有资源
        /// </summary>
        void Dispose();
    }
}