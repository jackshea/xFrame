using System;
using System.Collections.Generic;

namespace xFrame.Runtime.Networking.AgentBridge
{
    /// <summary>
    /// 命令处理器注册表。
    /// </summary>
    public sealed class AgentCommandRegistry
    {
        private readonly Dictionary<string, IAgentRpcCommandHandler> _handlers =
            new(StringComparer.Ordinal);

        public void Register(IAgentRpcCommandHandler handler)
        {
            if (handler == null || string.IsNullOrWhiteSpace(handler.Method))
            {
                return;
            }

            _handlers[handler.Method] = handler;
        }

        public bool TryGet(string method, out IAgentRpcCommandHandler handler)
        {
            return _handlers.TryGetValue(method, out handler);
        }

        public IReadOnlyCollection<string> GetMethods()
        {
            return _handlers.Keys;
        }
    }
}
