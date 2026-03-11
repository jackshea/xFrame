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
    ///     基于 Fleck 的 Unity Agent Bridge 服务。
    /// </summary>
    public sealed class FleckAgentBridgeServer : IDisposable
    {
        private readonly object _connectionLock = new();
        private readonly List<IWebSocketConnection> _connections = new();
        private readonly TimeSpan _dispatchTimeout;
        private readonly IAgentBridgeEndpointPersistence _endpointPersistence;
        private readonly IXLogger _logger;
        private readonly AgentBridgeOptions _options;
        private readonly AgentRpcRouter _router;
        private readonly AgentBridgeTokenPersistence _tokenPersistence;
        private EditorMainThreadDispatcher _dispatcher;

        private WebSocketServer _server;

        public FleckAgentBridgeServer(AgentBridgeOptions options = null, IXLogger logger = null,
            IAgentBridgeEndpointPersistence endpointPersistence = null)
        {
            _options = options ?? new AgentBridgeOptions();
            _logger = logger ?? new XLogManager().GetLogger("AgentBridge");
            _endpointPersistence = endpointPersistence ?? new AgentBridgeEndpointPersistence();
            _tokenPersistence = new AgentBridgeTokenPersistence();
            _dispatcher = new EditorMainThreadDispatcher();
            _dispatchTimeout = TimeSpan.FromMilliseconds(Math.Max(100, _options.MainThreadTimeoutMs));

            LoadPersistedEndpoint();
            EnsureAuthToken();

            var registry = new AgentCommandRegistry();
            registry.Register(new PingCommandHandler());
            registry.Register(new AuthenticateCommandHandler());
            registry.Register(new ListCommandsHandler());
            registry.Register(new FindGameObjectCommandHandler());
            registry.Register(new InvokeComponentCommandHandler());
            registry.Register(new StartupRunCommandHandler());
            registry.Register(new StartupStopCommandHandler());
            registry.Register(new EditorExecuteMenuCommandHandler());
            registry.Register(new EditorRunTestsCommandHandler());
            registry.Register(new EditorRunTestsCommandHandler.LastResultHandler());

            _router = new AgentRpcRouter(_options, registry, logger: _logger);
        }

        public bool IsRunning => _server != null;

        public string Endpoint => $"ws://{_options.Host}:{_options.Port}";

        public string AuthToken => _options.AuthToken;

        public void Dispose()
        {
            Stop();
        }

        public bool SetEndpoint(string host, int port, out string error)
        {
            error = null;
            if (!AgentBridgeOptions.ValidateEndpoint(host, port, out error))
            {
                _logger.Warning($"AgentBridge endpoint rejected. host={host}, port={port}, error={error}");
                return false;
            }

            host = host.Trim();
            if (_options.IsSameEndpoint(host, port))
            {
                _logger.Info($"AgentBridge endpoint unchanged. endpoint={Endpoint}");
                return true;
            }

            if (!_endpointPersistence.TrySave(host, port, out error))
            {
                _logger.Warning($"AgentBridge endpoint save failed. host={host}, port={port}, error={error}");
                return false;
            }

            _options.Host = host;
            _options.Port = port;

            _logger.Info($"AgentBridge endpoint updated. endpoint={Endpoint}");
            if (IsRunning)
            {
                _logger.Info("AgentBridge restarting for endpoint change.");
                Stop();
                Start();
            }

            return true;
        }

        public void Start()
        {
            if (_server != null) return;

            if (_dispatcher == null) _dispatcher = new EditorMainThreadDispatcher();

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

                        _logger.Info(
                            $"AgentBridge connection opened. endpoint={Endpoint}, connectionId={socket.ConnectionInfo.Id}, clientIp={socket.ConnectionInfo.ClientIpAddress}");
                    };

                    socket.OnClose = () =>
                    {
                        lock (_connectionLock)
                        {
                            _connections.Remove(socket);
                        }

                        _router.RemoveContext(socket.ConnectionInfo.Id.ToString());
                        _logger.Info(
                            $"AgentBridge connection closed. endpoint={Endpoint}, connectionId={socket.ConnectionInfo.Id}");
                    };

                    socket.OnError = ex =>
                    {
                        _logger.Error(
                            $"AgentBridge socket error. endpoint={Endpoint}, connectionId={socket.ConnectionInfo.Id}",
                            ex);
                    };

                    socket.OnMessage = message =>
                    {
                        var connectionId = socket.ConnectionInfo.Id.ToString();
                        var response = HandleMessage(message, connectionId);
                        if (!string.IsNullOrWhiteSpace(response)) socket.Send(response);
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

                foreach (var connection in snapshot) connection.Close();

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
                    throw new InvalidOperationException("Main thread dispatcher is not available.");

                return _dispatcher.Invoke(() => _router.Handle(message, connectionId), _dispatchTimeout);
            }
            catch (Exception ex)
            {
                _logger.Error("AgentBridge request handling failed.", ex);
                return BuildInternalErrorResponse(message, ex);
            }
        }

        private void LoadPersistedEndpoint()
        {
            var loadResult = _endpointPersistence.Load(out var host, out var port, out var error);
            if (loadResult == AgentBridgeEndpointLoadResult.Loaded)
            {
                _options.Host = host;
                _options.Port = port;
                _logger.Info($"AgentBridge endpoint loaded from persistence. endpoint=ws://{host}:{port}");
                return;
            }

            if (loadResult == AgentBridgeEndpointLoadResult.Invalid)
            {
                _options.Host = AgentBridgeOptions.DefaultHost;
                _options.Port = AgentBridgeOptions.DefaultPort;
                _logger.Warning(
                    $"AgentBridge persisted endpoint is invalid, fallback to default. error={error}, endpoint=ws://{_options.Host}:{_options.Port}");
            }
        }

        private void DisposeDispatcher()
        {
            _dispatcher?.Dispose();
            _dispatcher = null;
        }

        private void EnsureAuthToken()
        {
            if (!string.IsNullOrWhiteSpace(_options.AuthToken)) return;

            _options.AuthToken = _tokenPersistence.LoadOrCreateToken(out var createdNewToken);
            if (createdNewToken)
                _logger.Warning("AgentBridge generated a new local auth token. Check UserSettings/AgentBridgeSettings.json.");
        }

        private string BuildInternalErrorResponse(string message, Exception ex)
        {
            if (!TryReadRequestId(message, out var requestId, ex)) return null;

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

        private bool TryReadRequestId(string message, out JToken requestId, Exception originalException)
        {
            requestId = null;

            try
            {
                var requestObj = JObject.Parse(message);
                if (!requestObj.TryGetValue("id", out var idToken) || idToken == null ||
                    idToken.Type == JTokenType.Null) return false;

                requestId = idToken;
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    $"AgentBridge failed to read request id from malformed payload. payloadLength={message?.Length ?? 0}, error={ex.Message}, originalError={originalException?.Message}");
                return false;
            }
        }
    }
}
