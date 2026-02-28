using System;
using System.Collections.Generic;

namespace xFrame.Runtime.MVVM.Core
{
    /// <summary>
    /// 绑定上下文。
    /// 统一管理 View 层绑定句柄，避免遗忘解绑。
    /// </summary>
    public sealed class BindingContext : IDisposable
    {
        private readonly List<IDisposable> _bindings = new();

        /// <summary>
        /// 添加绑定句柄。
        /// </summary>
        public void Add(IDisposable binding)
        {
            if (binding == null)
            {
                return;
            }

            _bindings.Add(binding);
        }

        public void Dispose()
        {
            for (int i = _bindings.Count - 1; i >= 0; i--)
            {
                _bindings[i]?.Dispose();
            }

            _bindings.Clear();
        }
    }
}
