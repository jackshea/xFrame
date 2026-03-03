using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace xFrame.Runtime.Networking.AgentBridge
{
    /// <summary>
    /// JSON-RPC 路由器。
    /// </summary>
    public sealed class AgentRpcRouter
    {
        private readonly AgentBridgeOptions _options;
        private readonly AgentCommandRegistry _registry;
        private readonly AgentReflectionInvoker _reflectionInvoker;
        private readonly Dictionary<string, AgentRpcContext> _contexts =
            new(StringComparer.Ordinal);

        public AgentRpcRouter(AgentBridgeOptions options, AgentCommandRegistry registry, AgentReflectionInvoker reflectionInvoker = null)
        {
            _options = options ?? new AgentBridgeOptions();
            _registry = registry ?? new AgentCommandRegistry();
            _reflectionInvoker = reflectionInvoker ?? new AgentReflectionInvoker();
        }

        public string Handle(string payload, string connectionId)
        {
            JsonRpcRequest request;
            try
            {
                request = JsonConvert.DeserializeObject<JsonRpcRequest>(payload);
            }
            catch (Exception ex)
            {
                return SerializeError(null, AgentRpcErrorCodes.ParseError, "Parse error.", ex.Message);
            }

            if (request == null || !string.Equals(request.JsonRpc, "2.0", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(request.Method))
            {
                return SerializeError(request?.Id, AgentRpcErrorCodes.InvalidRequest, "Invalid request.");
            }

            var context = GetOrCreateContext(connectionId);

            try
            {
                if (_registry.TryGet(request.Method, out var handler))
                {
                    if (handler.RequiresAuthentication && !context.IsAuthenticated)
                    {
                        return SerializeError(request.Id, AgentRpcErrorCodes.Unauthorized, "Unauthorized.");
                    }

                    var result = handler.Execute(request, context);
                    if (result.Error != null)
                    {
                        return SerializeError(request.Id, result.Error.Code, result.Error.Message, result.Error.Data);
                    }

                    return SerializeResult(request.Id, result.Result);
                }

                if (string.Equals(request.Method, "unity.reflect.invoke", StringComparison.Ordinal))
                {
                    if (!context.IsAuthenticated)
                    {
                        return SerializeError(request.Id, AgentRpcErrorCodes.Unauthorized, "Unauthorized.");
                    }

                    var reflectionResult = _reflectionInvoker.Invoke(request, context);
                    if (reflectionResult.Error != null)
                    {
                        return SerializeError(
                            request.Id,
                            reflectionResult.Error.Code,
                            reflectionResult.Error.Message,
                            reflectionResult.Error.Data);
                    }

                    return SerializeResult(request.Id, reflectionResult.Result);
                }

                return SerializeError(request.Id, AgentRpcErrorCodes.MethodNotFound, "Method not found.");
            }
            catch (Exception ex)
            {
                return SerializeError(request.Id, AgentRpcErrorCodes.InternalError, "Internal error.", ex.Message);
            }
        }

        private AgentRpcContext GetOrCreateContext(string connectionId)
        {
            var key = string.IsNullOrWhiteSpace(connectionId) ? "default" : connectionId;
            if (_contexts.TryGetValue(key, out var context))
            {
                return context;
            }

            context = new AgentRpcContext(key, _options, _registry);
            _contexts[key] = context;
            return context;
        }

        private static string SerializeResult(JToken id, object result)
        {
            var response = new JsonRpcResponse
            {
                Id = id,
                Result = result
            };
            return JsonConvert.SerializeObject(response);
        }

        private static string SerializeError(JToken id, int code, string message, object data = null)
        {
            var response = new JsonRpcResponse
            {
                Id = id,
                Error = new JsonRpcError
                {
                    Code = code,
                    Message = message,
                    Data = data
                }
            };
            return JsonConvert.SerializeObject(response);
        }
    }
}
