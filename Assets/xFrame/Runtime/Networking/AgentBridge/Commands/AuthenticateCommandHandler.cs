using Newtonsoft.Json.Linq;

namespace xFrame.Runtime.Networking.AgentBridge.Commands
{
    public sealed class AuthenticateCommandHandler : IAgentRpcCommandHandler
    {
        public string Method => "agent.authenticate";

        public bool RequiresAuthentication => false;

        public AgentRpcExecutionResult Execute(JsonRpcRequest request, AgentRpcContext context)
        {
            if (request.Params is not JObject paramObj)
            {
                return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "params must be object.");
            }

            var token = paramObj.Value<string>("token");
            if (string.IsNullOrWhiteSpace(token) || token != context.Options.AuthToken)
            {
                return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.Unauthorized, "Invalid token.");
            }

            context.IsAuthenticated = true;
            return AgentRpcExecutionResult.Success(new { authenticated = true });
        }
    }
}
