using System;
using System.Threading;

namespace xFrame.Runtime.Utilities
{
    /// <summary>
    ///     运行时全局ID生成器默认实现。
    ///     该实现仅保证单次游戏运行期间唯一，不做持久化，重启后重新从0开始计数。
    /// </summary>
    public sealed class RuntimeIdGenerator : IRuntimeIdGenerator
    {
        private long _nextValue;

        /// <summary>
        ///     构造运行时ID生成器。
        ///     通过将内部游标初始化为-1，保证首次生成的ID为0。
        /// </summary>
        public RuntimeIdGenerator()
        {
            _nextValue = -1;
        }

        /// <summary>
        ///     生成下一个全局唯一ID。
        ///     使用原子递增保证多线程环境下不会产生重复值。
        /// </summary>
        /// <returns>当前进程生命周期内唯一的运行时ID。</returns>
        public long NextId()
        {
            while (true)
            {
                var currentValue = Interlocked.Read(ref _nextValue);
                if (currentValue == long.MaxValue)
                {
                    throw new OverflowException("运行时ID已达到 long.MaxValue，无法继续分配新的唯一ID。");
                }

                var nextValue = currentValue + 1;
                var originalValue = Interlocked.CompareExchange(ref _nextValue, nextValue, currentValue);
                if (originalValue == currentValue)
                {
                    return nextValue;
                }
            }
        }
    }
}
