using Newtonsoft.Json.Linq;
using UnityEditor;
using xFrame.Runtime.Networking.AgentBridge;

namespace xFrame.Editor.AgentBridge
{
    /// <summary>
    ///     执行 Unity Editor 菜单命令。
    /// </summary>
    public sealed class EditorExecuteMenuCommandHandler : IAgentRpcCommandHandler
    {
        public string Method => "unity.editor.executeMenu";

        public bool RequiresAuthentication => true;

        public AgentRpcExecutionResult Execute(JsonRpcRequest request, AgentRpcContext context)
        {
            if (request.Params is not JObject paramObj)
                return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "params must be object.");

            var menuPath = paramObj.Value<string>("menuPath");
            if (string.IsNullOrWhiteSpace(menuPath))
                return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "menuPath is required.");

            var executed = EditorApplication.ExecuteMenuItem(menuPath);
            return AgentRpcExecutionResult.Success(new
            {
                executed,
                menuPath
            });
        }
    }
}