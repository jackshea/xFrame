using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using xFrame.Runtime.Logging;

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
        private readonly IXLogger _logger;
        private readonly object _contextLock = new();
        private readonly Dictionary<string, AgentRpcContext> _contexts =
            new(StringComparer.Ordinal);

        public AgentRpcRouter(AgentBridgeOptions options, AgentCommandRegistry registry, AgentReflectionInvoker reflectionInvoker = null, IXLogger logger = null)
        {
            _options = options ?? new AgentBridgeOptions();
            _registry = registry ?? new AgentCommandRegistry();
            _reflectionInvoker = reflectionInvoker ?? new AgentReflectionInvoker();
            _logger = logger ?? new XLogManager().GetLogger("AgentBridge.Router");
        }

        public string Handle(string payload, string connectionId)
        {
            var normalizedConnectionId = NormalizeConnectionId(connectionId);
            _logger.Debug($"AgentBridge rpc direction=receive connectionId={normalizedConnectionId} payloadLength={payload?.Length ?? 0}");

            JsonRpcRequest request;
            try
            {
                request = JsonConvert.DeserializeObject<JsonRpcRequest>(payload);
            }
            catch (Exception ex)
            {
                _logger.Warning($"AgentBridge rpc direction=receive parse error connectionId={normalizedConnectionId} message={ex.Message}");
                return SerializeError(null, AgentRpcErrorCodes.ParseError, "Parse error.", ex.Message);
            }

            if (request == null || !string.Equals(request.JsonRpc, "2.0", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(request.Method))
            {
                _logger.Warning($"AgentBridge rpc direction=receive invalid request connectionId={normalizedConnectionId}");
                return SerializeError(request?.Id, AgentRpcErrorCodes.InvalidRequest, "Invalid request.");
            }

            var isNotification = request.Id == null || request.Id.Type == JTokenType.Null;
            var context = GetOrCreateContext(connectionId);

            try
            {
                if (_registry.TryGet(request.Method, out var handler))
                {
                    if (handler.RequiresAuthentication && !context.IsAuthenticated)
                    {
                        return SerializeErrorAndLog(request.Id, AgentRpcErrorCodes.Unauthorized, "Unauthorized.", normalizedConnectionId, isNotification);
                    }

                    var result = handler.Execute(request, context);
                    if (result.Error != null)
                    {
                        return SerializeErrorAndLog(request.Id, result.Error.Code, result.Error.Message, normalizedConnectionId, isNotification, result.Error.Data);
                    }

                    return SerializeResultAndLog(request.Id, result.Result, normalizedConnectionId, isNotification);
                }

                if (string.Equals(request.Method, "unity.reflect.invoke", StringComparison.Ordinal))
                {
                    if (!context.IsAuthenticated)
                    {
                        return SerializeErrorAndLog(request.Id, AgentRpcErrorCodes.Unauthorized, "Unauthorized.", normalizedConnectionId, isNotification);
                    }

                    var reflectionResult = _reflectionInvoker.Invoke(request, context);
                    if (reflectionResult.Error != null)
                    {
                        return SerializeErrorAndLog(
                            request.Id,
                            reflectionResult.Error.Code,
                            reflectionResult.Error.Message,
                            normalizedConnectionId,
                            isNotification,
                            reflectionResult.Error.Data);
                    }

                    return SerializeResultAndLog(request.Id, reflectionResult.Result, normalizedConnectionId, isNotification);
                }

                return SerializeErrorAndLog(request.Id, AgentRpcErrorCodes.MethodNotFound, "Method not found.", normalizedConnectionId, isNotification);
            }
            catch (Exception ex)
            {
                _logger.Error($"AgentBridge rpc direction=error connectionId={normalizedConnectionId} method={request.Method}", ex);
                return SerializeErrorAndLog(request.Id, AgentRpcErrorCodes.InternalError, "Internal error.", normalizedConnectionId, isNotification, ex.Message);
            }
        }

        private string SerializeResultAndLog(JToken id, object result, string connectionId, bool isNotification)
        {
            var response = SerializeResult(id, result, isNotification);
            if (!isNotification)
            {
                _logger.Debug($"AgentBridge rpc direction=send connectionId={connectionId} hasError=false");
            }

            return response;
        }

        private string SerializeErrorAndLog(JToken id, int code, string message, string connectionId, bool isNotification, object data = null)
        {
            var response = SerializeError(id, code, message, data, isNotification);
            if (!isNotification)
            {
                var level = code == AgentRpcErrorCodes.InternalError ? LogLevel.Error : LogLevel.Warning;
                _logger.Log(level, $"AgentBridge rpc direction=send connectionId={connectionId} hasError=true code={code} message={message}");
            }

            return response;
        }

        public void RemoveContext(string connectionId)
        {
            var key = NormalizeConnectionId(connectionId);
            lock (_contextLock)
            {
                _contexts.Remove(key);
            }
        }

        public void ClearContexts()
        {
            lock (_contextLock)
            {
                _contexts.Clear();
            }
        }

        private AgentRpcContext GetOrCreateContext(string connectionId)
        {
            var key = NormalizeConnectionId(connectionId);
            lock (_contextLock)
            {
                if (_contexts.TryGetValue(key, out var context))
                {
                    return context;
                }

                context = new AgentRpcContext(key, _options, _registry);
                _contexts[key] = context;
                return context;
            }
        }

        private static string NormalizeConnectionId(string connectionId)
        {
            return string.IsNullOrWhiteSpace(connectionId) ? "default" : connectionId;
        }

        private static string SerializeResult(JToken id, object result, bool isNotification)
        {
            if (isNotification)
            {
                return null;
            }

            var response = new JsonRpcResponse
            {
                Id = id,
                Result = result
            };
            return JsonConvert.SerializeObject(response);
        }

        private static string SerializeError(JToken id, int code, string message, object data = null, bool isNotification = false)
        {
            if (isNotification)
            {
                return null;
            }

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
