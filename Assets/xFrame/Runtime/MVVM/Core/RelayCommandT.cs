using System;

namespace xFrame.Runtime.MVVM.Core
{
    /// <summary>
    /// 单参数命令实现。
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    public sealed class RelayCommand<T>
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(T parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(T parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            _execute(parameter);
        }
    }
}
