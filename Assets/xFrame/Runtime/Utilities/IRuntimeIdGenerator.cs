namespace xFrame.Runtime.Utilities
{
    /// <summary>
    ///     运行时全局ID生成器接口。
    ///     为进程生命周期内的对象提供单调递增且全局唯一的数字ID。
    /// </summary>
    public interface IRuntimeIdGenerator
    {
        /// <summary>
        ///     生成下一个全局唯一ID。
        ///     首次调用返回0，后续每次调用递增1。
        /// </summary>
        /// <returns>当前进程生命周期内唯一的运行时ID。</returns>
        long NextId();
    }
}
