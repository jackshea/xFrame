using System;

namespace xFrame.Runtime.MVVM.Core
{
    /// <summary>
    /// 无参数命令实现。
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event Action OnCanExecuteChanged;

        public bool CanExecute()
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute()
        {
            if (!CanExecute())
            {
                return;
            }

            _execute();
        }

        /// <summary>
        /// 通知可执行状态变化。
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged?.Invoke();
        }
    }
}
