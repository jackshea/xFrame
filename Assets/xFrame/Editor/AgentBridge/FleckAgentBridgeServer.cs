using System;
using System.Collections.Generic;
using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using xFrame.Runtime.Logging;
using xFrame.Runtime.Networking.AgentBridge;
using xFrame.Runtime.Networking.AgentBridge.Commands;

namespace xFrame.Editor.AgentBridge
{
    /// <summary>
    /// 基于 Fleck 的 Unity Agent Bridge 服务。
    /// </summary>
    public sealed class FleckAgentBridgeServer : IDisposable
    {
        private readonly AgentBridgeOptions _options;
        private readonly IXLogger _logger;
        private readonly AgentRpcRouter _router;
        private EditorMainThreadDispatcher _dispatcher;
        private readonly List<IWebSocketConnection> _connections = new();
        private readonly object _connectionLock = new();
        private readonly TimeSpan _dispatchTimeout;

        private WebSocketServer _server;

        public FleckAgentBridgeServer(AgentBridgeOptions options = null)
        {
            _options = options ?? new AgentBridgeOptions();
            _logger = new XLogManager().GetLogger("AgentBridge");
            _dispatcher = new EditorMainThreadDispatcher();
            _dispatchTimeout = TimeSpan.FromMilliseconds(Math.Max(100, _options.MainThreadTimeoutMs));

            var registry = new AgentCommandRegistry();
            registry.Register(new PingCommandHandler());
            registry.Register(new AuthenticateCommandHandler());
            registry.Register(new ListCommandsHandler());
            registry.Register(new FindGameObjectCommandHandler());
            registry.Register(new InvokeComponentCommandHandler());

            _router = new AgentRpcRouter(_options, registry);
        }

        public bool IsRunning => _server != null;

        public string Endpoint => $"ws://{_options.Host}:{_options.Port}";

        public void Start()
        {
            if (_server != null)
            {
                return;
            }

            if (_dispatcher == null)
            {
                _dispatcher = new EditorMainThreadDispatcher();
            }

            var endpoint = Endpoint;

            try
            {
                _server = new WebSocketServer(endpoint);
                _server.Start(socket =>
                {
                    socket.OnOpen = () =>
                    {
                        lock (_connectionLock)
                        {
                            _connections.Add(socket);
                        }

                        _logger.Info($"AgentBridge connected: {socket.ConnectionInfo.ClientIpAddress}");
                    };

                    socket.OnClose = () =>
                    {
                        lock (_connectionLock)
                        {
                            _connections.Remove(socket);
                        }

                        _router.RemoveContext(socket.ConnectionInfo.Id.ToString());
                        _logger.Info("AgentBridge connection closed.");
                    };

                    socket.OnError = ex =>
                    {
                        _logger.Error("AgentBridge socket error.", ex);
                    };

                    socket.OnMessage = message =>
                    {
                        var connectionId = socket.ConnectionInfo.Id.ToString();
                        var response = HandleMessage(message, connectionId);
                        if (!string.IsNullOrWhiteSpace(response))
                        {
                            socket.Send(response);
                        }
                    };
                });

                _logger.Info($"AgentBridge started at {endpoint}");
            }
            catch (Exception ex)
            {
                _logger.Error($"AgentBridge startup failed. endpoint={endpoint}", ex);
                _server?.Dispose();
                _server = null;
                throw;
            }
        }

        public void Stop()
        {
            if (_server == null)
            {
                DisposeDispatcher();
                return;
            }

            try
            {
                IWebSocketConnection[] snapshot;
                lock (_connectionLock)
                {
                    snapshot = _connections.ToArray();
                    _connections.Clear();
                }

                foreach (var connection in snapshot)
                {
                    connection.Close();
                }

                _server.Dispose();
                _router.ClearContexts();
                _logger.Info("AgentBridge stopped.");
            }
            finally
            {
                _server = null;
                DisposeDispatcher();
            }
        }

        private string HandleMessage(string message, string connectionId)
        {
            try
            {
                if (_dispatcher == null)
                {
                    throw new InvalidOperationException("Main thread dispatcher is not available.");
                }

                return _dispatcher.Invoke(() => _router.Handle(message, connectionId), _dispatchTimeout);
            }
            catch (Exception ex)
            {
                _logger.Error("AgentBridge request handling failed.", ex);
                return BuildInternalErrorResponse(message, ex);
            }
        }

        private void DisposeDispatcher()
        {
            _dispatcher?.Dispose();
            _dispatcher = null;
        }

        private static string BuildInternalErrorResponse(string message, Exception ex)
        {
            if (!TryReadRequestId(message, out var requestId))
            {
                return null;
            }

            var response = new JsonRpcResponse
            {
                Id = requestId,
                Error = new JsonRpcError
                {
                    Code = AgentRpcErrorCodes.InternalError,
                    Message = "Internal error.",
                    Data = ex.Message
                }
            };

            return JsonConvert.SerializeObject(response);
        }

        private static bool TryReadRequestId(string message, out JToken requestId)
        {
            requestId = null;

            try
            {
                var requestObj = JObject.Parse(message);
                if (!requestObj.TryGetValue("id", out var idToken) || idToken == null || idToken.Type == JTokenType.Null)
                {
                    return false;
                }

                requestId = idToken;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
