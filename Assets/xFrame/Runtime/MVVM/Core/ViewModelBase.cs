using System;
using System.Collections.Generic;

namespace xFrame.Runtime.MVVM.Core
{
    /// <summary>
    /// ViewModel 基类。
    /// 提供可释放资源统一管理能力。
    /// </summary>
    public abstract class ViewModelBase : IDisposable
    {
        private readonly List<IDisposable> _disposables = new();
        private bool _disposed;

        /// <summary>
        /// 注册需要在销毁时释放的对象。
        /// </summary>
        protected void AddDisposable(IDisposable disposable)
        {
            if (disposable == null || _disposed)
            {
                return;
            }

            _disposables.Add(disposable);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            for (int i = _disposables.Count - 1; i >= 0; i--)
            {
                _disposables[i]?.Dispose();
            }

            _disposables.Clear();
            OnDispose();
        }

        /// <summary>
        /// 子类释放扩展点。
        /// </summary>
        protected virtual void OnDispose()
        {
        }
    }
}
