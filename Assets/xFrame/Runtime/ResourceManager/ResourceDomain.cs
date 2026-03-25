using System;
using System.Threading;

namespace xFrame.Runtime.ResourceManager
{
    /// <summary>
    ///     资源域。
    ///     表示一组资源请求所属的业务生命周期边界，用于拦截生命周期失效后的晚到异步结果。
    /// </summary>
    public sealed class ResourceDomain
    {
        private readonly object _syncRoot = new();
        private int _generation;
        private bool _isAlive;

        internal ResourceDomain(long domainId, string name)
        {
            DomainId = domainId;
            Name = string.IsNullOrWhiteSpace(name) ? $"Domain_{domainId}" : name;
            _generation = 0;
            _isAlive = true;
        }

        /// <summary>
        ///     资源域唯一标识。
        /// </summary>
        public long DomainId { get; }

        /// <summary>
        ///     资源域名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     当前生命周期代际。
        /// </summary>
        public int Generation
        {
            get
            {
                lock (_syncRoot)
                {
                    return _generation;
                }
            }
        }

        /// <summary>
        ///     当前资源域是否仍处于有效生命周期。
        /// </summary>
        public bool IsAlive
        {
            get
            {
                lock (_syncRoot)
                {
                    return _isAlive;
                }
            }
        }

        /// <summary>
        ///     获取资源域当前的生命周期快照。
        /// </summary>
        /// <param name="generation">当前代际。</param>
        /// <param name="isAlive">当前是否存活。</param>
        internal void GetSnapshot(out int generation, out bool isAlive)
        {
            lock (_syncRoot)
            {
                generation = _generation;
                isAlive = _isAlive;
            }
        }

        /// <summary>
        ///     销毁资源域。
        ///     销毁后当前代际不再有效，等待调用方显式续代后才可重新使用。
        /// </summary>
        internal void Destroy()
        {
            lock (_syncRoot)
            {
                _isAlive = false;
            }
        }

        /// <summary>
        ///     推进到下一代生命周期，并重新进入可用状态。
        /// </summary>
        internal void Renew()
        {
            lock (_syncRoot)
            {
                checked
                {
                    _generation++;
                }

                _isAlive = true;
            }
        }
    }
}
