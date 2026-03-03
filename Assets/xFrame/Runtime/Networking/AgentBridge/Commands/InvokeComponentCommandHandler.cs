using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace xFrame.Runtime.Networking.AgentBridge.Commands
{
    public sealed class InvokeComponentCommandHandler : IAgentRpcCommandHandler
    {
        public string Method => "unity.component.invoke";

        public bool RequiresAuthentication => true;

        public AgentRpcExecutionResult Execute(JsonRpcRequest request, AgentRpcContext context)
        {
            if (request.Params is not JObject paramObj)
            {
                return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "params must be object.");
            }

            var gameObjectName = paramObj.Value<string>("gameObjectName");
            var componentType = paramObj.Value<string>("componentType");
            var methodName = paramObj.Value<string>("method");
            var args = paramObj["args"] as JArray;

            if (string.IsNullOrWhiteSpace(gameObjectName) ||
                string.IsNullOrWhiteSpace(componentType) ||
                string.IsNullOrWhiteSpace(methodName))
            {
                return AgentRpcExecutionResult.Failure(
                    AgentRpcErrorCodes.InvalidParams,
                    "gameObjectName/componentType/method are required.");
            }

            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                {
                    return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "game object not found.");
                }

                var comp = FindComponent(go, componentType);
                if (comp == null)
                {
                    return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "component not found.");
                }

                var method = comp.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
                if (method == null)
                {
                    return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "method not found.");
                }

                var methodParams = method.GetParameters();
                var invokeArgs = new object[methodParams.Length];
                if (methodParams.Length > 0)
                {
                    if (args == null || args.Count != methodParams.Length)
                    {
                        return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "args length mismatch.");
                    }

                    for (var i = 0; i < methodParams.Length; i++)
                    {
                        invokeArgs[i] = args[i].ToObject(methodParams[i].ParameterType);
                    }
                }

                var result = method.Invoke(comp, invokeArgs);
                return AgentRpcExecutionResult.Success(new { success = true, result });
            }
            catch (Exception ex)
            {
                return AgentRpcExecutionResult.Failure(
                    AgentRpcErrorCodes.InternalError,
                    "invoke component failed.",
                    ex.Message);
            }
        }

        private static Component FindComponent(GameObject go, string componentType)
        {
            foreach (var component in go.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                if (string.Equals(component.GetType().Name, componentType, StringComparison.Ordinal) ||
                    string.Equals(component.GetType().FullName, componentType, StringComparison.Ordinal))
                {
                    return component;
                }
            }

            return null;
        }
    }
}
