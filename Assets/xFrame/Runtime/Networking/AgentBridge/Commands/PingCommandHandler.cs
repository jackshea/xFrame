using System;

namespace xFrame.Runtime.Networking.AgentBridge.Commands
{
    public sealed class PingCommandHandler : IAgentRpcCommandHandler
    {
        public string Method => "agent.ping";

        public bool RequiresAuthentication => false;

        public AgentRpcExecutionResult Execute(JsonRpcRequest request, AgentRpcContext context)
        {
            return AgentRpcExecutionResult.Success(new { pong = true, utc = DateTime.UtcNow.ToString("O") });
        }
    }
}
