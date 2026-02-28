using System;
using System.Collections.Generic;

namespace xFrame.Runtime.MVVM.Core
{
    /// <summary>
    /// 可绑定属性。
    /// 用于在 ViewModel 与 View 之间同步数据变化。
    /// </summary>
    /// <typeparam name="T">属性值类型</typeparam>
    public class BindableProperty<T>
    {
        private T _value;
        private Action<T> _onValueChanged;

        public BindableProperty(T initialValue = default)
        {
            _value = initialValue;
        }

        /// <summary>
        /// 当前属性值。
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value))
                {
                    return;
                }

                _value = value;
                _onValueChanged?.Invoke(_value);
            }
        }

        /// <summary>
        /// 绑定监听器。
        /// </summary>
        /// <param name="listener">监听回调</param>
        /// <param name="invokeImmediately">是否立即推送当前值</param>
        /// <returns>解绑句柄</returns>
        public IDisposable Bind(Action<T> listener, bool invokeImmediately = true)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            _onValueChanged += listener;

            if (invokeImmediately)
            {
                listener(_value);
            }

            return new Subscription(this, listener);
        }

        /// <summary>
        /// 解绑监听器。
        /// </summary>
        /// <param name="listener">监听回调</param>
        public void Unbind(Action<T> listener)
        {
            if (listener == null)
            {
                return;
            }

            _onValueChanged -= listener;
        }

        private sealed class Subscription : IDisposable
        {
            private readonly BindableProperty<T> _owner;
            private Action<T> _listener;

            public Subscription(BindableProperty<T> owner, Action<T> listener)
            {
                _owner = owner;
                _listener = listener;
            }

            public void Dispose()
            {
                if (_listener == null)
                {
                    return;
                }

                _owner.Unbind(_listener);
                _listener = null;
            }
        }
    }
}
