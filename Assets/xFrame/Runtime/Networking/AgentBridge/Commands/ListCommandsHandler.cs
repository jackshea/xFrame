using System;
using System.Collections.Generic;

namespace xFrame.Runtime.Networking.AgentBridge.Commands
{
    public sealed class ListCommandsHandler : IAgentRpcCommandHandler
    {
        public string Method => "agent.commands";

        public bool RequiresAuthentication => true;

        public AgentRpcExecutionResult Execute(JsonRpcRequest request, AgentRpcContext context)
        {
            var commands = new HashSet<string>(context.Registry.GetMethods(), StringComparer.Ordinal);
            if (context.Options.EnableReflectionBridge)
            {
                commands.Add("unity.reflect.invoke");
            }

            return AgentRpcExecutionResult.Success(new { commands });
        }
    }
}
