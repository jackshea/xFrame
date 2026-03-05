using Newtonsoft.Json.Linq;
using UnityEngine;

namespace xFrame.Runtime.Networking.AgentBridge.Commands
{
    public sealed class FindGameObjectCommandHandler : IAgentRpcCommandHandler
    {
        public string Method => "unity.gameobject.find";

        public bool RequiresAuthentication => true;

        public AgentRpcExecutionResult Execute(JsonRpcRequest request, AgentRpcContext context)
        {
            if (request.Params is not JObject paramObj)
                return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "params must be object.");

            var name = paramObj.Value<string>("name");
            if (string.IsNullOrWhiteSpace(name))
                return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "name is required.");

            var go = GameObject.Find(name);
            if (go == null) return AgentRpcExecutionResult.Success(new { found = false, name });

            return AgentRpcExecutionResult.Success(new
            {
                found = true,
                go.name,
                instanceId = go.GetInstanceID()
            });
        }
    }
}