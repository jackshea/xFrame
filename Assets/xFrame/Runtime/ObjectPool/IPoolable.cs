namespace xFrame.Runtime.ObjectPool
{
    /// <summary>
    /// 池化对象接口
    /// 实现此接口的对象可以自定义在对象池中的生命周期行为
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 当对象从池中获取时调用
        /// 用于重置对象状态，准备重新使用
        /// </summary>
        void OnGet();

        /// <summary>
        /// 当对象被释放回池中时调用
        /// 用于清理对象状态，准备下次使用
        /// </summary>
        void OnRelease();

        /// <summary>
        /// 当对象被销毁时调用
        /// 用于释放对象持有的资源
        /// </summary>
        void OnDestroy();
    }
}