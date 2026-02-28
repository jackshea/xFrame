using System;

namespace xFrame.Runtime.MVVM.Core
{
    /// <summary>
    /// 命令接口。
    /// 封装 View 触发的操作入口。
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 命令是否可执行。
        /// </summary>
        bool CanExecute();

        /// <summary>
        /// 执行命令。
        /// </summary>
        void Execute();

        /// <summary>
        /// 可执行状态变化事件。
        /// </summary>
        event Action OnCanExecuteChanged;
    }
}
