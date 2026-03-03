namespace xFrame.Runtime.Networking.AgentBridge.Commands
{
    public sealed class ListCommandsHandler : IAgentRpcCommandHandler
    {
        public string Method => "agent.commands";

        public bool RequiresAuthentication => true;

        public AgentRpcExecutionResult Execute(JsonRpcRequest request, AgentRpcContext context)
        {
            return AgentRpcExecutionResult.Success(new { commands = context.Registry.GetMethods() });
        }
    }
}
