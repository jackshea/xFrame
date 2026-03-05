using System;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace xFrame.Runtime.Networking.AgentBridge
{
    /// <summary>
    ///     受限反射调用器。
    /// </summary>
    public sealed class AgentReflectionInvoker
    {
        public AgentRpcExecutionResult Invoke(JsonRpcRequest request, AgentRpcContext context)
        {
            if (!context.Options.EnableReflectionBridge)
                return AgentRpcExecutionResult.Failure(
                    AgentRpcErrorCodes.ReflectionDenied,
                    "Reflection bridge disabled.");

            if (request.Params is not JObject paramObj)
                return AgentRpcExecutionResult.Failure(
                    AgentRpcErrorCodes.InvalidParams,
                    "params must be object.");

            var assemblyName = paramObj.Value<string>("assembly");
            var typeName = paramObj.Value<string>("type");
            var methodName = paramObj.Value<string>("method");
            var args = paramObj["args"] as JArray;

            if (string.IsNullOrWhiteSpace(assemblyName) ||
                string.IsNullOrWhiteSpace(typeName) ||
                string.IsNullOrWhiteSpace(methodName))
                return AgentRpcExecutionResult.Failure(
                    AgentRpcErrorCodes.InvalidParams,
                    "assembly/type/method are required.");

            if (!IsAllowedAssembly(assemblyName, context.Options.AllowedAssemblies) ||
                !IsAllowedType(typeName, context.Options.AllowedTypePrefixes))
                return AgentRpcExecutionResult.Failure(
                    AgentRpcErrorCodes.ReflectionDenied,
                    "Type or assembly is not allowed.");

            try
            {
                var targetType = ResolveType(assemblyName, typeName);
                if (targetType == null)
                    return AgentRpcExecutionResult.Failure(
                        AgentRpcErrorCodes.ReflectionDenied,
                        "Target type not found.");

                var method = targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                    return AgentRpcExecutionResult.Failure(
                        AgentRpcErrorCodes.ReflectionDenied,
                        "Only public static method is allowed.");

                var methodParams = method.GetParameters();
                var invokeArgs = new object[methodParams.Length];
                if (methodParams.Length > 0)
                {
                    if (args == null || args.Count != methodParams.Length)
                        return AgentRpcExecutionResult.Failure(
                            AgentRpcErrorCodes.InvalidParams,
                            "args length mismatch.");

                    for (var i = 0; i < methodParams.Length; i++)
                        invokeArgs[i] = args[i].ToObject(methodParams[i].ParameterType);
                }

                var result = method.Invoke(null, invokeArgs);
                return AgentRpcExecutionResult.Success(new { success = true, result });
            }
            catch (Exception ex)
            {
                return AgentRpcExecutionResult.Failure(
                    AgentRpcErrorCodes.InternalError,
                    "Reflection invoke failed.",
                    ex.Message);
            }
        }

        private static Type ResolveType(string assemblyName, string typeName)
        {
            var assemblyQualifiedName = $"{typeName}, {assemblyName}";
            return Type.GetType(assemblyQualifiedName);
        }

        private static bool IsAllowedAssembly(string assemblyName, string[] allowedAssemblies)
        {
            if (allowedAssemblies == null || allowedAssemblies.Length == 0) return false;

            foreach (var allowed in allowedAssemblies)
                if (string.Equals(allowed, assemblyName, StringComparison.Ordinal))
                    return true;

            return false;
        }

        private static bool IsAllowedType(string typeName, string[] allowedTypePrefixes)
        {
            if (allowedTypePrefixes == null || allowedTypePrefixes.Length == 0) return false;

            foreach (var prefix in allowedTypePrefixes)
                if (!string.IsNullOrEmpty(prefix) && typeName.StartsWith(prefix, StringComparison.Ordinal))
                    return true;

            return false;
        }
    }
}